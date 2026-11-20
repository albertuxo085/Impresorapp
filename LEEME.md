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

## Nuevo en 0.30: interfaz renovada

- **Barra superior verde azulado** con grupos de iconos: Archivo (nueva, abrir, guardar, plantillas), Editar (deshacer, **rehacer**, cortar, copiar, pegar, eliminar) y Diseño (colocar en la etiqueta, girar/voltear, orden, duplicar). La impresora y el botón Imprimir quedan a la derecha.
- **Herramientas a la izquierda**: seleccionar, texto, imagen, código de barras, QR, emoji, marco, línea y plantillas.
- **Barra de formato según la selección**, como en Word: sin nada seleccionado muestra el tamaño de la etiqueta; con un texto, fuente, tamaño, negrita/cursiva/subrayado y alineación; con una imagen, sus opciones.
- **Pantalla Nuevo / Abrir** al iniciar, con los tamaños agrupados como plantillas (rollo, DYMO, papel y medida personalizada).
- **Rehacer**: Ctrl+Y. Ctrl+N abre la pantalla de nueva etiqueta.

## Nuevo en 0.29: impresión más nítida con controlador

Antes, la etiqueta se enviaba al controlador con bordes suavizados (tonos grises) y el controlador los convertía a puntos a su manera, por eso logotipos y textos salían algo borrosos. Ahora Impresorapp prepara la etiqueta exactamente a 203 ppp y en blanco y negro puro: cada punto del diseño coincide con un punto del cabezal.

- Texto, códigos y líneas: bordes limpios, sin grises.
- Imágenes: nueva opción **Calidad al imprimir** (panel de propiedades o botón derecho sobre la imagen):
  - **Automática** (por defecto): detecta si es un logotipo o una fotografía.
  - **Nítida**: blanco y negro puro con umbral automático. Ideal para logotipos, iconos, pictogramas y texto escaneado.
  - **Fotografía**: tramado de puntos (Floyd–Steinberg) para conservar degradados y grises.
- La vista previa de impresión muestra exactamente los puntos que se envían.

Si aún sale clara o con poco contraste, sube la densidad/temperatura en las preferencias del controlador Toshiba (no en Impresorapp).

## Nuevo en 0.28: editor más cómodo y edición Sencilla

**Menú del botón derecho.** Haz clic derecho sobre cualquier elemento del lienzo o de la lista «Elementos del diseño»:
- Imagen: reemplazar, voltear horizontal o verticalmente, girar 90° a la derecha o a la izquierda, girar 180°, mantener proporciones, recuperar la forma original y ocupar toda la etiqueta.
- Texto: editar, negrita, cursiva, subrayado y alineación.
- Todos: cortar, copiar, pegar, duplicar, colocar en la etiqueta (centrar, arriba, abajo…), orden delante/detrás y eliminar.
- En una zona vacía: añadir texto, imagen, QR, código, marco o línea justo en ese punto, y pegar.

**Imágenes.** La cinta «Inicio» muestra los botones de imagen al seleccionar una (y oculta los de texto, que no aplican). Puedes arrastrar un archivo de imagen al lienzo: sobre otra imagen la reemplaza; en una zona libre la añade. Ctrl+V pega una imagen copiada (por ejemplo, una captura) o un texto. Voltear y girar modifican los píxeles de la imagen (PNG sin pérdida), así que la vista previa y todas las formas de imprimir dan el mismo resultado y los diseños siguen abriéndose en versiones anteriores.

**Teclado.** Flechas: mover 0,5 mm (con Mayús, 5 mm). Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+D: copiar, cortar, pegar, duplicar. Intro: editar el texto. Esc: quitar la selección. El cursor cambia al pasar sobre un elemento o su esquina de tamaño.

**Impresorapp Sencilla.** Edición aparte, sin búsqueda de impresoras de red, envío directo ZPL ni DYMO por Bluetooth: imprime solo en las impresoras instaladas en Windows (incluidas las colas compartidas que ya tengas añadidas) y en papel. Se instala por separado (Impresorapp-Sencilla-0.28-Setup.exe) y puede convivir con Impresorapp completa. No busca actualizaciones automáticamente.

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

# Impresorapp 0.24 · Etiquetas con controlador

