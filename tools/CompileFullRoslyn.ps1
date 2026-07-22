# Full Roslyn compile gate: reuse refs/defines from _tmp/storybook-compile.rsp as template,
# re-enumerate all .cs under Assets/Scripts, compile Assembly-CSharp (compile check only).
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$UnityEditorDir = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor'
)

$ErrorActionPreference = 'Stop'
Set-Location $ProjectRoot

$templateRsp = Join-Path $ProjectRoot '_tmp/storybook-compile.rsp'
if (-not (Test-Path $templateRsp)) {
    Write-Error "Missing template rsp: $templateRsp"
    exit 1
}

$outRsp = Join-Path $ProjectRoot '_tmp/full-compile.rsp'
$outLog = Join-Path $ProjectRoot '_tmp/full-compile.log'

# 1) Keep all non-source lines (flags/defines/references/additionalfile), swap output paths
$header = Get-Content -LiteralPath $templateRsp -Encoding utf8 |
    Where-Object { $_ -notmatch '\.cs"?\s*$' } |
    ForEach-Object {
        if ($_ -match '^-out:') { '-out:"_tmp/Assembly-CSharp-full.dll"' }
        elseif ($_ -match '^-refout:') { '-refout:"_tmp/Assembly-CSharp-full.ref.dll"' }
        else { $_ }
    }

# 1b) Append package assemblies resolved after the template was captured
$extraRefs = @(
    'Library/Bee/artifacts/1300b0aE.dag/Unity.Postprocessing.Runtime.ref.dll'
)
foreach ($ref in $extraRefs) {
    if ((Test-Path (Join-Path $ProjectRoot $ref)) -and ($header -notcontains ('-r:"' + $ref + '"'))) {
        $header += ('-r:"' + $ref + '"')
    }
}

# 2) Enumerate every script source (Assets/Scripts, including Editor: template defines UNITY_EDITOR)
$sources = Get-ChildItem -Path (Join-Path $ProjectRoot 'Assets/Scripts') -Recurse -Filter *.cs |
    ForEach-Object { '"' + ($_.FullName.Substring($ProjectRoot.Length + 1) -replace '\\', '/') + '"' }

$header + $sources | Set-Content -LiteralPath $outRsp -Encoding utf8

# 3) Invoke Unity bundled Roslyn
$dotnet = Join-Path $UnityEditorDir 'Data\NetCoreRuntime\dotnet.exe'
$csc = Join-Path $UnityEditorDir 'Data\DotNetSdkRoslyn\csc.dll'
& $dotnet $csc ('@' + $outRsp) *> $outLog
$exit = $LASTEXITCODE

if ($exit -ne 0) {
    Write-Host "COMPILE FAILED (exit $exit). Errors:" -ForegroundColor Red
    Get-Content $outLog | Where-Object { $_ -match 'error' } | Select-Object -First 60
} else {
    Write-Host "COMPILE OK ($((@($sources)).Count) sources)" -ForegroundColor Green
}
exit $exit
