param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe',
    [string]$OutputDirectory = 'Builds\Android',
    [switch]$SkipChecks
)

$ErrorActionPreference = 'Stop'
$projectSettingsPath = Join-Path $ProjectRoot 'ProjectSettings\ProjectSettings.asset'
$projectSettings = Get-Content -LiteralPath $projectSettingsPath -Raw -Encoding utf8
$versionNameMatch = [regex]::Match($projectSettings, '(?m)^\s*bundleVersion:\s*(\S+)\s*$')
$versionCodeMatch = [regex]::Match($projectSettings, '(?m)^\s*AndroidBundleVersionCode:\s*(\d+)\s*$')
$packageNameMatch = [regex]::Match($projectSettings, '(?ms)^\s*applicationIdentifier:\s*\r?\n\s+Android:\s*(\S+)\s*$')
if (-not $versionNameMatch.Success -or -not $versionCodeMatch.Success -or -not $packageNameMatch.Success) {
    throw "Could not read Android identity from $projectSettingsPath"
}
$versionName = $versionNameMatch.Groups[1].Value
$versionCode = $versionCodeMatch.Groups[1].Value
$packageName = $packageNameMatch.Groups[1].Value
$outputRoot = Join-Path $ProjectRoot $OutputDirectory
$logRoot = Join-Path $ProjectRoot 'Logs'
$logPath = Join-Path $logRoot 'android-apk-build.log'

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity editor not found: $UnityPath"
}

New-Item -ItemType Directory -Force -Path $outputRoot, $logRoot | Out-Null
$existingApkPaths = @(
    Get-ChildItem -LiteralPath $outputRoot -Filter 'PenguinRun-Test-v*.apk' -File |
        ForEach-Object { $_.FullName }
)

if (-not $SkipChecks) {
    foreach ($script in @(
        'VerifyUiArtP0.ps1',
        'VerifyStorybookUiPhase1.ps1',
        'VerifyRunDepthPhase2.ps1',
        'VerifyLongTermGrowthPhase3.ps1',
        'VerifyPremiumStorybookHome.ps1',
        'VerifyImage2Art.ps1',
        'VerifyCustomMapAssetsBatch1.ps1',
        'VerifyThemeMechanicsPhase4.ps1',
        'VerifyPlayerExperienceAudit.ps1'
    )) {
        & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $script) -ProjectRoot $ProjectRoot
        if ($LASTEXITCODE -ne 0) { throw "Pre-build verification failed: $script" }
    }
}

$arguments = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', ('"' + $ProjectRoot + '"'),
    '-executeMethod', 'PenguinRunCiBuild.BuildAndroidApkForTesting',
    '-logFile', ('"' + $logPath + '"')
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    $tail = if (Test-Path -LiteralPath $logPath) { (Get-Content -LiteralPath $logPath -Tail 80) -join [Environment]::NewLine } else { '' }
    throw "Unity APK build failed with exit code $($process.ExitCode).`n$tail"
}

$apk = Get-ChildItem -LiteralPath $outputRoot -Filter 'PenguinRun-Test-v*.apk' -File |
    Where-Object { $existingApkPaths -notcontains $_.FullName } |
    Sort-Object Name -Descending |
    Select-Object -First 1
if ($null -eq $apk) { throw "Unity exited successfully but no new APK was produced in $outputRoot" }

$androidRoot = Join-Path (Split-Path $UnityPath -Parent) 'Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\36.0.0'
$aapt = Join-Path $androidRoot 'aapt.exe'
$apksigner = Join-Path $androidRoot 'apksigner.bat'
if (-not (Test-Path -LiteralPath $aapt) -or -not (Test-Path -LiteralPath $apksigner)) {
    throw "Android build-tools 36.0.0 were not found under the Unity editor."
}

$badging = (& $aapt dump badging $apk.FullName 2>&1) -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0) { throw "aapt could not inspect $($apk.FullName)`n$badging" }
if ($badging -notmatch "package: name='$([regex]::Escape($packageName))'") { throw "Unexpected package id.`n$badging" }
if ($badging -notmatch "versionCode='$versionCode'") { throw "Unexpected versionCode.`n$badging" }
if ($badging -notmatch "versionName='$([regex]::Escape($versionName))'") { throw "Unexpected versionName.`n$badging" }

& $apksigner verify --verbose --print-certs $apk.FullName
if ($LASTEXITCODE -ne 0) { throw "APK signature verification failed: $($apk.FullName)" }

$stablePath = Join-Path $outputRoot "PenguinRun-Latest-Test-v$versionName-$versionCode.apk"
Copy-Item -LiteralPath $apk.FullName -Destination $stablePath -Force
$hash = (Get-FileHash -LiteralPath $stablePath -Algorithm SHA256).Hash
$sizeMb = [math]::Round((Get-Item -LiteralPath $stablePath).Length / 1MB, 2)

Write-Output "APK=$stablePath"
Write-Output "PACKAGE=$packageName"
Write-Output "VERSION=$versionName ($versionCode)"
Write-Output "SIZE_MB=$sizeMb"
Write-Output "SHA256=$hash"
