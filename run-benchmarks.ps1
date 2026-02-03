#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Запуск бенчмарков XbrlProcessor и сохранение результатов в CSV.
.DESCRIPTION
    Прогоняет BenchmarkDotNet бенчмарки в Release и копирует результаты
    в папку benchmark-results с таймстемпом.
#>

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$BenchmarkProject = Join-Path $Root "XbrlProcessor.Benchmarks"
$ResultsDir = Join-Path $Root "benchmark-results"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host "=== XbrlProcessor Benchmark Runner ===" -ForegroundColor Cyan
Write-Host ""

# 1. Build in Release
Write-Host "[1/3] Building solution in Release..." -ForegroundColor Yellow
dotnet build (Join-Path $Root "XbrlProcessor.sln") -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build OK" -ForegroundColor Green

# 2. Run benchmarks
Write-Host "[2/3] Running benchmarks..." -ForegroundColor Yellow
Push-Location $BenchmarkProject
try {
    dotnet run -c Release --no-build -- --exporters csv json
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Benchmarks failed!" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

# 3. Copy results
Write-Host "[3/3] Collecting results..." -ForegroundColor Yellow
$BdnResults = Join-Path $BenchmarkProject "BenchmarkDotNet.Artifacts" "results"

if (Test-Path $BdnResults) {
    $DestDir = Join-Path $ResultsDir $Timestamp
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null

    Copy-Item (Join-Path $BdnResults "*") $DestDir -Recurse -Force

    Write-Host ""
    Write-Host "=== Results saved to: $DestDir ===" -ForegroundColor Green
    Write-Host ""
    Get-ChildItem $DestDir | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "No BenchmarkDotNet results found at $BdnResults" -ForegroundColor Red
}

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
