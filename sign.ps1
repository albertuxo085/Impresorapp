param([Parameter(Mandatory=$true)][string]$FilePath,[Parameter(Mandatory=$true)][string]$CertificateThumbprint,[string]$SignToolPath)
$ErrorActionPreference='Stop'
if ($CertificateThumbprint -notmatch '^[0-9a-fA-F]{40}$') { throw 'La huella del certificado no es válida.' }
# El certificado puede estar en el almacén del usuario o en el del equipo.
$cert = $null
foreach ($store in @('Cert:\CurrentUser\My','Cert:\LocalMachine\My')) {
    $candidate = Join-Path $store $CertificateThumbprint
    if (Test-Path -LiteralPath $candidate) { $cert = Get-Item -LiteralPath $candidate; break }
}
if (-not $cert) { throw 'No se encuentra el certificado en CurrentUser\My ni en LocalMachine\My.' }
if (-not $cert.HasPrivateKey -or $cert.NotAfter -lt (Get-Date)) { throw 'Se necesita un certificado vigente con clave privada.' }
if (-not (@($cert.EnhancedKeyUsageList | ForEach-Object { [string]$_.ObjectId }) -contains '1.3.6.1.5.5.7.3.3')) { throw 'El certificado no permite firma de código.' }
if (-not $SignToolPath) {
    $tool=Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($tool) { $SignToolPath=$tool.Source }
    else { $sdkRoot=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'; $tool=Get-ChildItem -LiteralPath $sdkRoot -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.FullName -match '\\x64\\'} | Sort-Object FullName -Descending | Select-Object -First 1; if ($tool) {$SignToolPath=$tool.FullName} }
}
if ($SignToolPath) {
    $storeArgs = if ($cert.PSParentPath -match 'LocalMachine') { @('/sm') } else { @() }
    & $SignToolPath sign @storeArgs /sha1 $CertificateThumbprint /fd SHA256 /tr https://timestamp.digicert.com /td SHA256 /d Impresorapp $FilePath
    if ($LASTEXITCODE -ne 0) { throw 'No se ha podido firmar el archivo.' }
} else {
    # Sin el SDK de Windows: firma con PowerShell (SHA-256 y sello de tiempo).
    $result = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
    if ($result.Status -ne 'Valid') { throw ('No se ha podido firmar el archivo: '+$result.StatusMessage) }
}
$signature=Get-AuthenticodeSignature -LiteralPath $FilePath
if ($signature.Status -ne 'Valid') { throw ('La firma no se ha validado: '+$signature.Status) }
