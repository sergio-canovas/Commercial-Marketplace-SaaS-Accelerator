using System;
using System.Collections.Generic;
using System.Linq;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using Marketplace.SaaS.Accelerator.Services.Contracts;
using Marketplace.SaaS.Accelerator.Services.Models;

namespace Marketplace.SaaS.Accelerator.Services.Services;

/// <summary>
/// Resolves minimal tenant entitlements from accelerator subscription records.
/// </summary>
public class TenantEntitlementService : ITenantEntitlementService
{
    private readonly ISubscriptionsRepository subscriptionsRepository;
    private readonly ITenantTrialRepository tenantTrialRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantEntitlementService"/> class.
    /// </summary>
    /// <param name="subscriptionsRepository">The subscriptions repository.</param>
    /// <param name="tenantTrialRepository">The tenant trial repository.</param>
    public TenantEntitlementService(ISubscriptionsRepository subscriptionsRepository, ITenantTrialRepository tenantTrialRepository)
    {
        this.subscriptionsRepository = subscriptionsRepository;
        this.tenantTrialRepository = tenantTrialRepository;
    }

    /// <inheritdoc />
    public TenantEntitlementResult GetByTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("tenantId cannot be empty.", nameof(tenantId));
        }

        var subscriptions = this.subscriptionsRepository.GetSubscriptionsByPurchaserTenantId(tenantId).ToList();
        var selectedSubscription = SelectBestSubscription(subscriptions);

        // If we don't have an active or pending paid subscription, fallback to Trial logic
        if (selectedSubscription == null || MapStatus(selectedSubscription) == EntitlementStatus.Inactive)
        {
            var trial = this.tenantTrialRepository.GetByTenantId(tenantId);
            if (trial == null)
            {
                // No subscription and no trial started
                return new TenantEntitlementResult
                {
                    Status = "inactive",
                    Message = "No active subscription found for this tenant. Please start a free trial or purchase a plan."
                };
            }

            var daysElapsed = (int)(DateTime.UtcNow - trial.StartDateUtc).TotalDays;
            var trialDaysLeft = Math.Max(0, 15 - daysElapsed);

            if (trialDaysLeft > 0)
            {
                return new TenantEntitlementResult
                {
                    Status = "active_trial",
                    TrialDaysLeft = trialDaysLeft,
                    Message = $"Your 15-day trial is currently active. {trialDaysLeft} days remaining."
                };
            }
            else
            {
                return new TenantEntitlementResult
                {
                    Status = "expired_trial",
                    TrialDaysLeft = 0,
                    Message = "Your 15-day trial has expired. Please purchase a standard or premium subscription to continue using to the app."
                };
            }
        }

        return CreateResult(MapStatus(selectedSubscription), selectedSubscription);
    }

    internal static Subscriptions SelectBestSubscription(IEnumerable<Subscriptions> subscriptions)
    {
        return subscriptions?
            .OrderByDescending(subscription => GetPriority(subscription))
            .ThenByDescending(subscription => subscription.ModifyDate ?? subscription.CreateDate ?? DateTime.MinValue)
            .FirstOrDefault();
    }

    internal static EntitlementStatus MapStatus(Subscriptions subscription)
    {
        if (subscription == null)
        {
            return EntitlementStatus.Inactive;
        }

        var normalizedStatus = subscription.SubscriptionStatus?.Trim();

        if ((subscription.IsActive ?? false) &&
            string.Equals(normalizedStatus, SubscriptionStatusEnum.Subscribed.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return EntitlementStatus.Active;
        }

        if (string.Equals(normalizedStatus, SubscriptionStatusEnum.PendingFulfillmentStart.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, SubscriptionStatusEnum.PendingActivation.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return EntitlementStatus.Pending;
        }

        return EntitlementStatus.Inactive;
    }

    private static int GetPriority(Subscriptions subscription)
    {
        return MapStatus(subscription) switch
        {
            EntitlementStatus.Active => 3,
            EntitlementStatus.Pending => 2,
            _ => 1
        };
    }

    private static TenantEntitlementResult CreateResult(EntitlementStatus status, Subscriptions subscription)
    {
        return new TenantEntitlementResult
        {
            Status = status.ToString().ToLowerInvariant(),
            PlanId = subscription?.AmpplanId,
            ExpiresOn = subscription?.EndDate,
            SubscriptionId = subscription?.AmpsubscriptionId
        };
    }
}
