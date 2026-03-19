namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that damage reports were created for returned items.</summary>
public sealed class RecordDamagesAction : RentalAction { }

/// <summary>Records that maintenance jobs were spawned from damage reports.</summary>
public sealed class CreateMaintenanceJobsAction : RentalAction { }

/// <summary>Records that an invoice was generated for the rental.</summary>
public sealed class GenerateInvoiceAction : RentalAction { }

/// <summary>Records that a payment was received against an invoice.</summary>
public sealed class RecordPaymentAction : RentalAction { }

/// <summary>Records that the final rental report was generated.</summary>
public sealed class GenerateReportAction : RentalAction { }
