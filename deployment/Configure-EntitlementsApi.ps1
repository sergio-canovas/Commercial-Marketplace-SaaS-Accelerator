param(
    [Parameter(Mandatory = $true)]
    [string]$AppId,

    [string]$IdentifierUri,

    [string]$ScopeValue = "Entitlements.Read",

    [string]$ScopeDisplayName = "Read tenant entitlements",

    [string]$ScopeDescription = "Allow SPFx clients to read tenant entitlements from the SaaS Accelerator."
)

$ErrorActionPreference = "Stop"

Write-Host "Loading App Registration $AppId ..."
$app = az ad app show --id $AppId | ConvertFrom-Json

if (-not $app) {
    throw "Unable to find App Registration with AppId $AppId."
}

if ([string]::IsNullOrWhiteSpace($IdentifierUri)) {
    $IdentifierUri = "api://$($app.appId)"
}

$existingScopes = @()
if ($null -ne $app.api -and $null -ne $app.api.oauth2PermissionScopes) {
    $existingScopes = @($app.api.oauth2PermissionScopes)
}

$scope = $existingScopes | Where-Object { $_.value -eq $ScopeValue } | Select-Object -First 1
if (-not $scope) {
    $scope = @{
        id = [guid]::NewGuid().Guid
        adminConsentDisplayName = $ScopeDisplayName
        adminConsentDescription = $ScopeDescription
        userConsentDisplayName = $ScopeDisplayName
        userConsentDescription = $ScopeDescription
        isEnabled = $true
        type = "User"
        value = $ScopeValue
    }
    $existingScopes = @($existingScopes) + @($scope)
    Write-Host "Creating new delegated scope $ScopeValue"
}
else {
    Write-Host "Scope $ScopeValue already exists. It will be kept."
}

$normalizedScopes = @()
foreach ($existingScope in $existingScopes) {
    $normalizedScopes += @{
        id = $existingScope.id
        adminConsentDisplayName = $existingScope.adminConsentDisplayName
        adminConsentDescription = $existingScope.adminConsentDescription
        userConsentDisplayName = $existingScope.userConsentDisplayName
        userConsentDescription = $existingScope.userConsentDescription
        isEnabled = [bool]$existingScope.isEnabled
        type = $existingScope.type
        value = $existingScope.value
    }
}

$apiPayload = @{
    requestedAccessTokenVersion = 2
    oauth2PermissionScopes = $normalizedScopes
}

if ($null -ne $app.api) {
    if ($null -ne $app.api.preAuthorizedApplications) {
        $preAuthorizedApplications = @()
        foreach ($preAuth in $app.api.preAuthorizedApplications) {
            $preAuthorizedApplications += @{
                appId = $preAuth.appId
                delegatedPermissionIds = @($preAuth.delegatedPermissionIds)
            }
        }

        if ($preAuthorizedApplications.Count -gt 0) {
            $apiPayload.preAuthorizedApplications = $preAuthorizedApplications
        }
    }

    if ($null -ne $app.api.knownClientApplications) {
        $apiPayload.knownClientApplications = @($app.api.knownClientApplications)
    }
}

$body = @{
    identifierUris = @($IdentifierUri)
    api = $apiPayload
}

$jsonBody = $body | ConvertTo-Json -Depth 20 -Compress

Write-Host "Updating Application ID URI to $IdentifierUri"
az rest `
    --method PATCH `
    --uri "https://graph.microsoft.com/v1.0/applications/$($app.id)" `
    --headers "Content-Type=application/json" `
    --body $jsonBody | Out-Null

Write-Host ""
Write-Host "Completed."
Write-Host "Application Object Id : $($app.id)"
Write-Host "Application (client) Id: $($app.appId)"
Write-Host "Identifier URI        : $IdentifierUri"
Write-Host "Scope value           : $ScopeValue"
Write-Host ""
Write-Host "Use these values later in SPFx:"
Write-Host "  Resource / API URI  : $IdentifierUri"
Write-Host "  Delegated scope     : $ScopeValue"
