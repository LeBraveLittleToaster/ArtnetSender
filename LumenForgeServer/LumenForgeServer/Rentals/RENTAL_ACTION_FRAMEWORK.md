# Rental Action Framework

## Overview

The Rental Action Framework is a **stage-based workflow engine** that manages the complete lifecycle of a rental—from initial request through return, inspection, invoicing, and completion. Every mutation to a rental flows through a single orchestrator (`RentalActionService`) that enforces stage constraints, runs a before → execute → after handler lifecycle, persists stage transitions, and writes an immutable audit log.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│  HTTP Layer — RentalActionController                                 │
│  24 endpoints: 1 query + 1 create + 22 process actions              │
│  Always injects ActorKcId from JWT (never from request body)         │
└───────────────────┬──────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Orchestrator — RentalActionService                                  │
│  1. Load RentalProcessInstance (by GUID)                             │
│  2. Resolve handler by RentalActionType                              │
│  3. Validate stage via RentalActionRegistry                          │
│  4. handler.BeforeExecuteAsync()  →  validation / context loading    │
│  5. handler.ExecuteAsync()        →  core business logic             │
│  6. Apply stage transition (if signalled)                            │
│  7. handler.AfterExecuteAsync()   →  side-effects / notifications    │
│  8. Write RentalActionLog                                            │
│  9. SaveChanges                                                      │
└───────────────────┬──────────────────────────────────────────────────┘
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
┌──────────────────┐  ┌──────────────────────┐
│ IRentalAction-   │  │ IRentalProcess-      │
│ Registry         │  │ Repository           │
│ (stage→actions)  │  │ (EF Core / Postgres) │
└──────────────────┘  └──────────────────────┘
```

### Key Components

| Component | Responsibility |
|---|---|
| `RentalActionController` | HTTP API. 24 endpoints under `api/v1/rentals/actions/`. Extracts `ActorKcId` from JWT `sub` claim. |
| `RentalActionService` | Central orchestrator. Loads process, resolves handler, validates stage, runs lifecycle, persists. |
| `IRentalActionHandler` | Handler contract (non-generic). Declares `ActionType`, `AllowedStages`, and the three lifecycle methods. |
| `RentalActionHandlerBase<TInput>` | Generic base class. Deserialises the JSON `ActionInput` into a strongly-typed `TInput` DTO. |
| `RentalActionRegistry` | Static lookup table mapping each `RentalStage` to the set of `RentalActionType` values valid in that stage. |
| `IRentalProcessRepository` | Data-access contract for `RentalProcessInstance` and related entities (Checklists, Extensions, DamageReports, ActionLogs). |

---

## Rental Stages

The `RentalStage` enum defines the lifecycle states of a rental process:

```
None → Requested → Approved → ItemsAssigned → ItemsApproved
                                                     │
                                                     ▼
                                              ReadyForPickup → PickedUp → Returned → Inspected → Invoiced → Paid → Completed
                                                                    │
                                                                    └─→ Scrapped
