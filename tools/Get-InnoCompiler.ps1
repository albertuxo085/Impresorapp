param([string]$DestinationRoot)
$ErrorActionPreference='Stop'
if (-not $DestinationRoot) {$DestinationRoot=Join-Path $env:TEMP 'Impresorapp-Inno-7.1.1'}
New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
$archive=Join-Path $DestinationRoot 'innosetup-7.1.1-x64.exe'
$url='https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe'
$expected='8A5A6711C3766A6E5FB74F280BA0E3551E719CE94538D901FDBD1F8F9920B3F0'
if (-not (Test-Path -LiteralPath $archive)) {Invoke-WebRequest -Uri $url -OutFile $archive}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) {throw 'El compilador no coincide con el archivo oficial comprobado.'}
if ((Get-AuthenticodeSignature -LiteralPath $archive).Status -ne 'Valid') {throw 'La firma del compilador no se ha podido validar.'}
$compilerDir=Join-Path $DestinationRoot 'portable'
$compiler=Join-Path $compilerDir 'ISCC.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $process=Start-Process -FilePath $archive -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/PORTABLE=1','/NOICONS',('/DIR="'+$compilerDir+'"')) -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $compiler)) {throw 'No se ha preparado el compilador portable.'}
}
return $compiler
