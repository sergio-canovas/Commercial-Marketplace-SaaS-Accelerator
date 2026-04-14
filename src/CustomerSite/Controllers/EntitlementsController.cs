using Marketplace.SaaS.Accelerator.CustomerSite.Security;
using Marketplace.SaaS.Accelerator.Services.Contracts;
using Marketplace.SaaS.Accelerator.Services.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Marketplace.SaaS.Accelerator.CustomerSite.Controllers;

/// <summary>
/// Exposes a minimal tenant entitlement API backed by accelerator subscription data.
/// </summary>
[Route("api/entitlements")]
[ApiController]
[IgnoreAntiforgeryToken]
public class EntitlementsController : ControllerBase
{
    private readonly ITenantEntitlementService tenantEntitlementService;
    private readonly EntitlementAccessTokenValidator accessTokenValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitlementsController"/> class.
    /// </summary>
    /// <param name="tenantEntitlementService">The entitlement service.</param>
    /// <param name="accessTokenValidator">The access token validator.</param>
    public EntitlementsController(
        ITenantEntitlementService tenantEntitlementService,
        EntitlementAccessTokenValidator accessTokenValidator)
    {
        this.tenantEntitlementService = tenantEntitlementService;
        this.accessTokenValidator = accessTokenValidator;
    }

    /// <summary>
    /// Gets the normalized entitlement for a purchaser tenant.
    /// </summary>
    /// <param name="tenantId">The purchaser tenant identifier.</param>
    /// <returns>Minimal entitlement payload.</returns>
    [HttpGet("tenant")]
    public async Task<ActionResult<TenantEntitlementResult>> GetTenantEntitlement([FromQuery] Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Invalid tenantId",
                Detail = "Query parameter tenantId must be a non-empty GUID."
            });
        }

        ClaimsPrincipal principal = this.User.Identity?.IsAuthenticated ?? false
            ? this.User
            : await this.TryAuthenticateBearerTokenAsync();

        if (principal == null)
        {
            return this.Unauthorized();
        }

        var tokenTenantId = GetTenantId(principal);
        if (tokenTenantId == null || tokenTenantId.Value != tenantId)
        {
            return this.StatusCode(403);
        }

        var entitlement = this.tenantEntitlementService.GetByTenantId(tenantId);
        return this.Ok(entitlement);
    }

    private async Task<ClaimsPrincipal> TryAuthenticateBearerTokenAsync()
    {
        if (!this.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return null;
        }

        var authorizationHeader = authorizationHeaderValues.ToString();
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();

        try
        {
            return await this.accessTokenValidator.ValidateAsync(token);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var tenantClaim =
            principal.FindFirst("tid")?.Value ??
            principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
    }
}
