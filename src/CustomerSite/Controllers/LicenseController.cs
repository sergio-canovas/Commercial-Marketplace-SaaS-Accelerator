// Copyright (c) dForts. All rights reserved.

using System;
using System.Linq;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.SaaS.Accelerator.CustomerSite.Controllers;

/// <summary>
/// Public API endpoint consumed by SPFx web parts to validate tenant license.
/// No authentication required — only returns plan tier, no sensitive data.
/// </summary>
[Route("api/license")]
[ApiController]
[EnableCors("SharePointWebParts")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class LicenseController : ControllerBase
{
    private readonly ISubscriptionsRepository _subscriptionsRepository;

    public LicenseController(ISubscriptionsRepository subscriptionsRepository)
    {
        _subscriptionsRepository = subscriptionsRepository;
    }

    /// <summary>
    /// Returns the active license plan for a given purchaser tenant.
    /// </summary>
    /// <param name="tenantId">The Microsoft 365 tenant ID (GUID).</param>
    /// <returns>{ plan: "standard" | "premium" | "none", validUntil, planId }</returns>
    [HttpGet("{tenantId:guid}")]
    public IActionResult Get(Guid tenantId)
    {
        var active = _subscriptionsRepository
            .GetSubscriptionsByPurchaserTenantId(tenantId)
            .Where(s => s.IsActive == true && s.SubscriptionStatus == "Subscribed")
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (active == null)
        {
            return Ok(new { plan = "none", validUntil = (DateTime?)null, planId = (string)null });
        }

        var planId = active.AmpplanId ?? string.Empty;
        var plan = planId.Contains("premium", StringComparison.OrdinalIgnoreCase)
            ? "premium"
            : "standard";

        return Ok(new
        {
            plan,
            validUntil = active.EndDate,
            planId
        });
    }
}