```

Side exits: **Cancelled** (from Requested, Approved, ItemsAssigned, ItemsApproved, ReadyForPickup) and **Scrapped** (from PickedUp).

| Stage | Description |
|---|---|
| `None` | No process exists yet. Only `CreateRental` is valid. |
| `Requested` | A rental request has been submitted; awaits staff review. |
| `Approved` | The request was approved; inventory items can now be assigned. |
| `ItemsAssigned` | Items have been assigned; awaiting item-level approval. |
| `ItemsApproved` | Items approved; checklist generation is available. |
| `ReadyForPickup` | A pickup checklist has been generated; ready for scanning/signing. |
| `PickedUp` | The customer has picked up the items; rental is active. |
| `Returned` | Items have been returned; post-return inspection pending. |
| `Inspected` | Damages recorded; maintenance jobs may be created. |
| `Invoiced` | An invoice has been generated. |
| `Paid` | Payment received; rental can be completed. |
| `Completed` | Rental successfully completed and archived. |
| `Cancelled` | Rental was cancelled before completion. |
| `Scrapped` | Rental was scrapped (total write-off of assigned items). |

---

## Actions (Handlers)

Each action maps 1-to-1 to an `IRentalActionHandler` implementation and a dedicated API endpoint.

### Stage → Available Actions Matrix

| Stage | Available Actions |
|---|---|
| `None` | CreateRental |
| `Requested` | ApproveRequest, RejectRequest, CancelRental |
| `Approved` | AssignItems, CancelRental |
| `ItemsAssigned` | AssignItems, RemoveItems, ApproveItems, RejectItems, CancelRental |
| `ItemsApproved` | GenerateChecklist, CancelRental |
| `ReadyForPickup` | ScanChecklist, SignChecklist, RecordPickup, CancelRental |
| `PickedUp` | RecordReturn, RequestExtension, ApproveExtension, RejectExtension, ScrapRental |
| `Returned` | RecordDamages, CreateMaintenanceJobs, GenerateInvoice |
| `Inspected` | CreateMaintenanceJobs, GenerateInvoice |
| `Invoiced` | RecordPayment |
| `Paid` | GenerateReport, CompleteRental |
| `Completed` | GenerateReport |
| `Cancelled` | *(none)* |
| `Scrapped` | GenerateReport |

### Handler Details

#### Process Creation

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `CreateRental` | `CreateRentalHandler` | None → Requested | Creates a `Rental` and its `RentalProcessInstance`. Validates that `RequestedStart < RequestedEnd`. Persists via repository. |

#### Request Approval

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `ApproveRequest` | `ApproveRequestHandler` | Requested → Approved | Marks the request as approved. Optionally records staff notes. |
| `RejectRequest` | `RejectRequestHandler` | Requested → Cancelled | Rejects the request. Requires a `Reason`. |

#### Item Management

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `AssignItems` | `AssignItemsHandler` | Approved → ItemsAssigned | Creates stock bindings via `StockBindingService`. Requires at least one `StockBindingGuid`. Validates a linked rental exists. |
| `RemoveItems` | `RemoveItemsHandler` | *(stays in ItemsAssigned)* | Removes stock bindings. Requires at least one `StockBindingGuid`. |
| `ApproveItems` | `ApproveItemsHandler` | ItemsAssigned → ItemsApproved | Confirms the assigned item list is correct. |
| `RejectItems` | `RejectItemsHandler` | ItemsAssigned → Approved | Rejects the item list and sends it back for re-assignment. |

#### Checklists

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `GenerateChecklist` | `GenerateChecklistHandler` | ItemsApproved → ReadyForPickup | Generates a `Checklist` with `ChecklistItem` entries from the rental's stock bindings. Validates a linked rental exists. |
| `ScanChecklist` | `ScanChecklistHandler` | *(stays in stage)* | Scans a single checklist item by `StockBindingGuid`. Records the `ScannedValue`, `ScannedByKcId`, and timestamp. Returns error if item not found on checklist. |
| `SignChecklist` | `SignChecklistHandler` | *(stays in stage)* | Signs a checklist. Validates all items have been scanned first. Returns error if checklist is already signed or has unscanned items. |

#### Pickup & Return

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `RecordPickup` | `RecordPickupHandler` | ReadyForPickup → PickedUp | Records actual pickup timestamp. Optionally records notes. |
| `RecordReturn` | `RecordReturnHandler` | PickedUp → Returned | Records actual return timestamp and notes. |

#### Extensions

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `RequestExtension` | `RequestExtensionHandler` | *(stays in PickedUp)* | Creates a `RentalExtension` record. Validates `NewRequestedEnd > current RequestedEnd` and that a linked rental exists. |
| `ApproveExtension` | `ApproveExtensionHandler` | *(stays in PickedUp)* | Approves an extension by GUID. Updates the rental's `RequestedEnd`. Returns error if already reviewed. |
| `RejectExtension` | `RejectExtensionHandler` | *(stays in PickedUp)* | Rejects an extension by GUID. Returns error if already reviewed. |

#### Post-Return Inspection

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `RecordDamages` | `RecordDamagesHandler` | Returned → Inspected | Persists `RentalDamageReport` entries. Requires at least one damage entry (each with `StockBindingGuid`, `Description`, `Severity`). |
| `CreateMaintenanceJobs` | `CreateMaintenanceJobsHandler` | *(stays in stage)* | Creates `MaintenanceJob` entities in the database. Requires at least one `DeviceId`. Each job is created with `MaintenanceStatus.Reported`. |

#### Billing

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `GenerateInvoice` | `GenerateInvoiceHandler` | Returned/Inspected → Invoiced | Generates an invoice. Validates a linked rental exists. |
| `RecordPayment` | `RecordPaymentHandler` | Invoiced → Paid | Records a payment. Validates amount is greater than zero. Records `PaymentMethod` and `TransactionReference`. |

#### Reporting & Lifecycle

| Action | Handler | Stage Transition | Description |
|---|---|---|---|
| `GenerateReport` | `GenerateReportHandler` | *(stays in stage)* | Generates a summary report. Includes rental data, stage history, and optionally damage information based on `IncludeDamages` flag. |
| `CompleteRental` | `CompleteRentalHandler` | Paid → Completed | Marks the rental as complete. |
| `CancelRental` | `CancelRentalHandler` | (multiple) → Cancelled | Cancels the rental. Allowed from: Requested, Approved, ItemsAssigned, ItemsApproved, ReadyForPickup. |
| `ScrapRental` | `ScrapRentalHandler` | PickedUp → Scrapped | Scraps the rental (total write-off). |

---

## Handler Lifecycle

Every action runs through a three-phase lifecycle managed by `RentalActionService.RunLifecycleAsync()`:

```
1. BeforeExecuteAsync(process, input, ct)
   ├─ Validation: check input constraints (e.g., dates, required fields)
   ├─ Context loading: fetch related entities from repository
   └─ Return ActionResult.Fail(...) to abort the action

