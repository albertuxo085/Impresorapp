param([string]$CompilerPath,[string]$OutputDirectory,[string]$CertificateThumbprint,[string]$SignToolPath)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$versionText=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if (-not $CompilerPath) {
    $command=Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {$CompilerPath=$command.Source}
    else { foreach ($candidate in @((Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),(Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'))) {if(Test-Path -LiteralPath $candidate){$CompilerPath=$candidate;break}} }
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) { throw 'Instala Inno Setup 7/6 desde https://jrsoftware.org/inno-setup/downloads o indica -CompilerPath.' }
$out=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $root 'release'}
& (Join-Path $root 'package.ps1') -OutputDirectory $out -CertificateThumbprint $CertificateThumbprint -SignToolPath $SignToolPath
$inputRoot=Join-Path $out ('installer-input-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath (Join-Path $out ('Impresorapp-'+$versionText+'.zip')) -DestinationPath $inputRoot
$inputDir=Join-Path $inputRoot ('Impresorapp-'+$versionText)
$compilerArgs=@('/Qp',('/DAppVersion='+$versionText),('/DInputDir='+$inputDir),('/DOutputDir='+$out))
& $CompilerPath @compilerArgs (Join-Path $root 'installer\Impresorapp.iss')
if ($LASTEXITCODE -ne 0) { throw 'Ha fallado la creación del instalador.' }
$setup=Join-Path $out ('Impresorapp-'+$versionText+'-Setup.exe')
if ($CertificateThumbprint) { & (Join-Path $root 'sign.ps1') -FilePath $setup -CertificateThumbprint $CertificateThumbprint -SignToolPath $SignToolPath }
$hash=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($setup+'.sha256'),$hash+'  '+[IO.Path]::GetFileName($setup)+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
Write-Host "Instalador listo: $setup"
if (-not $CertificateThumbprint) {Write-Host 'Autor: Alberto Marín (albertuxo85). Sin firma digital Authenticode; requiere certificado para activarla.'}
