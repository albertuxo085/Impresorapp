param([switch]$Test,[switch]$Sencilla,[string]$OutputDirectory,[string]$CertificateThumbprint,[string]$SignToolPath)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'dist'
if ($OutputDirectory) { $out = [System.IO.Path]::GetFullPath($OutputDirectory) }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw 'Se necesita Windows de 64 bits y .NET Framework 4.8.' }
$refs = @((Join-Path $root 'lib\QRCoder.dll'),'System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.dll','System.Management.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Runtime.Serialization.dll')
foreach ($name in @('Windows.Devices','Windows.Foundation','Windows.Storage')) {
    $path = Join-Path $env:WINDIR ('System32\WinMetadata\'+$name+'.winmd')
    if (-not (Test-Path -LiteralPath $path)) { throw "Falta la referencia de Windows: $path" }
    $refs += $path
}
foreach ($name in @('System.Runtime','System.Runtime.InteropServices.WindowsRuntime')) {
    $base = Join-Path $env:WINDIR ('Microsoft.NET\assembly\GAC_MSIL\'+$name)
    $assembly = Get-ChildItem -LiteralPath $base -Recurse -Filter ($name+'.dll') | Select-Object -First 1
    if (-not $assembly) { throw "Falta la referencia .NET: $name" }
    $refs += $assembly.FullName
}
New-Item -ItemType Directory -Path $out -Force | Out-Null
$versionText = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($versionText -notmatch '^\d+\.\d+(\.\d+){0,2}$') { throw 'VERSION debe contener una versión numérica, por ejemplo 0.16.' }
$parsed = [Version]::Parse($versionText)
$assemblyVersion = '{0}.{1}.{2}.{3}' -f $parsed.Major,$parsed.Minor,[Math]::Max(0,$parsed.Build),[Math]::Max(0,$parsed.Revision)
$appName = if ($Sencilla) {'Impresorapp Sencilla'} else {'Impresorapp'}
$exeBase = if ($Sencilla) {'Impresorapp-Sencilla-'} else {'Impresorapp-'}
$versionSource = Join-Path $out ($exeBase+'AppVersion.g.cs')
$versionCode = 'using System.Reflection; [assembly:AssemblyVersion("'+$assemblyVersion+'")] [assembly:AssemblyFileVersion("'+$assemblyVersion+'")] [assembly:AssemblyProduct("'+$appName+'")] [assembly:AssemblyCompany("Alberto Marín (albertuxo85)")] [assembly:AssemblyCopyright("Copyright © 2026 Alberto Marín (albertuxo85)")] namespace EtiquetaLocal { public static class AppVersion { public const string Text="'+$versionText+'"; public const string Name="'+$appName+'"; } }'
[IO.File]::WriteAllText($versionSource,$versionCode)
$exe = Join-Path $out ($exeBase+$versionText+'.exe')
$arguments = @()
if ($Sencilla) { $arguments += '/define:SENCILLA' }
$arguments += @(('/win32icon:'+(Join-Path $root 'assets\EtiquetaLocal.ico')),'/nologo','/target:winexe',('/out:'+$exe))
$arguments += $versionSource
$arguments += $refs | ForEach-Object { '/r:'+$_ }
$arguments += Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
& $csc @arguments
if ($LASTEXITCODE -ne 0) { throw 'La compilacion ha fallado.' }
Copy-Item -LiteralPath (Join-Path $root 'src\App.config') -Destination ($exe+'.config') -Force
Copy-Item -Path (Join-Path $root 'examples\*.etiqueta') -Destination $out -Force
Copy-Item -LiteralPath (Join-Path $root 'lib\QRCoder.dll') -Destination $out -Force
Copy-Item -LiteralPath (Join-Path $root 'lib\QRCoder-LICENSE.txt') -Destination $out -Force
Copy-Item -LiteralPath (Join-Path $root 'update-defaults.json') -Destination $out -Force
if ($Test) {
    $process = Start-Process -FilePath $exe -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw 'Las pruebas han fallado. Consulta dist\error_pruebas.txt.' }
    Write-Host 'Pruebas correctas. No se ha enviado ninguna impresion ni realizado un barrido de red.'
}
if ($CertificateThumbprint) { & (Join-Path $root 'sign.ps1') -FilePath $exe -CertificateThumbprint $CertificateThumbprint -SignToolPath $SignToolPath }
Write-Host "Aplicacion compilada: $exe"
