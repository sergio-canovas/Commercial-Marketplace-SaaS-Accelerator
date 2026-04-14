using System;
using System.Text.Json.Serialization;

namespace Marketplace.SaaS.Accelerator.Services.Models;

/// <summary>
/// Minimal entitlement payload returned to tenant-bound callers.
/// </summary>
public class TenantEntitlementResult
{
    /// <summary>
    /// Gets or sets the normalized entitlement status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets the marketplace plan identifier.
    /// </summary>
    [JsonPropertyName("planId")]
    public string PlanId { get; set; }

    /// <summary>
    /// Gets or sets the subscription expiration date in UTC when present.
    /// </summary>
    [JsonPropertyName("expiresOn")]
    public DateTime? ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the number of days left in the trial (only applicable when status is 'trial').
    /// </summary>
    [JsonPropertyName("trialDaysLeft")]
    public int? TrialDaysLeft { get; set; }

    /// <summary>
    /// Gets or sets an optional message (e.g., expiration warning).
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the AMP subscription identifier.
    /// </summary>
    [JsonIgnore]
    public Guid? SubscriptionId { get; set; }
}
