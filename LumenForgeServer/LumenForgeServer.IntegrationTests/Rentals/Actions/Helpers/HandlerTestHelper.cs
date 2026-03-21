using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Persistance;
using LumenForgeServer.Inventory.Service;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.IntegrationTests.Rentals.Actions.Helpers;

/// <summary>
/// Factory methods for creating domain objects used across handler tests.
/// </summary>
internal static class HandlerTestHelper
{
    private static long _idCounter;

    /// <summary>Creates a substitute <see cref="StockBindingService"/> with a mocked repository.</summary>
    public static StockBindingService CreateStockBindingService()
        => new(Substitute.For<IInventoryRepository>());

    /// <summary>Creates a real <see cref="AppDbContext"/> backed by an in-memory database.</summary>
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Creates a <see cref="RentalProcessInstance"/> in the given stage with a linked rental.</summary>
    public static RentalProcessInstance CreateProcess(
        RentalStage stage = RentalStage.None,
        bool withRental = true)
    {
        var id = Interlocked.Increment(ref _idCounter);
        var now = SystemClock.Instance.GetCurrentInstant();

        var process = new RentalProcessInstance
        {
            Id = id,
            Guid = Guid.NewGuid(),
            CurrentStage = stage,
            CreatedByKcId = "test-user-kc-id",
            CreatedAt = now,
            UpdatedAt = now
        };

        if (withRental)
        {
            process.RentalId = id;
            process.Rental = new Rental
            {
                Id = id,
                Uuid = Guid.NewGuid(),
                CustomerKcId = "test-customer-kc-id",
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                Purpose = "Unit testing",
                RequestedStart = now,
                RequestedEnd = now + Duration.FromDays(7),
                Notes = null,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        return process;
    }

    /// <summary>Creates a checklist with the given number of items.</summary>
    public static Checklist CreateChecklist(long processInstanceId, int itemCount = 3, bool allScanned = false)
    {
        var checklist = new Checklist
        {
            Id = Interlocked.Increment(ref _idCounter),
            Guid = Guid.NewGuid(),
            ProcessInstanceId = processInstanceId,
            ChecklistType = Common.ChecklistType.PICKUP,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            Items = Enumerable.Range(0, itemCount).Select(i => new ChecklistItem
            {
                Id = Interlocked.Increment(ref _idCounter),
                Guid = Guid.NewGuid(),
                StockBindingGuid = Guid.NewGuid(),
                DeviceName = $"Device-{i}",
                IsScanned = allScanned,
                ScannedValue = allScanned ? $"SN-{i}" : null,
                ScannedByKcId = allScanned ? "test-user-kc-id" : null,
                ScannedAt = allScanned ? SystemClock.Instance.GetCurrentInstant() : null
            }).ToList()
        };

        return checklist;
    }

    /// <summary>Creates a <see cref="RentalExtension"/> that has not yet been reviewed.</summary>
    public static RentalExtension CreatePendingExtension(long processInstanceId, Instant originalEnd)
    {
        return new RentalExtension
        {
            Id = Interlocked.Increment(ref _idCounter),
            Guid = Guid.NewGuid(),
            ProcessInstanceId = processInstanceId,
            NewRequestedEnd = originalEnd + Duration.FromDays(7),
            OriginalEnd = originalEnd,
            Reason = "Need more time",
            IsApproved = null,
            RequestedByKcId = "test-customer-kc-id",
            RequestedAt = SystemClock.Instance.GetCurrentInstant()
        };
    }
}
