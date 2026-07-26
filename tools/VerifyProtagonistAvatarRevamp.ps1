param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativePath))) {
        $failures.Add("Missing: $relativePath")
    }
}

function Require-Pattern([string]$relativePath, [string]$pattern, [string]$label) {
    $fullPath = Join-Path $ProjectRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath) -or
        (Get-Content -Raw -Encoding utf8 -LiteralPath $fullPath) -notmatch $pattern) {
        $failures.Add("Missing contract: $label")
    }
}

foreach ($file in @(
    'Assets/Scripts/Runtime/PenguinRun/Avatar/IPlayerCharacterPresentation.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarState.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAvatarDefinition.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationSet.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAppearanceSet.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAccessorySocketSet.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs',
    'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerCharacterFx.cs',
    'Assets/Scripts/Editor/PenguinRun/ProjectRunnerAvatarValidation.cs',
    'Assets/Scripts/Editor/PenguinRun/ProjectAvatarGameplayViewCapture.cs',
    'Assets/Scripts/Editor/PenguinRun/RunnerAvatarAssetBuilder.cs',
    'tools/blender/build_gugugaga_avatar.py',
    'Assets/Shaders/GugugagaStylized.shader',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_avatar.fbx',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod1.fbx',
    'Assets/Resources/PenguinRun/Characters/Gugugaga/gugugaga_lod2.fbx'
)) {
    Require-File $file
}

Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs' `
    'public float VerticalVelocity\s*=>' 'read-only vertical velocity'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs' `
    'PlayerAvatarPresenter\.Create' 'presenter creation entry point'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/PlayerAvatarPresenter.cs' `
    'PenguinCharacter\.CreateCute' 'procedural fallback'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs' `
    'applyRootMotion\s*=\s*false' 'root motion disabled'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs' `
    'Animator\[\]' 'all LOD animators are synchronized'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Avatar/RunnerAnimationDriver.cs' `
    'Rebind\(\)' 'animator rebind after controller swap'
Require-Pattern 'Assets/Scripts/Editor/PenguinRun/ProjectPenguinPreviewCapture.cs' `
    'MinPoseMeanAbsDiff' 'intra-run pose pixel difference gate'
Require-Pattern 'Assets/Scripts/Editor/PenguinRun/ProjectPenguinPreviewCapture.cs' `
    'AssertPose' 'same-camera Idle vs action pose assertion'
# The avatar spends the whole run with its back to the camera. A world-space key
# direction leaves that side on the ambient floor, which is what made the scarf and
# goggles vanish into the body at play distance. Keep the light view-relative.
Require-Pattern 'Assets/Shaders/GugugagaStylized.shader' `
    'UNITY_MATRIX_V' 'view-relative key light'
Require-Pattern 'Assets/Scripts/Editor/PenguinRun/ProjectAvatarGameplayViewCapture.cs' `
    'MinMeanLuma' 'gameplay-distance lighting gate'
Require-Pattern 'Assets/Scripts/Editor/PenguinRun/ProjectAvatarGameplayViewCapture.cs' `
    'MinDetailStdDev' 'gameplay-distance silhouette detail gate'
# Surface patches are clipped to the host outline; without this the belly and face
# mask poke past the body in profile as detached pale ellipses.
Require-Pattern 'tools/blender/build_gugugaga_avatar.py' `
    'def conform_inside' 'surface patches clipped to host outline'

Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs' `
    'BodyHeight' 'crouch hitbox body height retained'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs' `
    'StandingBodyHeight' 'standing body height retained'
Require-Pattern 'Assets/Scripts/Runtime/PenguinRun/Systems/PlayerMotor.cs' `
    'SlidingBodyHeight' 'sliding body height retained'

$avatarRuntime = Join-Path $ProjectRoot 'Assets/Scripts/Runtime/PenguinRun/Avatar'
if (Test-Path -LiteralPath $avatarRuntime) {
    $forbidden = Get-ChildItem -LiteralPath $avatarRuntime -Recurse -Filter '*.cs' |
        Select-String -Pattern 'applyRootMotion\s*=\s*true' -SimpleMatch:$false
    if ($forbidden) {
        $failures.Add('Forbidden: applyRootMotion = true under avatar runtime')
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    exit 1
}

Write-Host 'PASS: protagonist avatar revamp contracts hold.'
