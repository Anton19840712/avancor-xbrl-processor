$repoRoot = $PSScriptRoot
$projectDir = Join-Path $repoRoot "XbrlProcessor"
$settingsPath = Join-Path $projectDir "appsettings.json"
$originalJson = Get-Content $settingsPath -Raw -Encoding UTF8

$configs = @(
    @{ ParserMode = "XDocument"; Parallelism = 1;  BatchSize = 0;  Label = "XDoc, seq" },
    @{ ParserMode = "XDocument"; Parallelism = 4;  BatchSize = 0;  Label = "XDoc, par=4" },
    @{ ParserMode = "XDocument"; Parallelism = 0;  BatchSize = 0;  Label = "XDoc, par=CPU" },
    @{ ParserMode = "Streaming"; Parallelism = 1;  BatchSize = 0;  Label = "Stream, seq" },
    @{ ParserMode = "Streaming"; Parallelism = 4;  BatchSize = 0;  Label = "Stream, par=4" },
    @{ ParserMode = "Streaming"; Parallelism = 0;  BatchSize = 0;  Label = "Stream, par=CPU" },
    @{ ParserMode = "Streaming"; Parallelism = 0;  BatchSize = 10; Label = "Stream, CPU, b=10" },
    @{ ParserMode = "Streaming"; Parallelism = 0;  BatchSize = 25; Label = "Stream, CPU, b=25" },
    @{ ParserMode = "XDocument"; Parallelism = 0;  BatchSize = 10; Label = "XDoc, CPU, b=10" },
    @{ ParserMode = "XDocument"; Parallelism = 0;  BatchSize = 25; Label = "XDoc, CPU, b=25" }
)

Write-Host ""
Write-Host "===== BENCHMARK: XBRL files ====="
Write-Host ""
Write-Host ("{0,-25} {1,12} {2,12} {3,15} {4,15}" -f "Config", "Parse(ms)", "Total(ms)", "MemBefore(MB)", "MemDelta(MB)")
Write-Host ("-" * 82)

Push-Location $projectDir
try {
    foreach ($cfg in $configs) {
        $json = $originalJson | ConvertFrom-Json
        $json.XbrlSettings.Processing.ParserMode = $cfg.ParserMode
        $json.XbrlSettings.Processing.MaxDegreeOfParallelism = $cfg.Parallelism
        $json.XbrlSettings.Processing.BatchSize = $cfg.BatchSize
        $json | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8

        $output = & dotnet run -c Release 2>&1 | Out-String

        $parseMs   = if ($output -match "PARSE_MS=(\d+)")          { $matches[1] } else { "?" }
        $totalMs   = if ($output -match "TOTAL_MS=(\d+)")          { $matches[1] } else { "?" }
        $memBefore = if ($output -match "MEM_BEFORE_MB=([\d.,]+)") { $matches[1] } else { "?" }
        $memDelta  = if ($output -match "MEM_DELTA_MB=([\d.,-]+)") { $matches[1] } else { "?" }

        Write-Host ("{0,-25} {1,12} {2,12} {3,15} {4,15}" -f $cfg.Label, $parseMs, $totalMs, $memBefore, $memDelta)
    }
} finally {
    $originalJson | Set-Content $settingsPath -Encoding UTF8
    Pop-Location
}
Write-Host ""
Write-Host "===== DONE ====="
