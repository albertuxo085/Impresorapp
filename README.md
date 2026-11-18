# Impresorapp

Editor de etiquetas y carteles para Windows 10/11 de 64 bits. Versión 0.19.

Autor: **Alberto Marín (albertuxo85)**.

Cinta con texto, imágenes, QR, Code 39, fuentes y alineación; guías al mover elementos; formatos de etiquetas y A5/A4/A3; favoritos y búsqueda de impresoras LAN/USB. Impresión directa LAN mediante ZPL y Bluetooth experimental para DYMO LetraTag 200B. El papel utiliza impresoras instaladas y sus controladores.

## Guardar diseños

Guardar y Guardar como muestran el diálogo para elegir nombre y ubicación. Se abre inicialmente **Documentos\Diseños Etiquetas** y se crea esa carpeta automáticamente. Se respeta la carpeta Documentos que indica Windows, incluida su redirección a OneDrive. Puedes elegir cualquier otra ubicación. Cancelar conserva los cambios pendientes; al cerrar, abrir otro diseño o actualizar, se ofrece guardar lo que no esté guardado.

Los archivos `.etiqueta` incluyen el contenido del diseño y las imágenes. No incluyen preferencias de impresoras. Los diseños anteriores siguen siendo compatibles.

## Instalar y distribuir

El archivo para los usuarios es **Impresorapp-0.19-Setup.exe**. Súbelo a los assets de una **GitHub Release**, no a la carpeta del código fuente. Instala para el usuario actual en `%LOCALAPPDATA%\Programs\Impresorapp`, sin pedir administrador. Incluye menú Inicio, acceso opcional en el escritorio y desinstalación desde Windows. Detecta si falta .NET Framework 4.8. Mantiene diseños y preferencias al desinstalar.

El programa y el instalador identifican como autor a Alberto Marín (albertuxo85), pero esta distribución **no tiene firma digital Authenticode** porque no se dispone de certificado. Windows puede mostrar editor desconocido o una advertencia de SmartScreen. No se ha creado un certificado autofirmado ni simulado una firma verificada.

Para generar el instalador localmente:

```powershell
$compiler = .\tools\Get-InnoCompiler.ps1
.\build-installer.ps1 -CompilerPath $compiler
```

El script prepara Inno Setup 7.1.1 desde su publicación oficial, comprueba SHA-256 y firma del compilador y lo extrae en modo portable. El compilador no se incluye en el código ni en el instalador de la aplicación. Alternativamente, proporciona la ruta de un ISCC.exe de Inno Setup que ya tengas instalado.

La salida incluye `Impresorapp-0.19-Setup.exe`, su `.sha256`, el ZIP portable y su `.zip.sha256`. Conserva también estos dos últimos en Releases: las versiones existentes de Impresorapp los necesitan para actualizarse automáticamente.

Si en el futuro dispones de un certificado de firma de código en el almacén personal de Windows y del SDK con signtool, puedes usar `-CertificateThumbprint` y opcionalmente `-SignToolPath` en build-installer.ps1. Se firman el ejecutable de la aplicación y el instalador, con sello de tiempo. No se guardan certificados ni claves en el repositorio. El desinstalador generado no se firma con esta opción.

## Compilar y crear el paquete

Se necesita Windows de 64 bits con .NET Framework 4.8 y sus referencias WinRT. QRCoder está incluido en `lib` con su licencia; no se descargan dependencias al compilar.

```powershell
.\build.ps1 -Test
.\package.ps1
```

Abre `dist/Impresorapp-0.19.exe`. `package.ps1` vuelve a compilar y comprobar; genera `release/Impresorapp-0.19.zip` y su archivo `.zip.sha256`. Distribuye ambos como assets de la versión; no uses el ZIP automático de código fuente como actualización.

## Actualizaciones desde GitHub

Repositorio configurado: https://github.com/albertuxo085/Impresorapp

