using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
namespace EtiquetaLocal {
 public class UsbPrinter {public string Name,Id,State,Manufacturer;public bool MissingDriver;}
 public static class UsbPrinters {
  public static bool IsPrinter(string id,string service,string[] compatible){
   return (id??"").StartsWith("USBPRINT\\",StringComparison.OrdinalIgnoreCase)
    ||(id??"").StartsWith("USB\\",StringComparison.OrdinalIgnoreCase)&&
      (String.Equals(service,"usbprint",StringComparison.OrdinalIgnoreCase)||(compatible??new string[0]).Any(x=>x.StartsWith("USB\\Class_07",StringComparison.OrdinalIgnoreCase)));
  }
  public static List<UsbPrinter> Read(){
   var found=new List<UsbPrinter>();
   var options=new EnumerationOptions{ReturnImmediately=false,Timeout=TimeSpan.FromSeconds(12)};
   using(var search=new ManagementObjectSearcher("root\\cimv2","SELECT Name,Manufacturer,PNPDeviceID,Service,CompatibleID,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Present = TRUE",options))
   using(var devices=search.Get())foreach(ManagementObject device in devices)using(device){
    string id=Convert.ToString(device["PNPDeviceID"]),service=Convert.ToString(device["Service"]);
    if(!IsPrinter(id,service,device["CompatibleID"] as string[]))continue;
    uint error=Convert.ToUInt32(device["ConfigManagerErrorCode"]);
    found.Add(new UsbPrinter{Name=String.IsNullOrWhiteSpace(Convert.ToString(device["Name"]))?"Impresora USB (modelo no identificado)":Convert.ToString(device["Name"]),Manufacturer=Convert.ToString(device["Manufacturer"]),MissingDriver=error==28,Id=id,State=error==0?"USB reconocido · controlador de impresión no verificado":error==28?"Falta el controlador (Windows: 28)":"Revisar dispositivo · Windows: "+error});
   }
   // The USB interface and its USBPRINT child can represent the same printer.
   // Prefer the named USBPRINT child when the device instance suffix matches.
   return found.Where(x=>!x.Id.StartsWith("USB\\",StringComparison.OrdinalIgnoreCase)||!found.Any(y=>y.Id.StartsWith("USBPRINT\\",StringComparison.OrdinalIgnoreCase)&&y.Id.Substring(y.Id.LastIndexOf('\\')+1)==x.Id.Substring(x.Id.LastIndexOf('\\')+1)))
    .GroupBy(x=>x.Id,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).OrderBy(x=>x.Name).ToList();
  }
  public static void SelfTest(){
   if(!IsPrinter("USBPRINT\\TOSHIBA\\ABC","",null)||!IsPrinter("USB\\VID_1234\\ABC","",new[]{"USB\\Class_07&SubClass_01"})||IsPrinter("USB\\VID_1234\\MOUSE","hidusb",new[]{"USB\\Class_03"})||IsPrinter("SWD\\PRINTENUM\\queue","",null))throw new Exception("Clasificación de impresoras USB incorrecta");
  }
  public static string Support(UsbPrinter printer){
   string name=((printer.Name??"")+" "+(printer.Manufacturer??"")+" "+(printer.Id??"")).ToUpperInvariant();
   if(name.Contains("TOSHIBA")||name.Contains("VID_08A6"))return "https://www.toshibatec.com/download_overseas/printer/printer_driver/";
   if(name.Contains("ZEBRA")||name.Contains("ZDESIGNER")||name.Contains("VID_0A5F"))return "https://www.zebra.com/us/en/support-downloads/printers.html";
   if(name.Contains("DYMO")||name.Contains("VID_0922"))return "https://www.dymo.com/support?cfid=online-support";
   if(name.Contains("BROTHER")||name.Contains("VID_04F9"))return "https://support.brother.com/g/b/productsearch.aspx?c=es&lang=es&content=dl";
   return null;
  }
 }
 public class UsbPrinterPanel:UserControl {
  readonly ListView devices=new ListView();readonly System.Windows.Forms.Label status=new System.Windows.Forms.Label();readonly Button refresh;
  bool busy;
  public UsbPrinterPanel(){
   Dock=DockStyle.Fill;BackColor=Color.White;
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=130,Padding=new Padding(12)};
   var hint=new System.Windows.Forms.Label{Text="Impresoras conectadas por USB a este equipo. Conecta y enciende la impresora y pulsa Actualizar.",Width=870,Height=30};top.Controls.Add(hint);top.SetFlowBreak(hint,true);
   refresh=Ui.Action("usb","Actualizar impresoras USB",async()=>await RefreshDevices(),245);top.Controls.Add(refresh);
   var copy=Ui.Action("copy","Copiar identificación",()=>{if(devices.SelectedItems.Count>0)Clipboard.SetText(devices.SelectedItems[0].SubItems[2].Text);},210);top.Controls.Add(copy);
   top.Controls.Add(Ui.Action("open","Descarga oficial…",OfficialDriver,195));top.Controls.Add(Ui.Action("save","Guardar en Mis impresoras",SaveUsb,245));
   devices.Dock=DockStyle.Fill;devices.View=View.Details;devices.FullRowSelect=true;devices.Columns.Add("Impresora / dispositivo",290);devices.Columns.Add("Estado",260);devices.Columns.Add("Identificación USB",500);
   var bottom=new Panel{Dock=DockStyle.Bottom,Height=85,Padding=new Padding(12)};
   status.Text="Pulsa Actualizar para consultar los dispositivos presentes.";status.Dock=DockStyle.Top;status.Height=25;
   var note=new System.Windows.Forms.Label{Text="Se avisa si Windows declara que falta el controlador. USB reconocido no confirma un controlador de impresión del fabricante. Descarga oficial abre su web para elegir modelo y Windows; no instala ni actualiza automáticamente. La impresión USB está pendiente; DYMO LetraTag usa Bluetooth.",Dock=DockStyle.Fill};
   bottom.Controls.Add(note);bottom.Controls.Add(status);Controls.Add(devices);Controls.Add(top);Controls.Add(bottom);
  }
  public async Task RefreshDevices(){
   if(busy)return;busy=true;refresh.Enabled=false;status.Text="Consultando impresoras USB…";
   try{var list=await Task.Run(()=>UsbPrinters.Read());if(IsDisposed)return;devices.Items.Clear();foreach(var p in list){var row=new ListViewItem(new[]{p.Name,p.State,p.Id}){Tag=p};if(p.MissingDriver)row.ForeColor=Color.Firebrick;devices.Items.Add(row);}status.Text=list.Count==0?"No se han identificado impresoras USB conectadas. Comprueba alimentación y cable; algunos dispositivos no declaran su clase de impresora.":list.Count+" dispositivo(s) USB · "+list.Count(p=>p.MissingDriver)+" sin controlador asociado. Selecciona uno para consultar la descarga oficial.";}
   catch(Exception ex){if(!IsDisposed)status.Text="No se pudo consultar USB: "+ex.GetBaseException().Message;}
   finally{busy=false;if(!IsDisposed)refresh.Enabled=true;}
  }
  void SaveUsb(){try{if(devices.SelectedItems.Count==0){MessageBox.Show(this,"Selecciona una impresora USB.");return;}var usb=devices.SelectedItems[0].Tag as UsbPrinter;if(usb==null)return;var p=new SavedPrinter{Alias=usb.Name,Model=usb.Name,Address=usb.Id,Connection="USB"};if(!SavedPrinters.EditName(this,p))return;var book=SavedPrinters.Load();SavedPrinters.Put(book,p,true);SavedPrinters.Save(book);status.Text="Conexión USB guardada. La impresión directa USB aún está pendiente; puedes usar LAN si está disponible.";}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void OfficialDriver(){
   if(devices.SelectedItems.Count==0){MessageBox.Show(this,"Selecciona primero una impresora USB de la lista.","Controlador oficial");return;}
   var printer=devices.SelectedItems[0].Tag as UsbPrinter;if(printer==null)return;string url=UsbPrinters.Support(printer);
   if(url==null){MessageBox.Show(this,"Modelo detectado: "+printer.Name+"\n\nNo se ha identificado una web oficial verificada para este fabricante. Copia su identificación y comprueba el modelo de la etiqueta física antes de descargar un controlador.","Fabricante no identificado");return;}
   if(MessageBox.Show(this,"Dispositivo: "+printer.Name+"\n\nSe abrirá la web oficial para seleccionar el modelo exacto y tu versión de Windows. Descarga el paquete del fabricante; no se instalará ni se actualizará automáticamente. Si el nombre es genérico, confirma el modelo en la impresora.\n\n"+url,"Descarga del fabricante",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
   try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,"No se pudo abrir la web: "+ex.Message);}
  }
 }
}
