## Versión 1.1: copias, pegatinas de 50 × 50 mm e imprimir aunque algo sobresalga

- Junto al botón **Imprimir** hay un selector **Copias** (1 a 999): Imprimir envía directamente ese número de etiquetas, sin abrir ninguna ventana. Funciona con impresoras con controlador de Windows y por red (ZPL).
- **Pegatinas de 50 × 50 mm** en el Catálogo de etiquetas: 31 plantillas nuevas (residuos peligrosos, reciclaje, seguridad, inventario con QR, ubicación, muestras, contenedor, calibrado, revisado, fuera de servicio y cuarentena). El catálogo se abre directamente en esta categoría.
- **Se puede imprimir aunque algo sobresalga de la etiqueta.** La app avisa una vez («¿Imprimir igualmente?») y lo que quede fuera se recorta al borde.
- **Impresorapp Sencilla ya no muestra la DYMO** (ni en la bienvenida, ni en los formatos, ni en la ayuda): imprime solo en impresoras instaladas en Windows y en papel.
- **Impresorapp Sencilla también se actualiza sola** desde *Ayuda › Buscar actualizaciones…*, igual que la completa. Cada variante descarga su propio paquete, así que la Sencilla sigue siendo Sencilla. Desde la 1.0 Sencilla hay que instalar la 1.1 una vez a mano; a partir de ahí ya se actualiza sola.

## Versión 1.0: versión definitiva

- **Imprimir directo**: el botón Imprimir envía la etiqueta a la impresora elegida (1 copia) sin ventanas intermedias. Al lado, **Vista previa y opciones** abre la vista previa, la impresora, las copias y las preferencias (también Ctrl+Mayús+P).
- **Catálogo de etiquetas para Azor Ambiental**: 48 plantillas en 9 categorías (residuos peligrosos, reciclaje, seguridad, almacén, muestras, inventario, mantenimiento, contenedores y oficina), con buscador. Se abre desde Archivo, la barra superior y la ventana Nueva etiqueta. Los códigos LER y pictogramas son orientativos: revísalos con el responsable de medio ambiente.
- **Aviso del elemento que queda fuera**: al imprimir se indica qué elemento se sale de la etiqueta y se selecciona.
- Programa e instalador **firmados digitalmente por Azor Ambiental Holdco S.L.U.** (certificado interno), en sus dos variantes: Impresorapp e Impresorapp Sencilla.

## Nuevo en 0.26: recorte físico del diseño

Las imágenes que sobresalen se recortan al contorno de la etiqueta. El recorte de cada elemento ahora se intersecta con el de la página, en lugar de sustituirlo. La vista previa y la impresión mediante controlador usan la misma imagen final a 203 ppp, adecuada para las Toshiba de 203 ppp probadas. El controlador recibe solo el área física de la etiqueta, sin el sobrante de la imagen original. No se cambian los desplazamientos privados del controlador ni la calibración de la impresora.

Se comprueban con pruebas imágenes que sobresalen por los cuatro lados, tamaño físico y compensación del origen imprimible. La vista previa permanece cuadrada para un formato cuadrado. El envío guarda marcas de tiempo y geometría (sin el contenido del diseño) en %LOCALAPPDATA%\EtiquetaLocal\ultima-impresion.log para localizar demoras del controlador/servidor. La verificación física sigue pendiente; la entrega a Windows no confirma que la etiqueta haya salido correctamente.

Distribución sin firma Authenticode. No instala certificados ni controladores.

## Nuevo en 0.25: vista previa local y espera sin bloquear

La vista previa de etiquetas con controlador se dibuja localmente, sin crear un contexto de impresión ni consultar al servidor. Seleccionar la cola no comprueba su disponibilidad en red. La lista de impresoras se carga en segundo plano y se conserva durante la sesión.

Las preferencias del fabricante se abren en su propia ventana de Windows. Guarda y cierra esa ventana antes de enviar el trabajo. La consulta de formatos y el envío GDI se ejecutan en segundo plano, con estado visible. Se puede cancelar antes de iniciar el envío; después se utiliza la cola de Windows para detener el trabajo. No hay reintentos automáticos. La entrega a Windows no confirma la impresión física.

