# Starts the whole stack on this machine (postgres, redis, api, nginx, certbot, backup) without restoring packages inside Docker.
# Run from the backend folder:  .\deploy\local\up.ps1        (stop: .\deploy\local\up.ps1 -Down)
param([switch]$Down)
# Native commands print progress on stderr; Windows PowerShell 5.1 would treat that as an error under "Stop", so exit codes are checked instead.
$ErrorActionPreference = "Continue"
Set-Location (Join-Path $PSScriptRoot "..\..")

$compose = @("-f", "docker-compose.yml", "-f", "docker-compose.local.yml")

if ($Down) {
    docker compose @compose down
    return
}

if (-not (Test-Path ".env")) { throw ".env not found: copy .env.example to .env first." }
$env_text = Get-Content ".env" -Raw
foreach ($name in "POSTGRES_PASSWORD", "REDIS_PASSWORD") {
    if ($env_text -notmatch "(?m)^\s*$name=\S+") { throw "$name is not set in .env (see deploy/local/README.md)." }
}

# The publish runs here, where the machine's certificates are trusted.
if (Test-Path "publish-local") { Remove-Item "publish-local" -Recurse -Force }
dotnet publish WebApi/WebApi.csproj -c Release -o publish-local -p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# The admin panel is built here too (same reason); skipped when the admin folder is not next to the backend.
$admin = Join-Path (Get-Location) "..\I-Rieltor.uz-admin"
if (Test-Path $admin) {
    Push-Location $admin
    npx ng build --configuration docker
    $adminExit = $LASTEXITCODE
    Pop-Location
    if ($adminExit -ne 0) { throw "admin build failed." }
}

docker compose @compose up -d --build
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed." }

$port = if ($env_text -match "(?m)^\s*HTTP_PORT=(\d+)") { $Matches[1] } else { "80" }
Write-Host "`nAPI through Nginx: http://localhost:$port/api/slide/public   health: http://localhost:$port/health/ready"
$adminPort = if ($env_text -match "(?m)^\s*ADMIN_PORT=(\d+)") { $Matches[1] } else { "8081" }
Write-Host "Admin panel: http://localhost:$adminPort"
Write-Host "Logs: docker compose $($compose -join ' ') logs -f api"
