$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path $root "publish\win-x64"

Push-Location $root

try {
    dotnet publish .\src\MinimalSnapTimer\MinimalSnapTimer.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o $output `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        /p:DebugType=None `
        /p:DebugSymbols=false
}
finally {
    Pop-Location
}