La vista previa muestra el diseño, no los desplazamientos privados del controlador ni la calibración del rollo. Se mantienen la comprobación del tamaño y la impresión al 100 %. La latencia del servidor al enviar puede continuar: esta versión evita repetir la consulta para generar la vista previa y mantiene la interfaz disponible.

Pruebas: renderizado local con medidas y posiciones verificadas, cancelación antes del envío, protección del trabajo ya iniciado y pruebas existentes. Pendiente: prueba física en la Toshiba compartida. No se han modificado certificados, controladores, emulación ni parámetros de la impresora.

Esta distribución 0.25 no tiene firma Authenticode. El certificado Seagull usado para confiar en el controlador Toshiba es independiente de la firma del programa.

Distribución 0.25 preparada en este entorno sin firma Authenticode: el proveedor de la clave del certificado instalado no permite firmar aquí. El script sign.ps1 sigue disponible para firmar desde el equipo/entorno que tenga acceso a esa clave. No se han exportado certificados ni claves privadas.

<!-- Versión actual: 1.1 -->

## Ruta de controlador incorporada en 0.24

En **Impresoras → Usar controlador de Windows / servidor** selecciona una cola instalada en este equipo, incluida una impresora compartida del servidor. La selección se guarda por usuario y aparece en el indicador de impresora. **Imprimir** abre una vista previa con las medidas del diseño al 100 %, sin escalado. En Preferencias se abre la ventana del fabricante; guarda los cambios allí antes de imprimir.

La opción usa GDI y el controlador traduce al lenguaje nativo, por ejemplo TPCL. No envía ZPL por TCP ni cambia la emulación física. Si no existe un formato coincidente, se solicita al controlador el tamaño personalizado del diseño; si devuelve otro tamaño, se impide continuar y se pide configurar el formato en Windows. Los márgenes no imprimibles del dispositivo siguen aplicándose. Instala o conecta primero la cola compartida en cada equipo; el programa no instala controladores automáticamente.

Para volver al envío directo, entra en Impresoras → Usar envío directo ZPL y selecciona una impresora compatible. Papel A4/A3 y DYMO mantienen sus rutas propias. Las conexiones USB pueden imprimirse mediante una cola instalada; el transporte USB directo sin controlador sigue pendiente.

Comprobados: conversión mm/unidades Windows, correspondencia de tamaños, rechazo de A4 para una etiqueta de 50 × 50 mm, renderizado GDI a tamaño real y persistencia de nombres UNC con Unicode. La impresión física con el servidor queda pendiente.

# Impresorapp

