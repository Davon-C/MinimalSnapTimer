$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$targets = @(
    (Join-Path $root "src\MinimalSnapTimer\bin"),
    (Join-Path $root "src\MinimalSnapTimer\obj"),
    (Join-Path $root "tests\MinimalSnapTimer.Tests\bin"),
    (Join-Path $root "tests\MinimalSnapTimer.Tests\obj"),
    (Join-Path $root "publish"),
    (Join-Path $root "artifacts"),
    (Join-Path $root "TestResults")
)

foreach ($target in $targets) {
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}
