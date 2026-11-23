#!/usr/bin/env pwsh
param(
    [switch]$All,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$DotnetArgs = @()
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

if ($All) {
    dotnet test Nexora.slnx @DotnetArgs
    exit $LASTEXITCODE
}

$changed = @(git diff --name-only HEAD) + @(git ls-files --others --exclude-standard)
$filter = "Category!=Slow"

$testFiles = Get-ChildItem tests -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($testFile in $testFiles) {
    $covered = @(
        Select-String -Path $testFile.FullName -Pattern '^\s*\[Trait\("Covers", "([^"]+)"\)\]\s*$' |
            ForEach-Object { $_.Matches[0].Groups[1].Value }
    )

    if ($covered.Count -eq 0) {
        continue
    }

    $relativePath = $testFile.FullName.Substring($repoRoot.Length + 1) -replace '\\', '/'
    $paths = @($relativePath) + $covered
    $isChanged = $changed | Where-Object { $file = $_; $paths | Where-Object { $file.StartsWith($_) } }

    if ($isChanged) {
        Write-Host "Including slow tests in ${relativePath}: its covered code changed."
        $filter += ($covered | ForEach-Object { "|Covers=$_" }) -join ""
    }
}

dotnet test Nexora.slnx --filter $filter @DotnetArgs
exit $LASTEXITCODE