**[Español](#español) · [English](#english)**

---

## Español

Editor de etiquetas y carteles para Windows 10/11 de 64 bits. **Versión 1.1.**

Autor: **Alberto Marín (albertuxo85)**.

Diseña etiquetas con texto, imágenes, códigos QR, Code 39, emojis, marcos y líneas, con guías al mover elementos. Incluye formatos de rollo, cinta DYMO de 12 mm y papel A5/A4/A3, favoritos y búsqueda de impresoras de red y USB.

- **Impresoras de etiquetas por red** (Ethernet/Wi-Fi) mediante ZPL, por ejemplo Zebra ZT231 o Toshiba B-FV4D en modo Z.
- **DYMO LetraTag 200B por Bluetooth** (experimental).
- **Papel** con las impresoras instaladas en Windows y sus controladores.

### Instalar

1. Descarga **`Impresorapp-1.1-Setup.exe`** desde [Releases](https://github.com/albertuxo085/Impresorapp/releases/latest).
2. Ejecútalo. Se instala para el usuario actual en `%LOCALAPPDATA%\Programs\Impresorapp`, sin pedir permisos de administrador, con acceso en el menú Inicio y, si quieres, en el escritorio.
3. Necesita .NET Framework 4.8 (incluido en Windows 10/11 actualizados; el instalador avisa si falta).

Para desinstalar, usa *Configuración de Windows → Aplicaciones*. Tus diseños y preferencias se conservan.

### Firma digital

La distribución 0.25 se entrega sin firma Authenticode. Windows puede mostrar un aviso de editor desconocido. El proceso de firma sigue disponible en sign.ps1.

### Actualizar

En **Ayuda → Buscar actualizaciones** puedes buscar versiones nuevas o activar «Buscar nuevas versiones automáticamente al abrir» (desactivado por defecto). La aplicación consulta la última Release de este repositorio, comprueba el SHA-256 del paquete y, antes de abrir la versión nueva, ofrece guardar los cambios. No necesita permisos de administrador y no modifica controladores ni firmware.

La versión nueva se guarda en `%LOCALAPPDATA%\EtiquetaLocal\versions`. Se conservan los favoritos y rangos de búsqueda de versiones anteriores, y el acceso directo original abre siempre la versión más reciente.

### Guardar diseños

*Guardar* y *Guardar como* abren por defecto **Documentos\Diseños Etiquetas** (se crea si no existe y respeta la redirección de Documentos a OneDrive). Puedes elegir cualquier otra carpeta. Al cerrar, abrir otro diseño o actualizar, se ofrece guardar lo pendiente.

Los archivos `.etiqueta` incluyen el diseño y sus imágenes, pero no las preferencias de impresoras. Los diseños de versiones anteriores siguen siendo compatibles.

### Impresión y límites

- **Toshiba B-FV4D:** necesita etiquetas térmicas directas, Z Mode (ZPL) y sensor calibrado. Ancho máximo 101,6 mm a 203 ppp. El rollo físico debe coincidir con las medidas del diseño.
- **DYMO LetraTag:** cinta de 12 mm, largo de 25 a 200 mm, Bluetooth LE. El texto y los emojis nuevos usan el mismo tamaño, el mayor que cabe con un margen mínimo, y se centran en la cinta.
- **Códigos QR:** se generan en el propio equipo, con texto o URL de hasta 1200 bytes UTF-8. Comprueba que se leen bien, sobre todo en cinta estrecha.
- **USB:** detecta y guarda dispositivos, pero **la impresión directa por USB aún no está disponible**. No imprime a través de colas compartidas de Windows.
- **Búsqueda en red:** usa SNMP de solo lectura con la comunidad `public`. No consulta la base de datos de Print Audit.

### Para desarrolladores

**Compilar y probar.** Requiere Windows de 64 bits con .NET Framework 4.8 y sus referencias WinRT. QRCoder va incluido en `lib` con su licencia; no se descarga nada al compilar.

```powershell
.\build.ps1 -Test
```

Las pruebas internas no hacen barridos de red ni envían impresiones.

**Crear una versión firmada.** Cambia el número en `VERSION` y ejecuta:

```powershell
$compiler = .\tools\Get-InnoCompiler.ps1   # o la ruta de un ISCC.exe ya instalado
.\build-installer.ps1 -CompilerPath $compiler -CertificateThumbprint <huella del certificado>
```

Se genera en `release/`: `Impresorapp-X.Y-Setup.exe`, `Impresorapp-X.Y.zip` y sus dos `.sha256`. El ejecutable se firma antes de empaquetarlo y el instalador después. `sign.ps1` busca el certificado en el almacén del usuario y en el del equipo, y usa `signtool` si está instalado o, si no, la firma de PowerShell. No se guardan certificados ni claves en el repositorio.

**Publicar.** Sube el código a `main` y crea una Release con la etiqueta `vX.Y` adjuntando los **cuatro** archivos de `release/`. Las aplicaciones instaladas necesitan el ZIP y su `.sha256` para actualizarse; no uses el ZIP automático del código fuente. Subir un commit sin publicar una Release no genera actualizaciones.

### Licencia

Aún no se ha elegido una licencia para el código propio del proyecto. La biblioteca QRCoder tiene licencia MIT (ver `THIRD_PARTY.md`). El repositorio no incluye registros, configuraciones personales, firmware ni controladores.

---

## English

Label and sign editor for 64-bit Windows 10/11. **Version 0.23.**

Author: **Alberto Marín (albertuxo85)**.

Design labels with text, images, QR codes, Code 39, emojis, frames and lines, with smart guides while moving elements. Includes roll label formats, 12 mm DYMO tape and A5/A4/A3 paper, favourites, and network and USB printer discovery.

- **Network label printers** (Ethernet/Wi-Fi) via ZPL, for example Zebra ZT231 or Toshiba B-FV4D in Z mode.
- **DYMO LetraTag 200B over Bluetooth** (experimental).
- **Paper** through the printers installed in Windows and their drivers.

### Install

1. Download **`Impresorapp-1.1-Setup.exe`** from [Releases](https://github.com/albertuxo085/Impresorapp/releases/latest).
2. Run it. It installs for the current user in `%LOCALAPPDATA%\Programs\Impresorapp`, without administrator rights, with a Start menu shortcut and an optional desktop shortcut.
3. Requires .NET Framework 4.8 (included in up-to-date Windows 10/11; the installer warns if it is missing).

To uninstall, use *Windows Settings → Apps*. Your designs and preferences are kept.

### Digital signature

The application and the installer are **digitally signed by Azor Ambiental Holdco S.L.U.**, with a timestamp. It is an internal company certificate: on computers with the Azor certificate installed, Windows shows a verified publisher. On other computers you may see an unknown publisher or SmartScreen warning.

### Updates

In **Ayuda → Buscar actualizaciones** (Help → Check for updates) you can check for new versions or enable automatic checks on start-up (off by default). The app reads the latest Release of this repository, verifies the package SHA-256 and offers to save your changes before opening the new version. No administrator rights are needed and no drivers or firmware are changed.

New versions are stored in `%LOCALAPPDATA%\EtiquetaLocal\versions`. Favourites and search ranges from earlier versions are kept, and the original shortcut always opens the latest version.

### Saving designs

*Save* and *Save as* open **Documents\Diseños Etiquetas** by default (created if missing, OneDrive redirection respected). You can choose any other folder. When closing, opening another design or updating, the app offers to save pending changes.

`.etiqueta` files contain the design and its images, but not printer preferences. Designs from earlier versions remain compatible.

### Printing and limits

- **Toshiba B-FV4D:** needs direct thermal labels, Z Mode (ZPL) and a calibrated sensor. Maximum width 101.6 mm at 203 dpi. The physical roll must match the design size.
- **DYMO LetraTag:** 12 mm tape, 25 to 200 mm long, Bluetooth LE. New text and emojis share the same size, the largest that fits with a minimal margin, and are centred on the tape.
- **QR codes:** generated locally, with text or URLs up to 1200 UTF-8 bytes. Check that they scan well, especially on narrow tape.
- **USB:** detects and saves devices, but **direct USB printing is not available yet**. It does not print through shared Windows queues.
- **Network discovery:** uses read-only SNMP with the `public` community. It does not query the Print Audit database.

### For developers

**Build and test.** Requires 64-bit Windows with .NET Framework 4.8 and its WinRT references. QRCoder is included in `lib` with its licence; nothing is downloaded at build time.

```powershell
.\build.ps1 -Test
```

The built-in tests do not scan the network or send print jobs.

**Create a signed release.** Change the number in `VERSION` and run:

```powershell
$compiler = .\tools\Get-InnoCompiler.ps1   # or the path to an installed ISCC.exe
.\build-installer.ps1 -CompilerPath $compiler -CertificateThumbprint <certificate thumbprint>
```

This creates in `release/`: `Impresorapp-X.Y-Setup.exe`, `Impresorapp-X.Y.zip` and both `.sha256` files. The executable is signed before packaging and the installer afterwards. `sign.ps1` looks for the certificate in the user and machine stores, and uses `signtool` when installed or PowerShell signing otherwise. No certificates or keys are stored in the repository.

**Publish.** Push the code to `main` and create a Release tagged `vX.Y` with all **four** files from `release/`. Installed apps need the ZIP and its `.sha256` to update; do not use GitHub's automatic source code ZIP. Pushing a commit without publishing a Release does not trigger updates.

### Licence

No licence has been chosen yet for the project's own code. The QRCoder library is MIT-licensed (see `THIRD_PARTY.md`). The repository contains no logs, personal settings, firmware or drivers.