1. Instala o conecta la impresora del servidor en Windows.
2. En Impresorapp entra en Impresoras → Usar controlador de Windows / servidor.
3. Elige la cola Toshiba y pulsa Usar esta impresora. Queda guardada.
4. Diseña la etiqueta al tamaño del rollo y pulsa Imprimir. Revisa la vista previa y usa su botón Imprimir.

Con el controlador Toshiba TPCL la B-FV4 debe estar en TPCL; no necesita emulación Zebra para esta ruta. El programa no cambia ese ajuste de la máquina. Configura el rollo, 203 dpi, sensor y térmica directa en las preferencias del controlador. La etiqueta se imprime al 100 %, sin escalar. Si el controlador rechaza las medidas, crea/selecciona el formato correspondiente en sus preferencias y vuelve a intentarlo.

El servidor y el cliente pueden exigir permisos de instalación de controladores según la política de Windows. Las colas compartidas deben aparecer instaladas en este equipo. No se han enviado impresiones durante las pruebas del programa; prueba primero una etiqueta.

# Impresorapp 0.23

Descomprime toda esta carpeta y abre Impresorapp-0.23.exe. Conserva QRCoder.dll junto al ejecutable.

GUARDAR: abre un diálogo en Documentos\Diseños Etiquetas y crea la carpeta si hace falta. Puedes elegir otra carpeta o nombre. Cancelar conserva los cambios pendientes.

ACTUALIZACIONES: abre Ayuda > Buscar actualizaciones. El repositorio albertuxo085/Impresorapp ya está configurado. Puedes activar la búsqueda automática al abrir. Si hay una versión estable nueva, ofrece descargarla, comprobarla y reiniciar después de guardar los cambios. No actualiza controladores ni firmware. Requiere un repositorio público con una Release y sus archivos ZIP y SHA-256.

Se conservan los favoritos y rangos de las versiones EtiquetaLocal. Impresión directa USB pendiente. Guarda el diseño antes de cerrar la versión anterior y abrir esta.

Autor: Alberto Marín (albertuxo85). El instalador permite instalar para el usuario actual y crear accesos directos. Esta distribución se entrega sin firma Authenticode; Windows puede mostrar un aviso de editor desconocido.

Versión 0.18: franja de impresora compacta, en una sola línea y con letra más pequeña. Los nombres largos se acortan con puntos suspensivos; al pasar el ratón se muestra el texto completo.

Versión 0.19: en la cinta DYMO de 12 mm, el texto y los emojis nuevos usan el mismo tamaño (el mayor que cabe con un margen mínimo), quedan centrados en la cinta y se colocan uno tras otro con el mismo espacio. Imágenes, QR y códigos de barras nuevos ocupan la altura útil de la cinta.

Versión 0.20: interfaz reorganizada para que sea más fácil de usar, sin quitar ninguna opción. Menús Archivo, Editar, Impresoras y Ayuda; un solo botón Imprimir con la impresora activa a su lado; guía de 3 pasos; panel de bienvenida; propiedades por secciones en castellano (Sí/No); pestaña Posición; ventana Impresoras con tarjetas «En uso» y «Predeterminada», búsqueda en red con opciones avanzadas y la DYMO LetraTag guardada como una impresora más, con conexión por Bluetooth en 3 pasos. Mejoras para teclado y lectores de pantalla.

Versión 0.21: las imágenes y demás elementos pueden sobresalir de la etiqueta por cualquier lado (antes solo por la derecha y por abajo); solo se avisa si lo que se imprime queda fuera. El panel de bienvenida aparece únicamente al abrir el programa, no al crear una etiqueta nueva, y se puede cerrar con ✕.

Versión 0.22: en Mis impresoras se ve y se cambia el lenguaje de cada impresora (ZPL de Zebra o Toshiba con emulación Zebra) en «Ajustes de la impresora». Si una impresora no tiene emulación Zebra, el programa ofrece buscar su controlador en Windows Update o en Internet e imprimir a través de Windows. Al imprimir, si algo queda fuera de la etiqueta, se indica qué elemento es y se selecciona.

Versión 0.23: la ventana de impresión directa muestra siempre las opciones del controlador (imprimir con el controlador de Windows, buscarlo en Windows Update o en Internet). Si el envío por red falla, el programa las ofrece al momento.
