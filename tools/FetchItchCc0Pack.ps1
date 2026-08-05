# Helper to claim a free itch.io asset pack download URL (opens browser).
# Automated /file POST currently returns invalid key from this environment;
# use this script to get a signed download page, then click Download manually.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('kaykit-forest', 'stylized-nature', 'ultimate-nature')]
    [string]$Pack
)

$ErrorActionPreference = 'Stop'
$map = @{
    'kaykit-forest'   = 'https://kaylousberg.itch.io/kaykit-forest'
    'stylized-nature' = 'https://quaternius.itch.io/stylized-nature-megakit'
    'ultimate-nature' = 'https://quaternius.itch.io/150-lowpoly-nature-models'
}
$slug = $map[$Pack]
$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$r1 = Invoke-WebRequest -Uri "$slug/purchase" -WebSession $session -Headers @{ 'User-Agent' = $ua } -UseBasicParsing
$csrf = ([regex]::Match($r1.Content, 'name="csrf_token"\s+value="([^"]+)"')).Groups[1].Value
$r2 = Invoke-WebRequest -Uri "$slug/download_url" -Method POST -WebSession $session -Headers @{ 'User-Agent' = $ua } -Body @{ price = 0; csrf_token = $csrf } -UseBasicParsing
$url = ($r2.Content | ConvertFrom-Json).url
Write-Host "Open this URL and click Download:"
Write-Host $url
Start-Process $url
exit 0
