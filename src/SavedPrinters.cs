using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
namespace EtiquetaLocal {
 [DataContract] public class SavedPrinter {
  [DataMember] public string Id=Guid.NewGuid().ToString("N"),Alias="",Model="",Location="",Connection="LAN",Address="";
  [DataMember] public int Port=9100,Profile=0,Dots=8;
  [DataMember] public decimal MaxWidth=104;
  [DataMember] public bool Configured;
  public override string ToString(){return Alias+" · "+Connection+(Connection=="USB"?" (impresión pendiente)":"");}
 }
 [DataContract] public class PrinterBook {
  [DataMember] public List<SavedPrinter> Printers=new List<SavedPrinter>();
  [DataMember] public string DefaultId="",SelectedId="";
  public SavedPrinter Preferred {get {return Printers.FirstOrDefault(p=>p.Id==SelectedId)??Printers.FirstOrDefault(p=>p.Id==DefaultId)??Printers.FirstOrDefault();}}
 }
 public static class SavedPrinters {
  public static string PathName {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtiquetaLocal","printers.json");}}
  public static PrinterBook Load(){return Load(PathName);}
  public static PrinterBook Load(string path){if(!File.Exists(path))return new PrinterBook();try{using(var f=File.OpenRead(path)){var b=(PrinterBook)new DataContractJsonSerializer(typeof(PrinterBook)).ReadObject(f);if(b==null||b.Printers==null)throw new Exception();foreach(var p in b.Printers)Validate(p);return b;}}catch{throw new Exception("No se pudo leer Mis impresoras. Conserva una copia de "+path+" antes de repararlo.");}}
  public static void Validate(SavedPrinter p){if(p==null||String.IsNullOrWhiteSpace(p.Id)||String.IsNullOrWhiteSpace(p.Alias)||String.IsNullOrWhiteSpace(p.Address)||p.Port<1||p.Port>65535||p.Profile<0||p.Profile>1||!new[]{8,12,24}.Contains(p.Dots)||p.MaxWidth<5||p.MaxWidth>1000||!new[]{"LAN","USB"}.Contains(p.Connection))throw new Exception("Ficha de impresora no válida.");if(p.Connection=="LAN"&&!System.Text.RegularExpressions.Regex.IsMatch(p.Address,@"^[A-Za-z0-9][A-Za-z0-9.\-]{0,252}$"))throw new Exception("IP o nombre de impresora no válido.");}
  public static void Save(PrinterBook book){Save(book,PathName);}
  public static void Save(PrinterBook book,string path){foreach(var p in book.Printers)Validate(p);Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var f=File.Create(temp))new DataContractJsonSerializer(typeof(PrinterBook)).WriteObject(f,book);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static SavedPrinter Put(PrinterBook book,SavedPrinter candidate,bool select){Validate(candidate);var old=book.Printers.FirstOrDefault(p=>p.Connection==candidate.Connection&&p.Port==candidate.Port&&String.Equals(p.Address,candidate.Address,StringComparison.OrdinalIgnoreCase));if(old!=null){candidate.Id=old.Id;book.Printers.Remove(old);}book.Printers.Add(candidate);if(String.IsNullOrEmpty(book.DefaultId))book.DefaultId=candidate.Id;if(select)book.SelectedId=candidate.Id;return candidate;}
  public static SavedPrinter FromNetwork(Found f){bool toshiba=f.Name.IndexOf("B-FV4",StringComparison.OrdinalIgnoreCase)>=0;return new SavedPrinter{Alias=String.IsNullOrWhiteSpace(f.DeviceName)?f.Name:f.DeviceName,Model=f.Name,Location=f.Location??"",Address=f.Address,Port=f.Ports.Contains(9100)?9100:f.Ports.Contains(8000)?8000:9100,Profile=toshiba?1:0,MaxWidth=toshiba?101.6m:104m};}
  public static SavedPrinter AddNetwork(Found f){if(f==null||f.Kind=="Compartida"||!f.Verified||!f.Ports.Any(p=>p==9100||p==8000))throw new Exception("Para impresión directa selecciona una impresora identificada con puerto RAW. Las impresoras de oficina se eligen en Imprimir en papel.");var book=Load();var old=book.Printers.FirstOrDefault(p=>p.Connection=="LAN"&&p.Address==f.Address);var result=Put(book,old??FromNetwork(f),true);Save(book);return result;}
  public static bool EditName(IWin32Window owner,SavedPrinter printer){using(var d=new Form{Text="Guardar impresora",ClientSize=new Size(460,210),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=new Font("Segoe UI",9),Icon=Branding.AppIcon}){
   var name=new TextBox{Text=printer.Alias,Left=18,Top=42,Width=420,MaxLength=120};var location=new TextBox{Text=printer.Location,Left=18,Top=105,Width=420,MaxLength=160};
   d.Controls.Add(new System.Windows.Forms.Label{Text="Nombre fácil de reconocer (por ejemplo, Toshiba almacén)",Left=18,Top=16,Width=420});d.Controls.Add(name);d.Controls.Add(new System.Windows.Forms.Label{Text="Ubicación en este programa (no modifica la impresora)",Left=18,Top=79,Width=420});d.Controls.Add(location);
   var ok=new Button{Text="Guardar",Left=240,Top=160,Width=95};var cancel=new Button{Text="Cancelar",Left=345,Top=160,Width=95,DialogResult=DialogResult.Cancel};ok.Click+=(s,e)=>{if(String.IsNullOrWhiteSpace(name.Text)){MessageBox.Show(d,"Escribe un nombre para identificarla.");return;}d.DialogResult=DialogResult.OK;};d.Controls.Add(ok);d.Controls.Add(cancel);d.AcceptButton=ok;d.CancelButton=cancel;if(d.ShowDialog(owner)!=DialogResult.OK)return false;printer.Alias=name.Text.Trim();printer.Location=location.Text.Trim();return true;
  }}
  public static void SelfTest(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"printers-test-"+Guid.NewGuid().ToString("N")+".json");try{
   var book=new PrinterBook();var a=Put(book,new SavedPrinter{Alias="Almacén",Address="192.0.2.8",Configured=true,Profile=1,MaxWidth=101.6m},true);Save(book,path);book=Load(path);if(book.Preferred==null||book.Preferred.Alias!="Almacén"||!book.Preferred.Configured)throw new Exception("No se recupera la impresora guardada");
   var b=Put(book,new SavedPrinter{Alias="Nueva ficha",Address="192.0.2.8"},true);if(book.Printers.Count!=1||b.Id!=a.Id||book.DefaultId!=a.Id)throw new Exception("Duplicación de impresora");Put(book,new SavedPrinter{Alias="USB almacén",Address="USBPRINT\\PRINTER\\TEST",Connection="USB"},false);Save(book,path);if(Load(path).Printers.Count!=2)throw new Exception("Persistencia LAN y USB");
  }finally{if(File.Exists(path))File.Delete(path);}}
 }
 public class SavedPrinterPanel:UserControl {
  readonly ListView list=new ListView();readonly System.Windows.Forms.Label status=new System.Windows.Forms.Label();public event Action<SavedPrinter> SelectedPrinter;
  public SavedPrinterPanel(){Dock=DockStyle.Fill;BackColor=Color.White;var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=92,Padding=new Padding(12)};var hint=new System.Windows.Forms.Label{Text="Tus impresoras habituales. Doble clic para seleccionar; no se realiza una búsqueda de red.",Width=870,Height=25};top.Controls.Add(hint);top.SetFlowBreak(hint,true);
   top.Controls.Add(Ui.Action("print","Usar seleccionada",Use,170));top.Controls.Add(Ui.Action("save","Predeterminada",Default,155));top.Controls.Add(Ui.Action("edit","Nombre y ubicación",Edit,180));top.Controls.Add(Ui.Action("delete","Quitar de la lista",Remove,165));top.Controls.Add(Ui.Action("file","Añadir por IP",Manual,145));
   list.Dock=DockStyle.Fill;list.View=View.Details;list.FullRowSelect=true;list.Columns.Add("Nombre",230);list.Columns.Add("Conexión",75);list.Columns.Add("Dirección / dispositivo",200);list.Columns.Add("Ubicación local",180);list.Columns.Add("Estado guardado",270);list.DoubleClick+=(s,e)=>Use();
   status.Dock=DockStyle.Bottom;status.Height=42;status.Padding=new Padding(10);Controls.Add(list);Controls.Add(top);Controls.Add(status);VisibleChanged+=(s,e)=>{if(Visible)RefreshList();};RefreshList();
  }
  SavedPrinter Selected {get{return list.SelectedItems.Count==0?null:list.SelectedItems[0].Tag as SavedPrinter;}}
  public void RefreshList(){try{var b=SavedPrinters.Load();list.Items.Clear();foreach(var p in b.Printers){var row=new ListViewItem(new[]{(p.Id==b.DefaultId?"★ ":"")+p.Alias,p.Connection,p.Address,p.Location,p.Connection=="USB"?"Impresión USB pendiente":p.Configured?"Perfil de impresión guardado":"Revisar perfil en la primera impresión"}){Tag=p};list.Items.Add(row);}status.Text=b.Printers.Count==0?"Guarda una impresora desde Red o USB, o añádela por IP.":"Preferida: "+(b.Preferred==null?"ninguna":b.Preferred.Alias)+". La ubicación local solo se guarda en este equipo.";}catch(Exception ex){status.Text=ex.Message;}}
  void Use(){try{var p=Selected;if(p==null)return;var b=SavedPrinters.Load();b.SelectedId=p.Id;SavedPrinters.Save(b);if(SelectedPrinter!=null)SelectedPrinter(p);}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Default(){try{var p=Selected;if(p==null)return;var b=SavedPrinters.Load();b.DefaultId=b.SelectedId=p.Id;SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Edit(){try{var p=Selected;if(p==null||!SavedPrinters.EditName(this,p))return;var b=SavedPrinters.Load();SavedPrinters.Put(b,p,false);SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Remove(){try{var p=Selected;if(p==null||MessageBox.Show(this,"¿Quitar "+p.Alias+" de Mis impresoras? No se desinstala ni cambia la impresora.","Quitar de favoritos",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;var b=SavedPrinters.Load();b.Printers.RemoveAll(x=>x.Id==p.Id);if(b.DefaultId==p.Id)b.DefaultId="";if(b.SelectedId==p.Id)b.SelectedId="";SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Manual(){using(var d=new Form{Text="Añadir impresora de red",ClientSize=new Size(410,150),StartPosition=FormStartPosition.CenterParent,Font=Font,Icon=Branding.AppIcon}){var ip=new TextBox{Left=18,Top=42,Width=365};d.Controls.Add(new System.Windows.Forms.Label{Text="IP o nombre de la impresora (sin puerto)",Left=18,Top=18,Width=365});d.Controls.Add(ip);var ok=new Button{Text="Continuar",Left=280,Top=92,Width=105};d.Controls.Add(ok);ok.Click+=(s,e)=>{try{var p=new SavedPrinter{Alias=ip.Text.Trim(),Address=ip.Text.Trim()};SavedPrinters.Validate(p);if(!SavedPrinters.EditName(d,p))return;var b=SavedPrinters.Load();SavedPrinters.Put(b,p,true);SavedPrinters.Save(b);d.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(d,ex.Message);}};d.AcceptButton=ok;d.ShowDialog(this);RefreshList();}}
 }
}
