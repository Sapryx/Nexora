#!/usr/bin/env pwsh
param(
    [string]$Rid = "win-x64"
)

$ErrorActionPreference = "Stop"

$publishDir = Join-Path $PSScriptRoot "src/Nexora/bin/Release/net10.0/$Rid/publish"
$configPath = Join-Path $PSScriptRoot "publish-whitelist.conf"

dotnet publish src/Nexora -c Release -r $Rid

Write-Host "`n--- Trimming VLC plugins (RID: $Rid) ---"

$deleteFolders = [System.Collections.Generic.List[string]]::new()
$keepInFolder = @{}
$currentSection = $null

Get-Content $configPath | ForEach-Object {
    $line = $_.Trim()
    if ($line -eq "" -or $line.StartsWith("#")) { return }
    if ($line -eq "[DeleteFolders]") { $currentSection = "delete"; return }
    if ($line -match '^\[KeepInFolder:(.+)\]$') {
        $currentSection = "keep:$($Matches[1])"
        $keepInFolder[$Matches[1]] = [System.Collections.Generic.List[string]]::new()
        return
    }
    if ($currentSection -eq "delete") {
        $deleteFolders.Add($line)
    } elseif ($currentSection -like "keep:*") {
        $folderName = $currentSection.Substring(5)
        $keepInFolder[$folderName].Add($line)
    }
}

$removedFolders = 0
Get-ChildItem -Path $publishDir -Recurse -Directory | ForEach-Object {
    if ($deleteFolders -contains $_.Name) {
        Remove-Item -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
        $removedFolders++
    }
}

$removedFiles = 0
foreach ($folderName in $keepInFolder.Keys) {
    $keepList = $keepInFolder[$folderName]
    Get-ChildItem -Path $publishDir -Recurse -Directory -Filter $folderName | ForEach-Object {
        Get-ChildItem -Path $_.FullName -File | ForEach-Object {
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            if ($keepList -notcontains $baseName) {
                Remove-Item -Force $_.FullName
                $removedFiles++
            }
        }
    }
}

Get-ChildItem -Path $publishDir -Recurse -Include "*.pdb", "*.lib" -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-Item -Force $_.FullName }
$anglePath = Join-Path $publishDir "av_libglesv2.dll"
if (Test-Path $anglePath) { Remove-Item -Force $anglePath }

Get-ChildItem -Path $publishDir -Recurse -Directory |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object {
            if (@(Get-ChildItem -Path $_.FullName -Force).Count -eq 0) {
                Remove-Item -Force $_.FullName
            }
        }

$finalSize = (Get-ChildItem -Path $publishDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Removed $removedFolders plugin folder(s) and $removedFiles curated file(s)."
Write-Host "Publish complete: $publishDir"
Write-Host ("Final size: {0:N1} MB" -f $finalSize)
