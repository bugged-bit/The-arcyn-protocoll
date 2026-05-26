$exe = Join-Path $PSScriptRoot "..\ARCYN.exe"
if (Test-Path $exe) {
    Start-Process -FilePath $exe -WindowStyle Hidden
} else {
    Write-Error "ARCYN.exe not found at $exe"
}
