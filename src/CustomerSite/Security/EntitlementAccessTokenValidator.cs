using Marketplace.SaaS.Accelerator.Services.Configurations;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Marketplace.SaaS.Accelerator.CustomerSite.Security;

/// <summary>
/// Validates bearer tokens intended for the entitlement endpoint.
/// </summary>
public class EntitlementAccessTokenValidator
{
    private readonly SaaSApiClientConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitlementAccessTokenValidator"/> class.
    /// </summary>
    /// <param name="configuration">The SaaS API configuration.</param>
    public EntitlementAccessTokenValidator(SaaSApiClientConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Validates the token and returns the claims principal.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <returns>The validated principal.</returns>
    public async Task<ClaimsPrincipal> ValidateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException("Bearer token is missing.");
        }

        var authorityHost = string.IsNullOrWhiteSpace(this.configuration.AdAuthenticationEndPoint)
            ? "https://login.microsoftonline.com"
            : this.configuration.AdAuthenticationEndPoint.TrimEnd('/');

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{authorityHost}/common/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validAudiences = new List<string>();
        if (!string.IsNullOrWhiteSpace(this.configuration.MTClientId))
        {
            validAudiences.Add(this.configuration.MTClientId);
            validAudiences.Add($"api://{this.configuration.MTClientId}");
        }

        if (!string.IsNullOrWhiteSpace(this.configuration.ClientId) &&
            !validAudiences.Contains(this.configuration.ClientId, StringComparer.OrdinalIgnoreCase))
        {
            validAudiences.Add(this.configuration.ClientId);
            validAudiences.Add($"api://{this.configuration.ClientId}");
        }

        if (validAudiences.Count == 0)
        {
            throw new SecurityTokenException("No valid audiences are configured for entitlement token validation.");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudiences = validAudiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = openIdConfig.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, validationParameters, out _);
    }
}
