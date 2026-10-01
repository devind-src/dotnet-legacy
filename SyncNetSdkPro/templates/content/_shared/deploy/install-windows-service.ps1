# Memasang API Template sebagai Windows Service (jalankan sebagai Administrator).
# Hasil publish: dotnet publish -c Release -o C:\SyncNet\syncnet-template-service
param(
    [string]$Path = "C:\SyncNet\syncnet-template-service",
    [string]$Name = "syncnet-template-service"
)

$exe = Join-Path $Path "SyncNet.Template.exe"
if (-not (Test-Path $exe)) { throw "Tidak ditemukan: $exe" }

New-Service -Name $Name -BinaryPathName "`"$exe`"" -DisplayName "API Template (SyncNet)" -StartupType Automatic
[Environment]::SetEnvironmentVariable("SYNCNET_HOME", "C:\SyncNet", "Machine")
Start-Service -Name $Name
Write-Host "Service $Name terpasang dan berjalan."
