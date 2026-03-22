using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// High-level statistics for the rental module.
/// </summary>
public sealed record RentalOverviewDto
{
    /// <summary>Total number of rental processes.</summary>
    [JsonPropertyName("total_processes")]
    public int TotalProcesses { get; init; }

    /// <summary>Number of processes per stage.</summary>
    [JsonPropertyName("by_stage")]
    public required IReadOnlyDictionary<RentalStage, int> ByStage { get; init; }

    /// <summary>Number of processes that are in an active (non-terminal) stage.</summary>
    [JsonPropertyName("active_count")]
    public int ActiveCount { get; init; }

    /// <summary>Number of processes in a terminal stage (Completed, Cancelled, Scrapped).</summary>
    [JsonPropertyName("terminal_count")]
    public int TerminalCount { get; init; }

    /// <summary>Total number of damage reports across all processes.</summary>
    [JsonPropertyName("total_damage_reports")]
    public int TotalDamageReports { get; init; }

    /// <summary>Total number of extension requests across all processes.</summary>
    [JsonPropertyName("total_extension_requests")]
    public int TotalExtensionRequests { get; init; }

    /// <summary>Number of extensions that are still pending review.</summary>
    [JsonPropertyName("pending_extensions")]
    public int PendingExtensions { get; init; }

    /// <summary>Total number of action log entries (audit events).</summary>
    [JsonPropertyName("total_action_logs")]
    public int TotalActionLogs { get; init; }
}

/// <summary>
/// Breakdown of recent activity within a configurable time window.
/// </summary>
public sealed record RentalRecentActivityDto
{
    /// <summary>Number of processes created in the window.</summary>
    [JsonPropertyName("processes_created")]
    public int ProcessesCreated { get; init; }

    /// <summary>Number of processes completed in the window.</summary>
    [JsonPropertyName("processes_completed")]
    public int ProcessesCompleted { get; init; }

    /// <summary>Number of processes cancelled in the window.</summary>
    [JsonPropertyName("processes_cancelled")]
    public int ProcessesCancelled { get; init; }

    /// <summary>Number of action log entries recorded in the window.</summary>
    [JsonPropertyName("actions_performed")]
    public int ActionsPerformed { get; init; }

    /// <summary>Number of damage reports filed in the window.</summary>
    [JsonPropertyName("damages_reported")]
    public int DamagesReported { get; init; }

    /// <summary>ISO-8601 duration of the time window (e.g. "7" for 7 days).</summary>
    [JsonPropertyName("window_days")]
    public int WindowDays { get; init; }
}

/// <summary>
/// Per-stage count entry used in breakdown lists.
/// </summary>
public sealed record StageBucketDto
{
    [JsonPropertyName("stage")]
    public RentalStage Stage { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
