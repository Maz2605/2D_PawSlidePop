param(
    [string]$InputFile = "Assets/_PawSlidePopGame/Docs/game-flow.mmd",
    [string]$OutputFile = "Assets/_PawSlidePopGame/Docs/game-flow.png",
    [string]$BackgroundColor = "white"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$inputPath = Join-Path $root $InputFile
$outputPath = Join-Path $root $OutputFile

if (!(Test-Path $inputPath)) {
    throw "Input file not found: $inputPath"
}

npx -y @mermaid-js/mermaid-cli `
    -i $inputPath `
    -o $outputPath `
    -b $BackgroundColor

Write-Host "Generated: $outputPath"
