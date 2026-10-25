<#
.SYNOPSIS
    Applies src/dbQuotes/up/*.sql with grate as a .NET tool, without Docker.

.DESCRIPTION
    Same scripts and options as the db-migrations service in docker-compose.yml. Use it when PostgreSQL runs
    natively, or to migrate the compose database from the host. Each script runs once; re-running is safe.

.EXAMPLE
    ./scripts/migrate.ps1
    ./scripts/migrate.ps1 -ConnectionString "Host=myhost;Port=5432;Database=advisor_quotes;Username=me;Password=secret"
#>
param(
    [string]$ConnectionString = $env:ConnectionStrings__QuotesDb
)

$ErrorActionPreference = 'Stop'

$defaultConnectionString = 'Host=localhost;Port=5432;Database=advisor_quotes;Username=advisor_quotes;Password=change_me_locally'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = $defaultConnectionString
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repositoryRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed with exit code $LASTEXITCODE" }

    dotnet tool run grate `
        "--connstring=$ConnectionString" `
        --databasetype=postgresql `
        --sqlfilesdirectory=src/dbQuotes `
        --environment=LOCAL `
        --transaction `
        --silent
    if ($LASTEXITCODE -ne 0) { throw "grate failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}
