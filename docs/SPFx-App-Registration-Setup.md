# SPFx App Registration Setup for Entitlements API

Use the existing multi-tenant `CustomerSite` App Registration as both:

- the interactive sign-in app for the Accelerator
- the API resource for `GET /api/entitlements/tenant`

## 1. Expose the existing App Registration as an API

Run:

```powershell
pwsh .\deployment\Configure-EntitlementsApi.ps1 -AppId "<MTClientId>"
```

Optional parameters:

```powershell
pwsh .\deployment\Configure-EntitlementsApi.ps1 `
  -AppId "<MTClientId>" `
  -IdentifierUri "api://<MTClientId>" `
  -ScopeValue "Entitlements.Read"
```

The script will:

- set the Application ID URI
- create the delegated scope if it does not already exist
- preserve existing exposed scopes where possible

## 2. Values to keep for the SPFx side

After the script runs, keep these values:

- API resource URI: `api://<MTClientId>` or the custom `IdentifierUri` you selected
- Scope name: `Entitlements.Read`
- Endpoint URL: `https://<your-customer-site>/api/entitlements/tenant`

## 3. What the endpoint accepts

The entitlement endpoint accepts:

- existing signed-in `CustomerSite` session cookies
- bearer tokens with audience matching either:
  - `<MTClientId>`
  - `api://<MTClientId>`
  - `<ClientId>`
  - `api://<ClientId>`

That gives you some flexibility if the token ends up using GUID audience or Application ID URI audience.

## 4. SPFx follow-up

When you later authorize the SPFx package, it will need permission to the exposed delegated scope on this App Registration. In practice that means:

- request the custom API permission from the SPFx solution
- approve it in the SharePoint Admin Center API access page
- call the entitlement endpoint with `AadHttpClient`

## 5. Security notes

- The endpoint compares the caller tenant claim (`tid`) with the `tenantId` query string.
- A signed-in user from tenant A cannot ask for tenant B.
- Unknown tenants return `inactive`.
