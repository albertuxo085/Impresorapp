param([switch]$Sencilla,[string]$OutputDirectory,[string]$CertificateThumbprint,[string]$SignToolPath)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$versionText = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($versionText -notmatch '^\d+\.\d+(\.\d+){0,2}$') { throw 'VERSION no válida.' }
$base = if ($Sencilla) {'Impresorapp-Sencilla-'} else {'Impresorapp-'}
& (Join-Path $root 'build.ps1') -Test -Sencilla:$Sencilla -CertificateThumbprint $CertificateThumbprint -SignToolPath $SignToolPath
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'No se ha podido compilar.' }
$out = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root 'release' }
New-Item -ItemType Directory -Path $out -Force | Out-Null
$staging = Join-Path $out ('staging-'+[Guid]::NewGuid().ToString('N'))
$folder = Join-Path $staging ($base+$versionText)
New-Item -ItemType Directory -Path $folder -Force | Out-Null
foreach ($name in @(($base+$versionText+'.exe'),($base+$versionText+'.exe.config'),'QRCoder.dll','QRCoder-LICENSE.txt','update-defaults.json')) {
    Copy-Item -LiteralPath (Join-Path (Join-Path $root 'dist') $name) -Destination $folder
}
Copy-Item -Path (Join-Path $root 'examples\*.etiqueta') -Destination $folder
Copy-Item -LiteralPath (Join-Path $root 'assets\EtiquetaLocal.ico') -Destination (Join-Path $folder 'Impresorapp.ico')
Copy-Item -LiteralPath (Join-Path $root 'LEEME.md') -Destination $folder
$zip = Join-Path $out ($base+$versionText+'.zip')
Compress-Archive -LiteralPath $folder -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($zip+'.sha256'),$hash+'  '+[IO.Path]::GetFileName($zip)+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
Write-Host "Paquete listo: $zip"
Write-Host "Comprobación SHA-256: $zip.sha256"
