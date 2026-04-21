#!/usr/bin/env pwsh

param(
    [string]$Framework = "net9.0",
    [string]$OutputDir = "coverage-report"
)

Write-Host "🧪 Running tests with coverage collection..." -ForegroundColor Cyan

# Run tests with coverage
dotnet test tests/Head.Net.Tests/Head.Net.Tests.csproj `
    --configuration Debug `
    --framework $Framework `
    --logger "trx;LogFileName=test-results.trx" `
    /p:CollectCoverage=true `
    /p:CoverageFormat=opencover `
    /p:CoverageFileName="tests/Head.Net.Tests/coverage.xml"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Tests passed!" -ForegroundColor Green

# Check if ReportGenerator is installed
$reportGenExists = dotnet tool list -g | Select-String "reportgenerator"

if (-not $reportGenExists) {
    Write-Host "📦 Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g reportgenerator
}

Write-Host "📊 Generating coverage report..." -ForegroundColor Cyan

# Generate coverage report
reportgenerator `
    -reports:"tests/Head.Net.Tests/coverage.xml" `
    -targetdir:$OutputDir `
    -reporttypes:"Html;Badges" `
    -classfilters:"+Head.Net.* -Head.Net.Tests.*"

Write-Host "✅ Coverage report generated in '$OutputDir'" -ForegroundColor Green
Write-Host "📈 Open '$OutputDir/index.html' to view detailed report" -ForegroundColor Cyan

# Display summary
$coverageFile = "tests/Head.Net.Tests/coverage.xml"
if (Test-Path $coverageFile) {
    [xml]$coverage = Get-Content $coverageFile
    $linesCovered = $coverage.CoverageSession.Summary.numLinesCovered
    $linesValid = $coverage.CoverageSession.Summary.numLinesValid
    $percent = if ($linesValid -gt 0) { [math]::Round(($linesCovered / $linesValid) * 100, 1) } else { 0 }

    Write-Host ""
    Write-Host "Coverage Summary:" -ForegroundColor Green
    Write-Host "  Lines Covered: $linesCovered / $linesValid ($percent%)" -ForegroundColor Yellow
}
