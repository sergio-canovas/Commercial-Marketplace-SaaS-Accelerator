using System;
using Marketplace.SaaS.Accelerator.Services.Models;

namespace Marketplace.SaaS.Accelerator.Services.Contracts;

/// <summary>
/// Resolves tenant-level entitlement state from accelerator subscription data.
/// </summary>
public interface ITenantEntitlementService
{
    /// <summary>
    /// Resolves the entitlement for a purchaser tenant.
    /// </summary>
    /// <param name="tenantId">The purchaser tenant identifier.</param>
    /// <returns>The normalized entitlement payload.</returns>
    TenantEntitlementResult GetByTenantId(Guid tenantId);
}
