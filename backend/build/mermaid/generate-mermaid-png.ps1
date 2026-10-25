<#
.SYNOPSIS
    Renders every docs/diagrams/**/*.mermaid file to a .png next to it, so the images in the docs stay in sync with
    their sources.

.DESCRIPTION
    Runs automatically after Debug builds of Quotes.Api on Windows (see the GenerateMermaidDiagrams target in
    Quotes.Api.csproj); MSBuild only invokes it when a .mermaid file is newer than its .png. Run it by hand to
    regenerate everything with -Force.

.NOTES
    Requires the Mermaid CLI:  npm install -g @mermaid-js/mermaid-cli
    Without it the script prints a message and exits successfully, so builds never fail because of documentation.

.EXAMPLE
    ./build/mermaid/generate-mermaid-png.ps1          # only diagrams whose source changed
    ./build/mermaid/generate-mermaid-png.ps1 -Force   # every diagram
#>
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$diagramsDirectory = Resolve-Path (Join-Path $PSScriptRoot '..\..\docs\diagrams')

if (-not (Get-Command mmdc -ErrorAction SilentlyContinue)) {
    # Informational only: MSBuild would turn a PowerShell warning into a build warning.
    Write-Output 'Skipping diagram generation: the Mermaid CLI (mmdc) is not installed (npm install -g @mermaid-js/mermaid-cli).'
    exit 0
}

$rendered = 0

foreach ($file in Get-ChildItem -Path $diagramsDirectory -Recurse -Filter *.mermaid) {
    $outputFile = Join-Path $file.DirectoryName "$($file.BaseName).png"

    if (-not $Force -and (Test-Path $outputFile) -and ((Get-Item $outputFile).LastWriteTimeUtc -ge $file.LastWriteTimeUtc)) {
        continue
    }

    mmdc --input $file.FullName --output $outputFile --backgroundColor white --scale 2 --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "The Mermaid CLI failed for $($file.FullName)."
    }

    Write-Output "Rendered $($file.Name) -> $([System.IO.Path]::GetFileName($outputFile))"
    $rendered++
}

Write-Output "Mermaid diagrams: $rendered rendered."
