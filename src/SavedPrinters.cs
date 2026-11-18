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
  // true si la impresora NO tiene la emulación Zebra (ZPL): entonces se imprime con su controlador de Windows.
  [DataMember] public bool NoZpl;
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
  public static readonly string[] Languages={"ZPL · Zebra o compatible (incluye Toshiba con emulación Zebra)","Toshiba B-FV4D-GS · emulación Zebra (ZPL)"};
  // Ajustes de una impresora: nombre, ubicación, lenguaje y emulación Zebra (ZPL).
  public static bool EditName(IWin32Window owner,SavedPrinter printer){using(var d=new Form{Text="Ajustes de la impresora",ClientSize=new Size(560,440),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=new Font("Segoe UI",9.5f),Icon=Branding.AppIcon}){
   var name=new TextBox{Text=printer.Alias,Left=18,Top=40,Width=520,MaxLength=120,AccessibleName="Nombre de la impresora"};var location=new TextBox{Text=printer.Location,Left=18,Top=100,Width=520,MaxLength=160,AccessibleName="Ubicación"};
   d.Controls.Add(new System.Windows.Forms.Label{Text="Nombre fácil de reconocer (por ejemplo, Toshiba almacén)",Left=18,Top=16,Width=520});d.Controls.Add(name);d.Controls.Add(new System.Windows.Forms.Label{Text="Ubicación en este programa (no modifica la impresora)",Left=18,Top=76,Width=520});d.Controls.Add(location);
   d.Controls.Add(new System.Windows.Forms.Label{Text="Lenguaje de impresión",Left=18,Top=140,Width=520});
   var language=new ComboBox{Left=18,Top=164,Width=520,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Lenguaje de impresión"};language.Items.AddRange(Languages);language.SelectedIndex=Math.Max(0,Math.Min(1,printer.Profile));d.Controls.Add(language);
   var zpl=new CheckBox{Text="La impresora tiene activada la emulación Zebra (ZPL)",Left=18,Top=204,Width=520,Checked=!printer.NoZpl};d.Controls.Add(zpl);
   var help=new Panel{Left=18,Top=234,Width=520,Height=140,BackColor=Color.FromArgb(255,246,230)};d.Controls.Add(help);
   help.Controls.Add(new System.Windows.Forms.Label{Text="Sin emulación Zebra no se puede enviar la etiqueta directamente: instala el controlador y se imprimirá a través de Windows (Archivo › Imprimir en papel). Muchas Toshiba pueden activar la emulación Zebra desde su menú o su página web.",Left=12,Top=10,Width=496,Height=80,ForeColor=Color.FromArgb(107,68,0)});
   var update=new Button{Text="Buscar controlador en Windows Update",Left=12,Top=94,Width=250,Height=34};update.Click+=(s,e)=>DriverHelp.OpenWindowsUpdate(d);help.Controls.Add(update);
   var web=new Button{Text="Buscar controlador en Internet",Left=270,Top=94,Width=238,Height=34};web.Click+=(s,e)=>DriverHelp.SearchOnline(d,String.IsNullOrWhiteSpace(printer.Model)?name.Text:printer.Model);help.Controls.Add(web);
   help.Visible=!zpl.Checked;zpl.CheckedChanged+=(s,e)=>help.Visible=!zpl.Checked;
   var ok=new Button{Text="Guardar",Left=340,Top=392,Width=95,Height=32};var cancel=new Button{Text="Cancelar",Left=443,Top=392,Width=95,Height=32,DialogResult=DialogResult.Cancel};ok.Click+=(s,e)=>{if(String.IsNullOrWhiteSpace(name.Text)){MessageBox.Show(d,"Escribe un nombre para identificarla.");return;}d.DialogResult=DialogResult.OK;};d.Controls.Add(ok);d.Controls.Add(cancel);d.AcceptButton=ok;d.CancelButton=cancel;if(d.ShowDialog(owner)!=DialogResult.OK)return false;
   printer.Alias=name.Text.Trim();printer.Location=location.Text.Trim();printer.NoZpl=!zpl.Checked;if(printer.Profile!=language.SelectedIndex){printer.Profile=language.SelectedIndex;printer.MaxWidth=printer.Profile==1?101.6m:104m;if(printer.Profile==1)printer.Dots=8;printer.Configured=false;}return true;
  }}
  public static void SelfTest(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"printers-test-"+Guid.NewGuid().ToString("N")+".json");try{
   var book=new PrinterBook();var a=Put(book,new SavedPrinter{Alias="Almacén",Address="192.0.2.8",Configured=true,Profile=1,MaxWidth=101.6m},true);Save(book,path);book=Load(path);if(book.Preferred==null||book.Preferred.Alias!="Almacén"||!book.Preferred.Configured)throw new Exception("No se recupera la impresora guardada");
   var b=Put(book,new SavedPrinter{Alias="Nueva ficha",Address="192.0.2.8"},true);if(book.Printers.Count!=1||b.Id!=a.Id||book.DefaultId!=a.Id)throw new Exception("Duplicación de impresora");Put(book,new SavedPrinter{Alias="USB almacén",Address="USBPRINT\\PRINTER\\TEST",Connection="USB"},false);Save(book,path);if(Load(path).Printers.Count!=2)throw new Exception("Persistencia LAN y USB");
   Put(book,new SavedPrinter{Alias="TEC sin ZPL",Model="TEC B-SA4T",Address="192.0.2.9",NoZpl=true},false);Save(book,path);var reloaded=Load(path).Printers.FirstOrDefault(p=>p.Address=="192.0.2.9");if(reloaded==null||!reloaded.NoZpl)throw new Exception("No se guarda la falta de emulación Zebra");if(!DriverHelp.SearchUrl("TEC B-SA4T").Contains("TEC%20B-SA4T%20driver"))throw new Exception("Búsqueda de controlador");
  }finally{if(File.Exists(path))File.Delete(path);}}
 }
 public class SavedPrinterPanel:UserControl {
  readonly PrinterCardList list=new PrinterCardList();readonly System.Windows.Forms.Label status=new System.Windows.Forms.Label();public event Action<SavedPrinter> SelectedPrinter;public event Action DymoConnect,DymoUse;
  readonly System.Windows.Forms.Label dymoTitle=new System.Windows.Forms.Label(),dymoText=new System.Windows.Forms.Label();readonly Button dymoMain=new Button(),dymoOther=new Button();
  public SavedPrinterPanel(){Dock=DockStyle.Fill;BackColor=Color.FromArgb(247,250,248);Font=new Font("Segoe UI",9.5f);
   var top=new Panel{Dock=DockStyle.Top,Height=58,Padding=new Padding(14,12,14,8)};var hint=new System.Windows.Forms.Label{Text="Las impresoras que usas habitualmente. Elige una y pulsa Usar ahora (o haz doble clic).",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Theme.Muted};var add=Ui.Action("file","Añadir por IP",Manual,150);add.Dock=DockStyle.Right;top.Controls.Add(hint);top.Controls.Add(add);
   list.Dock=DockStyle.Fill;list.DoubleClick+=(s,e)=>Use();list.AccessibleName="Mis impresoras";list.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Use();e.Handled=true;}};
   var listHost=new Panel{Dock=DockStyle.Fill,Padding=new Padding(10,0,10,0)};listHost.Controls.Add(list);
   // Tarjeta de la DYMO LetraTag (por Bluetooth).
   var dymo=new Panel{Dock=DockStyle.Bottom,Height=84,Padding=new Padding(14,8,14,8)};var dymoCard=new Panel{Dock=DockStyle.Fill,BackColor=Color.White};dymo.Controls.Add(dymoCard);
   dymoCard.Paint+=(s,e)=>{using(var pen=new Pen(Color.FromArgb(156,195,172),1.5f)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;e.Graphics.DrawRectangle(pen,1,1,dymoCard.Width-3,dymoCard.Height-3);}using(var im=Ui.Icon("bluetooth",Theme.Brand))e.Graphics.DrawImage(im,18,(dymoCard.Height-20)/2);};
   dymoTitle.Location=new Point(52,10);dymoTitle.AutoSize=true;dymoTitle.Font=new Font("Segoe UI",10.5f,FontStyle.Bold);dymoTitle.ForeColor=Theme.Ink;dymoCard.Controls.Add(dymoTitle);
   dymoText.Location=new Point(52,34);dymoText.AutoSize=true;dymoText.ForeColor=Theme.Muted;dymoCard.Controls.Add(dymoText);
   dymoMain.Size=new Size(170,34);dymoMain.Anchor=AnchorStyles.Right;dymoMain.FlatStyle=FlatStyle.Flat;dymoMain.FlatAppearance.BorderColor=Theme.Brand;dymoMain.ForeColor=Theme.BrandDark;dymoMain.BackColor=Color.White;dymoMain.Font=new Font("Segoe UI",9.5f,FontStyle.Bold);dymoMain.Image=Ui.Icon("bluetooth",Theme.Brand);dymoMain.TextImageRelation=TextImageRelation.ImageBeforeText;dymoCard.Controls.Add(dymoMain);
   dymoOther.Size=new Size(130,34);dymoOther.Anchor=AnchorStyles.Right;dymoOther.Text="Conectar otra";dymoOther.FlatStyle=FlatStyle.Flat;dymoOther.FlatAppearance.BorderColor=Color.FromArgb(185,204,191);dymoOther.BackColor=Color.White;dymoCard.Controls.Add(dymoOther);
   dymoCard.Resize+=(s,e)=>{dymoMain.Location=new Point(dymoCard.Width-dymoMain.Width-16,(dymoCard.Height-dymoMain.Height)/2);dymoOther.Location=new Point(dymoMain.Left-dymoOther.Width-8,dymoMain.Top);};
   dymoMain.Click+=(s,e)=>{if(DymoStore.Load()!=null){if(DymoUse!=null)DymoUse();}else if(DymoConnect!=null)DymoConnect();};dymoOther.Click+=(s,e)=>{if(DymoConnect!=null)DymoConnect();};
   // Acciones sobre la impresora elegida.
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=58,Padding=new Padding(14,10,14,8),FlowDirection=FlowDirection.RightToLeft,BackColor=Color.White};
   var use=Ui.Action("print","Usar ahora",Use,140,true);bottom.Controls.Add(use);bottom.Controls.Add(Ui.Action("delete","Quitar de la lista",Remove,170));bottom.Controls.Add(Ui.Action("save","Hacer predeterminada",Default,190));bottom.Controls.Add(Ui.Action("edit","Ajustes de la impresora",Edit,215));
   status.Dock=DockStyle.Bottom;status.Height=40;status.Padding=new Padding(14,4,14,4);status.ForeColor=Theme.Muted;
   Controls.Add(listHost);Controls.Add(top);Controls.Add(dymo);Controls.Add(status);Controls.Add(bottom);VisibleChanged+=(s,e)=>{if(Visible)RefreshList();};RefreshList();
  }
  SavedPrinter Selected {get{return list.SelectedItem as SavedPrinter;}}
  public void RefreshList(){try{var b=SavedPrinters.Load();var keep=Selected==null?null:Selected.Id;list.DefaultId=b.DefaultId??"";list.InUseId=b.Preferred==null?"":b.Preferred.Id;list.Items.Clear();foreach(var p in b.Printers)list.Items.Add(p);
    var again=list.Items.Cast<SavedPrinter>().FirstOrDefault(p=>p.Id==(keep??list.InUseId));if(again!=null)list.SelectedItem=again;
    status.Text=b.Printers.Count==0?"Todavía no hay impresoras guardadas. Búscalas en «Buscar en la red» o añádelas por IP.":"En uso: la que recibirá la próxima impresión.   ★ Predeterminada: la que se elige al abrir el programa.";}catch(Exception ex){status.Text=ex.Message;}
   var dymo=DymoStore.Load();if(dymo==null){dymoTitle.Text="¿Usas una DYMO LetraTag?";dymoText.Text="Se conecta por Bluetooth. Al conectarla, el diseño cambia solo a cinta de 12 mm.";dymoMain.Text=" Conectar mi DYMO";dymoOther.Visible=false;}
   else{dymoTitle.Text="DYMO LetraTag · "+dymo.Name;dymoText.Text="Por Bluetooth · Guardada en este equipo. Al usarla, el diseño pasa a cinta de 12 mm.";dymoMain.Text=" Usar la DYMO";dymoOther.Visible=true;}
   dymoMain.AccessibleName=dymoMain.Text.Trim();}
  void Use(){try{var p=Selected;if(p==null)return;var b=SavedPrinters.Load();b.SelectedId=p.Id;SavedPrinters.Save(b);if(SelectedPrinter!=null)SelectedPrinter(p);}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Default(){try{var p=Selected;if(p==null)return;var b=SavedPrinters.Load();b.DefaultId=b.SelectedId=p.Id;SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Edit(){try{var p=Selected;if(p==null||!SavedPrinters.EditName(this,p))return;var b=SavedPrinters.Load();SavedPrinters.Put(b,p,false);SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Remove(){try{var p=Selected;if(p==null||MessageBox.Show(this,"¿Quitar "+p.Alias+" de Mis impresoras? No se desinstala ni cambia la impresora.","Quitar de favoritos",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;var b=SavedPrinters.Load();b.Printers.RemoveAll(x=>x.Id==p.Id);if(b.DefaultId==p.Id)b.DefaultId="";if(b.SelectedId==p.Id)b.SelectedId="";SavedPrinters.Save(b);RefreshList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
  void Manual(){using(var d=new Form{Text="Añadir impresora de red",ClientSize=new Size(410,150),StartPosition=FormStartPosition.CenterParent,Font=Font,Icon=Branding.AppIcon}){var ip=new TextBox{Left=18,Top=42,Width=365};d.Controls.Add(new System.Windows.Forms.Label{Text="IP o nombre de la impresora (sin puerto)",Left=18,Top=18,Width=365});d.Controls.Add(ip);var ok=new Button{Text="Continuar",Left=280,Top=92,Width=105};d.Controls.Add(ok);ok.Click+=(s,e)=>{try{var p=new SavedPrinter{Alias=ip.Text.Trim(),Address=ip.Text.Trim()};SavedPrinters.Validate(p);if(!SavedPrinters.EditName(d,p))return;var b=SavedPrinters.Load();SavedPrinters.Put(b,p,true);SavedPrinters.Save(b);d.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(d,ex.Message);}};d.AcceptButton=ok;d.ShowDialog(this);RefreshList();}}
 }
}
