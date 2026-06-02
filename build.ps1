$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root

try {
    dotnet restore .\MinimalSnapTimer.sln
    dotnet build .\MinimalSnapTimer.sln -c Release
    dotnet test .\tests\MinimalSnapTimer.Tests\MinimalSnapTimer.Tests.csproj -c Release --no-build
}
finally {
    Pop-Location
}
