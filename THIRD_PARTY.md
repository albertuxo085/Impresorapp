# Referencias de protocolos

- ZPL: https://docs.zebra.com/us/en/printers/software/zpl-pg/c-zpl-zpl-commands.html
- SNMP y Host Resources MIB: https://www.rfc-editor.org/rfc/rfc1157 y https://www.rfc-editor.org/rfc/rfc2790
- Referencias para el protocolo Bluetooth de LetraTag: https://github.com/ysfchn/dymo-bluetooth y https://thermal-label.github.io/letratag/protocol/letratag-bt

No se incluyen firmware, instaladores ni controladores de los fabricantes. Las referencias no implican afiliacion, certificacion o soporte oficial. Windows, DYMO, Toshiba y Zebra son marcas de sus respectivos titulares. Las bibliotecas del sistema se referencian desde la instalacion local de Windows, sin redistribuirlas en este repositorio.

## Generación QR

QRCoder 1.4.3, por Raffael Herrmann y colaboradores. Licencia MIT incluida en lib/QRCoder-LICENSE.txt y en la distribución. DLL net40 obtenida del paquete oficial https://www.nuget.org/packages/QRCoder/1.4.3 (https://github.com/codebude/QRCoder). La biblioteca se ejecuta localmente; no recibe actualizaciones automáticas.

## Construcción del instalador

Inno Setup 7.1.1, de sus autores y colaboradores: https://jrsoftware.org/inno-setup y https://github.com/jrsoftware/issrc. Se utiliza como herramienta de compilación y no se incluye su compilador en el repositorio. El autor de Impresorapp es Alberto Marín (albertuxo85); la autoría de las dependencias se mantiene separada.