En **Actualizaciones** puedes activar «Buscar nuevas versiones automáticamente al abrir» o buscar manualmente. Está desactivado por defecto. La aplicación consulta la última Release estable del repositorio público, compara la versión y ofrece descargarla. Comprueba SHA-256, rutas del ZIP, archivos necesarios y versión del ejecutable. Antes de abrirla ofrece guardar cambios y reiniciar. No necesita permisos de administrador y no modifica controladores ni firmware.

La versión nueva se abre en una carpeta por usuario dentro de `%LOCALAPPDATA%\EtiquetaLocal\versions`. Se conserva la carpeta de preferencias histórica `EtiquetaLocal` para mantener los favoritos y rangos de versiones anteriores. Los diseños permanecen en la ubicación elegida. Los ejecutables de Impresorapp a partir de 0.16 recuerdan la versión instalada: al abrir de nuevo el acceso directo original, redirigen a la versión nueva si está disponible. Los ejecutables antiguos de EtiquetaLocal no disponen de esta función; sustituye su acceso directo al pasar por primera vez a Impresorapp.

No se incluyen contraseñas ni tokens. Esta implementación consulta repositorios públicos; un repositorio privado requiere una integración de autenticación adicional. El SHA-256 comprueba integridad y no sustituye una firma de código. Las consultas automáticas sin conexión no bloquean el editor. Subir un commit sin publicar una Release no produce actualizaciones.

## Publicar una versión

1. Sube **el contenido de esta carpeta** a la raíz del repositorio, incluidos `.github/workflows`, `lib` y `VERSION`.
2. Para la versión siguiente, cambia `VERSION` (por ejemplo, `0.18`) y añade los cambios de código. El número del ejecutable, metadatos y actualizador se generan desde ese archivo.
3. Sube el commit y una etiqueta que coincida, por ejemplo `v0.18`.
4. El workflow Windows prepara el compilador oficial, compila, ejecuta las pruebas, crea el instalador y un borrador con EXE, ZIP y ambos SHA-256, y después lo publica como última versión. GitHub Actions debe estar habilitado y permitir escritura de contenido para este workflow.
5. Revisa el resultado en Actions y Releases. No reutilices una etiqueta ya publicada. Si el workflow falla después de crear el borrador, revisa el borrador y los archivos antes de publicarlo manualmente.

Los workflows se han preparado y se ha probado el empaquetado local. El instalador se ha probado desplegando los archivos en una carpeta aislada (modo de prueba sin registro ni accesos) y ejecutando las pruebas internas del programa desplegado. La integración de accesos/desinstalación en Windows y la ejecución real de Actions y la descarga desde una Release quedan pendientes hasta subir el proyecto y publicar la primera versión.

## Impresión y límites

La Toshiba B-FV4D necesita etiquetas térmicas directas, Z Mode compatible con ZPL y sensor/calibración adecuados. Cada trabajo envía las medidas del diseño; el rollo físico debe coincidir. El perfil Toshiba limita el ancho a 101,6 mm y usa 203 dpi. DYMO utiliza el mismo editor y requiere Bluetooth LE. Los QR se generan localmente, con texto Unicode o URL, hasta 1200 bytes UTF-8; comprueba su lectura física, especialmente en cinta estrecha.

USB permite detectar y guardar dispositivos; **la impresión directa USB sigue pendiente**. No imprime mediante colas compartidas de Windows. La ubicación de favoritos es local; no cambia la impresora. SNMP usa consultas de lectura con comunidad `public` y no consulta la base de datos de Print Audit.

Las pruebas internas no realizan barridos de red ni envían impresiones. Incluyen persistencia, márgenes, ZPL, protocolo DYMO, QR, guías, alineación, guardado atómico, versiones, integridad del paquete y protección de rutas ZIP. Se ha verificado aparte la decodificación QR del mapa ZPL a 203 dpi. La impresión física de funciones nuevas debe probarse con el dispositivo de destino.

## Licencia y publicación

No se ha elegido una licencia para el código propio del proyecto. Decide su licencia antes de distribuirlo como software de código abierto. La biblioteca QRCoder tiene licencia MIT incluida. Consulta `THIRD_PARTY.md`. No se incluyen registros, configuraciones personales, firmware ni controladores.
