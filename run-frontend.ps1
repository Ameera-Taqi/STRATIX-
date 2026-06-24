# Stratix — start Angular dashboard (port 4200)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
Set-Location (Join-Path $PSScriptRoot "frontend")
npm start
