# Tenant Entitlements Endpoint

This customization adds a minimal endpoint to `CustomerSite`:

```http
GET /api/entitlements/tenant?tenantId=<guid>
```

Response shape:

```json
{
  "status": "active",
  "planId": "basic",
  "expiresOn": "2026-12-31T00:00:00Z"
}
```

## How it works

- Reads the existing `Subscriptions` table managed by the SaaS Accelerator landing page and webhooks.
- Resolves the caller tenant using `Subscriptions.PurchaserTenantId`.
- Normalizes accelerator statuses to the external contract:
  - `Subscribed` + `IsActive = true` -> `active`
  - `PendingFulfillmentStart` or `PendingActivation` -> `pending`
  - anything else -> `inactive`
- If multiple subscriptions exist for the same tenant, the endpoint prefers:
  - active over pending over inactive
  - then the most recently modified subscription

## Authentication model

The endpoint supports two access modes:

- Existing authenticated session cookie in `CustomerSite`
- Bearer token whose audience matches the existing Accelerator App Registration

No separate app registration is required for this customization, but the existing App Registration must be exposed as an API so SPFx can later request a token for it.

## App Registration setup

In the multi-tenant App Registration already used by `CustomerSite`:

1. Expose an API.
2. Set the Application ID URI.
3. Create an application scope for SPFx callers, for example `Entitlements.Read`.
4. Grant the SharePoint Online Client Extensibility Web Application permission to that scope when wiring the SPFx side.

The token validator currently accepts audiences matching:

- `SaaSApiConfiguration:MTClientId`
- `api://SaaSApiConfiguration:MTClientId`
- `SaaSApiConfiguration:ClientId`
- `api://SaaSApiConfiguration:ClientId`

## Notes

- The endpoint intentionally returns the minimum contract required for external entitlement checks.
- It does not mutate subscription state.
- It does not bypass or replace Accelerator webhook validation.
