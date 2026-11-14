#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory)]
    [string]$Method,
    [switch]$NoTest
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$resultsDir = Join-Path $repoRoot "Test Results/Crap"
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Get-SourceMethodName([string]$generatedClass, [string]$name) {
    if ($name -eq "MoveNext" -and $generatedClass -match '^<(.+)>d(__\d+)?$') {
        $name = $Matches[1]
    }
    if ($name -match '^<(.+)>b__(.+)$') {
        return "$($Matches[1]) (lambda $($Matches[2]))"
    }
    if ($name -match '^<(.+)>g__([^|]+)\|') {
        return "$($Matches[1]) (local $($Matches[2]))"
    }
    return $name
}

if (-not $NoTest) {
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $resultsDir
    dotnet test (Join-Path $repoRoot "Nexora.slnx") -v q -nologo --collect:"XPlat Code Coverage" --results-directory $resultsDir
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Some tests failed or did not build, so scores may be based on incomplete coverage."
    }
}

$reports = @(Get-ChildItem -Path $resultsDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue)
if ($reports.Count -eq 0) {
    throw "No coverage found in $resultsDir. Run without -NoTest first."
}

$methods = @{}
foreach ($report in $reports) {
    $xml = [xml](Get-Content -Raw $report.FullName)
    $source = @($xml.coverage.sources.source)[0]
    foreach ($class in $xml.SelectNodes("//class")) {
        $segments = $class.name -split "/"
        $owner = (($segments | Where-Object { -not $_.StartsWith("<") }) -join ".") -replace '`\d+', ""
        $generatedClass = if ($segments[-1].StartsWith("<")) { $segments[-1] } else { "" }
        $file = [System.IO.Path]::GetRelativePath($repoRoot, (Join-Path $source $class.filename))
        foreach ($node in $class.SelectNodes("methods/method")) {
            $key = "$file|$($class.name)|$($node.name)|$($node.signature)"
            if (-not $methods.ContainsKey($key)) {
                $name = Get-SourceMethodName $generatedClass $node.name
                $signature = if ($name -eq $node.name) { $node.signature } else { "" }
                $methods[$key] = [pscustomobject]@{
                    Name = "$owner.$name"
                    Label = "$owner.$name$signature"
                    File = $file
                    Complexity = [int]$node.complexity
                    Lines = @{}
                }
            }
            $entry = $methods[$key]
            foreach ($line in $node.SelectNodes("lines/line")) {
                $number = [int]$line.number
                $entry.Lines[$number] = $entry.Lines[$number] -or ([int]$line.hits -gt 0)
            }
        }
    }
}

$matched = @($methods.Values | Where-Object { $_.Name.Contains($Method, [System.StringComparison]::OrdinalIgnoreCase) })
if ($matched.Count -eq 0) {
    throw "No method matches '$Method'."
}

$matched |
        ForEach-Object {
            $lineCount = $_.Lines.Count
            $coverage = if ($lineCount -eq 0) { 1.0 } else { @($_.Lines.Values | Where-Object { $_ }).Count / $lineCount }
            $crap = [math]::Pow($_.Complexity, 2) * [math]::Pow(1 - $coverage, 3) + $_.Complexity
            $firstLine = if ($lineCount -eq 0) { 0 } else { ($_.Lines.Keys | Measure-Object -Minimum).Minimum }
            [pscustomobject]@{ Entry = $_; Coverage = $coverage; Crap = $crap; FirstLine = $firstLine }
        } |
        Sort-Object -Property Crap -Descending |
        ForEach-Object {
            [string]::Format(
                $invariant,
                "{0}  crap={1:0.0}  complexity={2}  coverage={3:0}%  {4}:{5}",
                $_.Entry.Label,
                $_.Crap,
                $_.Entry.Complexity,
                $_.Coverage * 100,
                $_.Entry.File,
                $_.FirstLine)
        }
