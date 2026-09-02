param(
    [string]$PlayerPath
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($PlayerPath)) {
    $PlayerPath = Join-Path $workspace 'Builds\Windows\SparkStrikers.exe'
}
$PlayerPath = [System.IO.Path]::GetFullPath($PlayerPath)
$buildRoot = Split-Path -Parent $PlayerPath
$runtimeLog = Join-Path $workspace 'Builds\release-preflight-runtime.log'

Push-Location $workspace
try {
    & dotnet run --project Tools/SmokeTest/SmokeTest.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Rules smoke test failed.' }

    & dotnet run --project Tools/SyntaxCheck/SyntaxCheck.csproj -- Assets
    if ($LASTEXITCODE -ne 0) { throw 'C# syntax check failed.' }

    & pwsh -NoProfile -File Tools/GenerateSteamCapsules.ps1
    if ($LASTEXITCODE -ne 0) { throw 'Steam asset verification failed.' }

    $required = @(
        $PlayerPath,
        (Join-Path $buildRoot 'UnityPlayer.dll'),
        (Join-Path $buildRoot 'UnityCrashHandler64.exe'),
        (Join-Path $buildRoot 'MonoBleedingEdge'),
        (Join-Path $buildRoot 'D3D12'),
        (Join-Path $buildRoot 'SparkStrikers_Data\Managed\Assembly-CSharp.dll'),
        (Join-Path $buildRoot 'ThirdPartyNotices.txt')
    )
    foreach ($path in $required) {
        if (-not (Test-Path -LiteralPath $path)) { throw "Missing release file: $path" }
    }

    $sourceNotice = (Get-FileHash -Algorithm SHA256 -LiteralPath 'ThirdPartyNotices.txt').Hash
    $builtNotice = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $buildRoot 'ThirdPartyNotices.txt')).Hash
    if ($sourceNotice -ne $builtNotice) { throw 'Built third-party notices are stale.' }

    $arguments = @('-batchmode', '-nographics', '-smoke-test', '--capture-english', '-logFile', ('"' + $runtimeLog + '"'))
    $process = Start-Process -FilePath $PlayerPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(45000)) {
        $process.Kill()
        throw 'Built-player smoke test timed out.'
    }
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $runtimeLog -SimpleMatch 'Spark Strikers runtime smoke test passed.')) {
        Get-Content -LiteralPath $runtimeLog -Tail 120
        throw "Built-player smoke test failed: $($process.ExitCode)"
    }

    Write-Output 'Spark Strikers release preflight passed.'
} finally {
    Pop-Location
}
