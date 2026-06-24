# Stratix — start API (dev profile, in-memory H2)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$mvn = Join-Path $PSScriptRoot "tools\maven\apache-maven-3.9.11\bin\mvn.cmd"
if (-not (Test-Path $mvn)) {
    Write-Error "Maven not found. Run setup once or install Maven."
    exit 1
}
Set-Location (Join-Path $PSScriptRoot "backend")
& $mvn spring-boot:run "-Dspring-boot.run.profiles=dev"
