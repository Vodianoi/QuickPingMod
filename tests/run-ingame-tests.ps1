<#
.SYNOPSIS
    Build QuickPing (Debug), start Valheim on the mod profile, run the in-game tests and print the results.

.DESCRIPTION
    The Debug build deploys QuickPing to MOD_DEPLOYPATH (Environment.props). Valheim is started with that
    profile's BepInEx and the -quickping-test argument: the mod loads (or creates) the QuickPingTest character
    and world, runs the tests, writes BepInEx\QuickPingTests.txt and quits.
    Steam must be running. Exit code: 0 when every test passed, 1 otherwise.

.EXAMPLE
    .\tests\run-ingame-tests.ps1
    .\tests\run-ingame-tests.ps1 -NoBuild -TimeoutSeconds 600
#>
param(
    # Valheim folder (default: VALHEIM_INSTALL from Environment.props)
    [string]$ValheimPath,
    # Profile BepInEx folder (default: the parent of MOD_DEPLOYPATH from Environment.props)
    [string]$BepInExPath,
    [int]$TimeoutSeconds = 300,
    [switch]$NoBuild,
    # Keep the game in a 1280x720 window (default) or use your usual display settings
    [switch]$Fullscreen
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot

function Get-EnvironmentProp([string]$name) {
    $file = Join-Path $repo 'Environment.props'
    if (-not (Test-Path $file)) { return $null }
    $node = ([xml](Get-Content $file -Raw)).Project.PropertyGroup.$name
    if ($node) { return "$node".Trim() }
    return $null
}

if (-not $ValheimPath) { $ValheimPath = Get-EnvironmentProp 'VALHEIM_INSTALL' }
if (-not $BepInExPath) {
    $deploy = Get-EnvironmentProp 'MOD_DEPLOYPATH'
    if ($deploy) { $BepInExPath = Split-Path -Parent $deploy }
}
$exe = Join-Path $ValheimPath 'valheim.exe'
$preloader = Join-Path $BepInExPath 'core\BepInEx.Preloader.dll'
if (-not (Test-Path $exe)) { throw "valheim.exe not found in '$ValheimPath' (set VALHEIM_INSTALL in Environment.props or pass -ValheimPath)" }
if (-not (Test-Path $preloader)) { throw "BepInEx not found in '$BepInExPath' (set MOD_DEPLOYPATH in Environment.props or pass -BepInExPath)" }

if (Get-Process valheim -ErrorAction SilentlyContinue) { throw 'Valheim is already running: close it first.' }
if (-not (Get-Process steam -ErrorAction SilentlyContinue)) { Write-Warning 'Steam does not seem to be running: Valheim needs it.' }

if (-not $NoBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild not found (Visual Studio with the .NET desktop workload is needed)' }

    Write-Host 'Building QuickPing (Debug)...'
    & $msbuild (Join-Path $repo 'QuickPing.sln') /restore /p:Configuration=Debug /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
}

$results = Join-Path $BepInExPath 'QuickPingTests.txt'
$log = Join-Path $BepInExPath 'LogOutput.log'
Remove-Item $results -ErrorAction SilentlyContinue

$arguments = @(
    '--doorstop-enabled', 'true',
    '--doorstop-target-assembly', "`"$preloader`"",
    '-console',
    '-quickping-test'
)
if (-not $Fullscreen) { $arguments += '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720' }

Write-Host "Starting Valheim with $BepInExPath ..."
$game = Start-Process -FilePath $exe -WorkingDirectory $ValheimPath -ArgumentList $arguments -PassThru

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$finished = $false
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $results) -and (Select-String -Path $results -Pattern '^RESULT:' -Quiet)) { $finished = $true; break }
    if ($game.HasExited) { break }
    Start-Sleep -Seconds 2
}

if (-not $finished) {
    if (-not $game.HasExited) { Stop-Process -Id $game.Id -Force }
    $reason = if ($game.HasExited) { 'Valheim closed' } else { "Timed out after $TimeoutSeconds s" }
    Write-Host "$reason before the tests finished. Last log lines:" -ForegroundColor Red
    if (Test-Path $log) { Get-Content $log -Tail 40 }
    exit 1
}

# Give the game time to save and quit
if (-not $game.WaitForExit(30000)) { Stop-Process -Id $game.Id -Force }

$failed = $false
foreach ($line in Get-Content $results) {
    if ($line -like 'PASS*') { Write-Host $line -ForegroundColor Green }
    elseif ($line -like 'FAIL*') { Write-Host $line -ForegroundColor Red; $failed = $true }
    elseif ($line -like 'RESULT:*') { Write-Host $line -ForegroundColor $(if ($failed -or $line -notmatch ' 0 failed') { 'Red' } else { 'Green' }) }
    else { Write-Host $line }
}

if ($failed -or -not (Select-String -Path $results -Pattern '^RESULT: [1-9]\d* passed, 0 failed' -Quiet)) { exit 1 }
exit 0