2. ExecuteAsync(process, input, ct)
   ├─ Core business logic
   ├─ Persist changes (create entities, update state)
   └─ Return ActionResult.Ok(newStage) to signal a stage transition
       or ActionResult.Ok() to stay in the current stage

3. AfterExecuteAsync(process, result, ct)
   └─ Side-effects: notifications, cleanup (called regardless of success/failure)
```

After the lifecycle completes, the orchestrator:
- Applies the stage transition (if `ActionResult.NewStage` is set)
- Writes a `RentalActionLog` entry (action type, actor, timestamps, input snapshot, errors)
- Calls `SaveChangesAsync` to flush all changes in a single transaction

---

## Domain Entities

### RentalProcessInstance
The central workflow entity. Tracks the `CurrentStage` and links to:
- `Rental` — customer data, requested dates, purpose
- `Checklists` — pickup/return checklists with scannable items
- `Extensions` — rental period extension requests
- `DamageReports` — post-return damage records

### Rental
Customer-facing data: `CustomerKcId`, `CustomerName`, `CustomerEmail`, `Purpose`, `RequestedStart`/`RequestedEnd`, `Notes`.

### Checklist / ChecklistItem
A checklist contains scannable items (one per stock binding). Each item tracks `IsScanned`, `ScannedValue`, `ScannedByKcId`, and `ScannedAt`. The checklist itself tracks `SignedByKcId` and `SignedAt`.

### RentalExtension
An extension request with `OriginalEnd`, `NewRequestedEnd`, `Reason`, and approval state (`IsApproved`, `ReviewedByKcId`, `ReviewedAt`).

### RentalDamageReport
Damage records with `StockBindingGuid`, `Description`, and `Severity` (MINOR, MODERATE, SEVERE, TOTAL_LOSS).

### RentalActionLog
Immutable audit trail. Each entry records: `ActionType`, `ActorKcId`, `StageBefore`, `StageAfter`, `InputSnapshot` (serialized JSON), `Success`, `Errors`, and timestamps.

---

## API Endpoints

Base path: `api/v1/rentals/actions/`

All endpoints require authentication (JWT via Keycloak). The `ActorKcId` is always extracted from the JWT `sub` claim and **never** accepted from the request body.

| Method | Path | Action |
|---|---|---|
| GET | `{processGuid}/available` | Get available actions for a process |
| POST | `create` | CreateRental |
| POST | `{processGuid}/approve-request` | ApproveRequest |
| POST | `{processGuid}/reject-request` | RejectRequest |
| POST | `{processGuid}/assign-items` | AssignItems |
| POST | `{processGuid}/remove-items` | RemoveItems |
| POST | `{processGuid}/approve-items` | ApproveItems |
| POST | `{processGuid}/reject-items` | RejectItems |
| POST | `{processGuid}/generate-checklist` | GenerateChecklist |
| POST | `{processGuid}/scan-checklist` | ScanChecklist |
| POST | `{processGuid}/sign-checklist` | SignChecklist |
| POST | `{processGuid}/record-pickup` | RecordPickup |
| POST | `{processGuid}/record-return` | RecordReturn |
| POST | `{processGuid}/request-extension` | RequestExtension |
| POST | `{processGuid}/approve-extension` | ApproveExtension |
| POST | `{processGuid}/reject-extension` | RejectExtension |
| POST | `{processGuid}/record-damages` | RecordDamages |
| POST | `{processGuid}/create-maintenance-jobs` | CreateMaintenanceJobs |
| POST | `{processGuid}/generate-invoice` | GenerateInvoice |
| POST | `{processGuid}/record-payment` | RecordPayment |
| POST | `{processGuid}/generate-report` | GenerateReport |
| POST | `{processGuid}/complete` | CompleteRental |
| POST | `{processGuid}/cancel` | CancelRental |
| POST | `{processGuid}/scrap` | ScrapRental |

### Response Format

All action endpoints return an `ActionResult`:

```json
{
  "success": true,
  "newStage": "Approved",
  "errors": {},
  "data": { }
}
```

On validation failure:

```json
{
  "success": false,
  "newStage": null,
  "errors": {
    "fieldName": ["Error message 1", "Error message 2"]
  },
  "data": null
}
```

---

## Persistence Layer

`IRentalProcessRepository` provides 12 methods:

| Method | Purpose |
|---|---|
| `GetByGuidAsync` | Load process instance with linked Rental |
| `GetByGuidWithDetailsAsync` | Load process with all navigation properties (Checklists, Extensions, DamageReports) |
| `AddAsync` | Persist a new process instance |
| `UpdateAsync` | Update an existing process instance |
| `AddActionLogAsync` | Append an audit log entry |
| `AddRentalAsync` | Create a rental record |
| `AddChecklistAsync` | Create a checklist with items |
| `GetChecklistByGuidAsync` | Load a checklist by GUID with items |
| `AddExtensionAsync` | Create an extension request |
| `GetExtensionByGuidAsync` | Load an extension by GUID |
| `AddDamageReportsAsync` | Persist damage report entries |
| `SaveChangesAsync` | Flush all pending changes (unit of work) |

The repository is implemented via EF Core with PostgreSQL (Npgsql) using snake_case naming conventions and NodaTime for timestamps.

---

## Security Model

- All endpoints are protected with `[Authorize]` (Keycloak JWT).
- `ActionInput.ActorKcId` is decorated with `[JsonIgnore]` — it is **never** deserialized from the request body.
- The controller's `SetActor()` method **always overwrites** `ActorKcId` from the JWT `sub` claim (or `NameIdentifier` fallback).
- This ensures the audit trail always reflects the authenticated user, regardless of what a client sends.

---

## Adding a New Action

1. **Add an enum value** to `RentalActionType`.
2. **Create a handler** inheriting `RentalActionHandlerBase<TInput>`:
   - Define a `TInput` record extending `ActionInput`.
   - Set `ActionType` and `AllowedStages`.
   - Implement `BeforeExecuteAsync` (validation), `ExecuteAsync` (logic), and optionally `AfterExecuteAsync` (side-effects).
3. **Register the stage mapping** in `RentalActionRegistry.StageActions`.
4. **Add an endpoint** to `RentalActionController`.
5. **Register the handler** in DI (`DiRegistration.cs`) as `IRentalActionHandler`.
6. **Write tests** following the established patterns in `HandlerMetadataTests`, `HandlerValidationTests`, and `HandlerExecutionTests`.

---

## Test Suite

The test suite covers 3 layers with 61+ handler-specific tests:

### HandlerMetadataTests (46 test cases)
Verifies that every handler declares the correct `ActionType` and `AllowedStages` set. Uses `[Theory]` with `[MemberData]` to iterate all 23 handlers × 2 properties.

### HandlerValidationTests (13 test cases)
Tests `BeforeExecuteAsync` validation logic:
- Date validation (e.g., `CreateRental` requires `RequestedStart < RequestedEnd`)
- Required field validation (e.g., `AssignItems` requires at least one GUID)
- Linked-entity validation (e.g., `GenerateChecklist` requires a linked rental)
- Numeric validation (e.g., `RecordPayment` rejects zero/negative amounts)

### HandlerExecutionTests (32 test cases)
Tests `ExecuteAsync` business logic:
- Stage transitions for simple handlers (Approve/Reject/Complete/Cancel/Scrap)
- Repository interactions for complex handlers (Create, Assign, Remove)
- Checklist scanning, signing, and edge cases (already-signed, unscanned items, item not found)
- Extension approval/rejection and already-reviewed guards
- Damage recording and report generation

### RentalActionServiceTests (9 test cases)
Tests the orchestrator:
- Process not found → `NotFoundException`
- Action not allowed in stage → `ValidationException`
- No handler registered → `ValidationException`
- Successful stage transition
- Failed `BeforeExecuteAsync` aborts the action
- Stage unchanged when handler signals no transition
- `CreateProcessAsync` bootstraps a new process
- `GetAvailableActionsAsync` returns correct actions per stage

### Test Infrastructure
- **NSubstitute** for mocking `IRentalProcessRepository` and `IInventoryRepository`
- **EF Core InMemory** provider for `AppDbContext` (concrete class, cannot be proxied)
- **Real `StockBindingService`** constructed with a mocked `IInventoryRepository`
- **`HandlerTestHelper`** factory methods for creating test objects (`CreateProcess`, `CreateChecklist`, `CreatePendingExtension`, `CreateStockBindingService`, `CreateInMemoryDbContext`)

---

## File Structure

```
LumenForgeServer/Rentals/
├── Actions/
│   ├── ActionInput.cs                  # Base input DTO ([JsonIgnore] ActorKcId)
│   ├── ActionResult.cs                 # Ok/Fail result with optional NewStage
│   ├── IRentalActionHandler.cs         # Non-generic handler interface
│   ├── IRentalActionRegistry.cs        # Stage→Actions mapping contract
│   ├── RentalActionHandlerBase.cs      # Generic base class (TInput deserialization)
│   ├── RentalActionRegistry.cs         # Static stage→actions lookup table
│   ├── RentalActionService.cs          # Central orchestrator
│   ├── RentalActionType.cs             # 23-value enum
│   └── Handlers/
│       ├── ApproveExtensionHandler.cs
│       ├── ApproveItemsHandler.cs
│       ├── ApproveRequestHandler.cs
│       ├── AssignItemsHandler.cs
│       ├── CancelRentalHandler.cs
│       ├── CompleteRentalHandler.cs
│       ├── CreateMaintenanceJobsHandler.cs
│       ├── CreateRentalHandler.cs
│       ├── GenerateChecklistHandler.cs
│       ├── GenerateInvoiceHandler.cs
│       ├── GenerateReportHandler.cs
│       ├── RecordDamagesHandler.cs
│       ├── RecordPaymentHandler.cs
│       ├── RecordPickupHandler.cs
│       ├── RecordReturnHandler.cs
│       ├── RejectExtensionHandler.cs
│       ├── RejectItemsHandler.cs
│       ├── RejectRequestHandler.cs
│       ├── RemoveItemsHandler.cs
│       ├── RequestExtensionHandler.cs
│       ├── ScanChecklistHandler.cs
│       ├── ScrapRentalHandler.cs
│       └── SignChecklistHandler.cs
├── Controller/
│   └── RentalActionController.cs       # 24 HTTP endpoints
├── Domain/
│   ├── Checklist.cs
│   ├── ChecklistItem.cs
│   ├── Rental.cs
│   ├── RentalActionLog.cs
│   ├── RentalDamageReport.cs
│   ├── RentalExtension.cs
│   ├── RentalProcessInstance.cs
│   └── RentalStage.cs
└── Persistence/
    ├── IRentalProcessRepository.cs
    └── RentalProcessRepository.cs

LumenForgeServer.IntegrationTests/Rentals/
└── Actions/
    ├── Helpers/
    │   └── HandlerTestHelper.cs
    ├── Handlers/
    │   ├── HandlerMetadataTests.cs
    │   ├── HandlerValidationTests.cs
    │   └── HandlerExecutionTests.cs
    └── RentalActionServiceTests.cs
```
