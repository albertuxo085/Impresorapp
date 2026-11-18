using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth.Advertisement;

namespace EtiquetaLocal {
 // Colores comunes de la interfaz.
 public static class Theme {
  public static readonly Color Brand=Color.FromArgb(37,108,76),BrandDark=Color.FromArgb(29,90,62),Ink=Color.FromArgb(31,51,41),Muted=Color.FromArgb(74,93,82),Line=Color.FromArgb(211,226,216),Surface=Color.FromArgb(244,248,245),Soft=Color.FromArgb(227,240,232),Active=Color.FromArgb(205,230,214),Warn=Color.FromArgb(122,75,0);
 }

 // Panel de propiedades: «Sí/No» en lugar de True/False.
 public class SiNoConverter:BooleanConverter {
  public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture,object value,Type destinationType){if(destinationType==typeof(string)&&value is bool)return (bool)value?"Sí":"No";return base.ConvertTo(context,culture,value,destinationType);}
  public override object ConvertFrom(ITypeDescriptorContext context,CultureInfo culture,object value){var text=value as string;if(text!=null){text=text.Trim();if(text.Equals("Sí",StringComparison.OrdinalIgnoreCase)||text.Equals("Si",StringComparison.OrdinalIgnoreCase)||text.Equals("True",StringComparison.OrdinalIgnoreCase))return true;if(text.Equals("No",StringComparison.OrdinalIgnoreCase)||text.Equals("False",StringComparison.OrdinalIgnoreCase))return false;}return base.ConvertFrom(context,culture,value);}
 }
 public class AlignConverter:StringConverter {
  public override bool GetStandardValuesSupported(ITypeDescriptorContext context){return true;}
  public override bool GetStandardValuesExclusive(ITypeDescriptorContext context){return true;}
  public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context){return new StandardValuesCollection(new[]{"Izquierda","Centro","Derecha"});}
 }
 public class FontNameConverter:StringConverter {
  static string[] names;
  public override bool GetStandardValuesSupported(ITypeDescriptorContext context){return true;}
  public override bool GetStandardValuesExclusive(ITypeDescriptorContext context){return false;}
  public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context){if(names==null)names=System.Drawing.FontFamily.Families.Select(f=>f.Name).OrderBy(n=>n).ToArray();return new StandardValuesCollection(names);}
 }

 // Botón «Imprimir en …» que muestra la impresora activa en dos líneas.
 public class PrinterChip:Button {
  string caption="Imprimir en",value="";
  public string Caption {get{return caption;}set{caption=value??"";Invalidate();}}
  public string Value {get{return value;}set{this.value=value??"";AccessibleName=caption+" "+this.value+". Pulsa para cambiar de impresora.";Invalidate();}}
  public string Kind="network";
  public PrinterChip(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderColor=Color.FromArgb(185,204,191);BackColor=Theme.Surface;Height=48;Width=320;Cursor=Cursors.Hand;Margin=new Padding(0,0,8,0);Text="";AccessibleRole=AccessibleRole.PushButton;}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var icon=Ui.Icon(Kind,Theme.Brand))g.DrawImage(icon,12,(Height-20)/2);
   var small=new Rectangle(42,6,Width-72,16);var big=new Rectangle(42,22,Width-72,20);
   TextRenderer.DrawText(g,caption,new Font("Segoe UI",8.5f),small,Theme.Muted,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   using(var bold=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(g,value,bold,big,Theme.Ink,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   using(var pen=new Pen(Theme.Ink,1.6f)){int x=Width-22,y=Height/2-2;g.DrawLines(pen,new[]{new Point(x-5,y-2),new Point(x,y+3),new Point(x+5,y-2)});}
   if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6));
  }
 }

 // Guía de tres pasos: tamaño → contenido → imprimir.
 public class StepsBar:Control {
  int current=2;string tip="";
  public int Current {get{return current;}set{if(current==value)return;current=value;UpdateAccessible();Invalidate();}}
  public string Tip {get{return tip;}set{tip=value??"";Invalidate();}}
  static readonly string[] Names={"Elige el tamaño","Añade contenido","Imprime"};
  public StepsBar(){Dock=DockStyle.Top;Height=40;BackColor=Theme.Surface;DoubleBuffered=true;Font=new Font("Segoe UI",9.5f);AccessibleRole=AccessibleRole.StaticText;UpdateAccessible();}
  void UpdateAccessible(){AccessibleName="Paso "+current+" de 3: "+Names[current-1];}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;int x=16,mid=Height/2;
   using(var line=new Pen(Theme.Line))g.DrawLine(line,0,Height-1,Width,Height-1);
   for(int i=0;i<3;i++){
    int step=i+1;bool done=step<current,now=step==current;
    var text=Names[i];var font=now?new Font(Font,FontStyle.Bold):Font;var size=TextRenderer.MeasureText(text,font);
    if(now){var pill=new Rectangle(x-4,mid-14,size.Width+40,28);using(var path=Round(pill,14))using(var fill=new SolidBrush(Color.White))using(var border=new Pen(Color.FromArgb(156,195,172))){g.FillPath(fill,path);g.DrawPath(border,path);}}
    var dot=new Rectangle(x,mid-11,22,22);
    if(done||now)using(var b=new SolidBrush(Theme.Brand))g.FillEllipse(b,dot);else using(var p=new Pen(Color.FromArgb(143,168,152),1.5f))g.DrawEllipse(p,dot);
    if(done)using(var p=new Pen(Color.White,2.2f)){g.DrawLines(p,new[]{new Point(x+6,mid),new Point(x+10,mid+4),new Point(x+16,mid-4)});}
    else TextRenderer.DrawText(g,step.ToString(),new Font("Segoe UI",8.5f,FontStyle.Bold),dot,now?Color.White:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    TextRenderer.DrawText(g,text,font,new Point(x+28,mid-size.Height/2),done?Theme.BrandDark:now?Theme.Ink:Theme.Muted);
    x+=28+size.Width+16;
    if(i<2){using(var p=new Pen(step<current?Color.FromArgb(156,195,172):Color.FromArgb(201,217,207),2))g.DrawLine(p,x,mid,x+34,mid);x+=46;}
   }
   if(tip.Length>0){var size=TextRenderer.MeasureText(tip,Font);if(x+size.Width+24<Width)TextRenderer.DrawText(g,tip,Font,new Point(Width-size.Width-16,mid-size.Height/2),Theme.Muted);}
  }
  public static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }

 // Panel de bienvenida sobre el lienzo vacío.
 public class WelcomePanel:Panel {
  public event Action ChooseSize,LoadExample,OpenDesign,UseDymo;
  public WelcomePanel(){
   Size=new Size(860,300);BackColor=Color.White;Padding=new Padding(26);Font=new Font("Segoe UI",9.5f);AccessibleName="Primeros pasos";
   var title=new System.Windows.Forms.Label{Text="Vamos a crear una etiqueta",Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=Theme.Ink,AutoSize=true,Location=new Point(26,22)};
   var intro=new System.Windows.Forms.Label{Text="Elige cómo empezar. Puedes cambiar el tamaño y el contenido en cualquier momento.",ForeColor=Theme.Muted,AutoSize=true,Location=new Point(28,62)};
   Controls.Add(title);Controls.Add(intro);
   var options=new[]{
    new{Icon="size",Title="Elegir tamaño",Hint="Etiqueta de rollo, cinta DYMO o papel A4 / A3.",Go=(Action)(()=>{if(ChooseSize!=null)ChooseSize();})},
    new{Icon="example",Title="Partir de un ejemplo",Hint="Un diseño ya hecho que puedes modificar.",Go=(Action)(()=>{if(LoadExample!=null)LoadExample();})},
    new{Icon="open",Title="Abrir un diseño",Hint="Un archivo .etiqueta que guardaste antes.",Go=(Action)(()=>{if(OpenDesign!=null)OpenDesign();})},
    new{Icon="bluetooth",Title="Imprimir en una DYMO",Hint="Conecta tu LetraTag por Bluetooth.",Go=(Action)(()=>{if(UseDymo!=null)UseDymo();})}
   };
   int x=26;foreach(var o in options){var go=o.Go;var b=new Button{Location=new Point(x,100),Size=new Size(194,150),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Theme.Ink,Cursor=Cursors.Hand,Text="",AccessibleName=o.Title+". "+o.Hint,AccessibleRole=AccessibleRole.PushButton,TabStop=true};b.FlatAppearance.BorderColor=Color.FromArgb(201,217,207);b.FlatAppearance.MouseOverBackColor=Theme.Surface;
    string icon=o.Icon,head=o.Title,hint=o.Hint;b.Paint+=(s,e)=>{var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var bg=new SolidBrush(Theme.Soft))using(var path=StepsBar.Round(new Rectangle(16,16,42,42),10))g.FillPath(bg,path);using(var im=Ui.Icon(icon,Theme.Brand))g.DrawImage(im,27,27);
     using(var f=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(g,head,f,new Rectangle(16,68,b.Width-28,24),Theme.Ink,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
     TextRenderer.DrawText(g,hint,b.Font,new Rectangle(16,94,b.Width-28,Math.Max(20,b.Height-98)),Theme.Muted,TextFormatFlags.Left|TextFormatFlags.WordBreak);if(b.Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,b.Width-8,b.Height-8));};
    b.Click+=(s,e)=>go();Controls.Add(b);cards.Add(b);x+=204;}
   note=new System.Windows.Forms.Label{Text="Este panel desaparece al añadir el primer elemento.",ForeColor=Theme.Muted,AutoSize=true,Location=new Point(28,264)};Controls.Add(note);
   Arrange(900);
  }
  readonly List<Button> cards=new List<Button>();System.Windows.Forms.Label note;
  public bool Wanted;
  // Cuatro tarjetas en fila si caben; si no, en dos filas de dos.
  public void Arrange(int available){int cols=available>=880?4:2;int w=cols==4?194:Math.Max(180,Math.Min(250,(available-90)/2)),h=150;
   for(int i=0;i<cards.Count;i++){cards[i].Size=new Size(w,h);cards[i].Location=new Point(26+(i%cols)*(w+10),100+(i/cols)*(h+10));}
   int rows=(cards.Count+cols-1)/cols;int bottom=100+rows*(h+10);note.Location=new Point(28,bottom+6);Size=new Size(52+cols*w+(cols-1)*10,bottom+44);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Color.FromArgb(201,217,207)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
 }

 // DYMO guardada en este equipo (se guarda aparte de Mis impresoras para no afectar a versiones anteriores).
 [DataContract] public class SavedDymo {[DataMember] public string Name="";[DataMember] public ulong Address;}
 public static class DymoStore {
  public static string PathName {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtiquetaLocal","dymo.json");}}
  public static LTDevice Load(){return Load(PathName);}
  public static LTDevice Load(string path){try{if(!File.Exists(path))return null;using(var f=File.OpenRead(path)){var d=(SavedDymo)new DataContractJsonSerializer(typeof(SavedDymo)).ReadObject(f);if(d==null||d.Address==0)return null;return new LTDevice{Address=d.Address,Name=String.IsNullOrWhiteSpace(d.Name)?"LetraTag":d.Name};}}catch{return null;}}
  public static void Save(LTDevice device){Save(PathName,device);}
  public static void Save(string path,LTDevice device){if(device==null||device.Address==0)return;Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var f=File.Create(temp))new DataContractJsonSerializer(typeof(SavedDymo)).WriteObject(f,new SavedDymo{Name=device.Name,Address=device.Address});if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static void SelfTest(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"dymo-test-"+Guid.NewGuid().ToString("N")+".json");try{if(Load(path)!=null)throw new Exception("DYMO inexistente");Save(path,new LTDevice{Address=0xA1B2C3D4E5F6,Name="Letratag 200B"});var d=Load(path);if(d==null||d.Address!=0xA1B2C3D4E5F6||d.Name!="Letratag 200B")throw new Exception("No se recupera la DYMO guardada");File.WriteAllText(path,"roto");if(Load(path)!=null)throw new Exception("DYMO dañada");}finally{if(File.Exists(path))File.Delete(path);}}
 }

 // Búsqueda de LetraTag por Bluetooth LE.
 public static class LetraTagScanner {
  public static async Task Scan(Control owner,Action<LTDevice> found,int milliseconds,CancellationToken token){
   var seen=new HashSet<ulong>();var watcher=new BluetoothLEAdvertisementWatcher{ScanningMode=BluetoothLEScanningMode.Active};
   watcher.Received+=(s,e)=>{string name=e.Advertisement.LocalName??"";bool match=name.StartsWith("Letratag",StringComparison.OrdinalIgnoreCase)||name.StartsWith("DYMO LT-200B",StringComparison.OrdinalIgnoreCase)||e.Advertisement.ServiceUuids.Any(u=>u.ToString().StartsWith("be3dd650",StringComparison.OrdinalIgnoreCase));if(!match)return;ulong address=e.BluetoothAddress;string shown=String.IsNullOrEmpty(name)?"LetraTag BLE":name;try{owner.BeginInvoke(new Action(()=>{if(owner.IsDisposed||!seen.Add(address))return;found(new LTDevice{Address=address,Name=shown});}));}catch(InvalidOperationException){}};
   watcher.Start();try{await Task.Delay(milliseconds,token);}finally{watcher.Stop();}
  }
 }

 // Conectar una DYMO LetraTag en tres pasos.
 public class DymoConnectForm:Form {
  readonly ListBox devices=new ListBox();readonly Button search=new Button(),stop=new Button(),ok=new Button(),cancel=new Button();readonly System.Windows.Forms.Label state=new System.Windows.Forms.Label();readonly NumericUpDown length=new NumericUpDown();CancellationTokenSource cts;
  public LTDevice Device {get{return devices.SelectedItem as LTDevice;}}
  public decimal LabelLength {get{return length.Value;}}
  static System.Windows.Forms.Label Number(int n,int x,int y){var l=new System.Windows.Forms.Label{Text=n.ToString(),Location=new Point(x,y),Size=new Size(26,26),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.White,BackColor=Theme.Brand,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)};return l;}
  static System.Windows.Forms.Label Caption(string t,int x,int y,int w,bool bold=false,bool muted=false){return new System.Windows.Forms.Label{Text=t,Location=new Point(x,y),Width=w,AutoSize=false,Height=bold?24:40,Font=bold?new Font("Segoe UI",10.5f,FontStyle.Bold):new Font("Segoe UI",9.5f),ForeColor=muted?Theme.Muted:Theme.Ink};}
  public DymoConnectForm(decimal initialLength){
   Text="Conectar DYMO LetraTag";Icon=Branding.AppIcon;ClientSize=new Size(820,430);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(247,250,248);
   Controls.Add(Number(1,24,24));Controls.Add(Caption("Enciende la DYMO y acércala al ordenador",60,25,430,true));Controls.Add(Caption("Comprueba que el Bluetooth del ordenador está activado.",60,50,430,false,true));
   Controls.Add(Number(2,24,96));Controls.Add(Caption("Elige tu DYMO",60,97,430,true));
   search.Text="Buscar DYMO";search.Location=new Point(60,128);search.Size=new Size(140,32);search.Click+=async(s,e)=>await Search();Controls.Add(search);
   stop.Text="Detener";stop.Location=new Point(208,128);stop.Size=new Size(90,32);stop.Enabled=false;stop.Click+=(s,e)=>{if(cts!=null)cts.Cancel();};Controls.Add(stop);
   state.Location=new Point(308,134);state.Size=new Size(200,22);state.ForeColor=Theme.Muted;Controls.Add(state);
   devices.Location=new Point(60,170);devices.Size=new Size(430,110);devices.Font=new Font("Segoe UI",10.5f);devices.AccessibleName="DYMO encontradas";devices.SelectedIndexChanged+=(s,e)=>ok.Enabled=Device!=null;Controls.Add(devices);
   Controls.Add(Caption("¿No aparece? Apágala y vuelve a encenderla, y pulsa Buscar DYMO otra vez.",60,288,430,false,true));
   var box=new Panel{Location=new Point(520,24),Size=new Size(276,256),BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};Controls.Add(box);
   box.Controls.Add(Number(3,16,16));box.Controls.Add(Caption("Tamaño de la etiqueta",52,17,210,true));
   var lengthCaption=Caption("Largo (mm)",16,62,120);lengthCaption.Height=22;box.Controls.Add(lengthCaption);length.Minimum=25;length.Maximum=200;length.DecimalPlaces=0;length.Value=Math.Max(25,Math.Min(200,initialLength));length.Location=new Point(16,86);length.Width=110;length.AccessibleName="Largo de la etiqueta en milímetros";box.Controls.Add(length);
   var tallCaption=Caption("Alto",140,62,110);tallCaption.Height=22;box.Controls.Add(tallCaption);var tall=new TextBox{Text="12 mm (cinta)",ReadOnly=true,Location=new Point(140,86),Width=118,AccessibleName="Alto de la cinta: 12 milímetros"};box.Controls.Add(tall);
   var hint=Caption("El largo puede ir de 25 a 200 mm. El alto lo marca la cinta DYMO.",16,124,244,false,true);hint.Height=60;box.Controls.Add(hint);
   cancel.Text="Cancelar";cancel.Size=new Size(110,36);cancel.Location=new Point(546,374);cancel.DialogResult=DialogResult.Cancel;Controls.Add(cancel);
   ok.Text="Usar esta DYMO";ok.Size=new Size(140,36);ok.Location=new Point(664,374);ok.Enabled=false;ok.BackColor=Theme.Brand;ok.ForeColor=Color.White;ok.FlatStyle=FlatStyle.Flat;ok.FlatAppearance.BorderSize=0;ok.Font=new Font("Segoe UI",9.5f,FontStyle.Bold);ok.Click+=(s,e)=>{if(Device==null)return;if(cts!=null)cts.Cancel();DialogResult=DialogResult.OK;};Controls.Add(ok);
   ok.EnabledChanged+=(s,e)=>ok.BackColor=ok.Enabled?Theme.Brand:Color.FromArgb(160,180,168);ok.BackColor=Color.FromArgb(160,180,168);
   AcceptButton=ok;CancelButton=cancel;
   var saved=DymoStore.Load();if(saved!=null){devices.Items.Add(saved);devices.SelectedIndex=0;state.Text="Guardada en este equipo";}
   Shown+=async(s,e)=>{if(saved==null)await Search();};FormClosing+=(s,e)=>{if(cts!=null)cts.Cancel();};
  }
  async Task Search(){if(cts!=null)return;cts=new CancellationTokenSource();var token=cts.Token;search.Enabled=false;stop.Enabled=true;int seconds=12;var timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>{seconds=Math.Max(0,seconds-1);state.Text="Buscando… quedan "+seconds+" s";};state.Text="Buscando… quedan 12 s";timer.Start();
   try{await LetraTagScanner.Scan(this,d=>{if(devices.Items.Cast<object>().OfType<LTDevice>().Any(x=>x.Address==d.Address))return;devices.Items.Add(d);if(devices.SelectedIndex<0)devices.SelectedIndex=devices.Items.Count-1;},12000,token);if(!IsDisposed)state.Text=devices.Items.Count==0?"No se ha encontrado ninguna DYMO.":"Búsqueda terminada.";}
   catch(OperationCanceledException){if(!IsDisposed)state.Text="Búsqueda detenida.";}
   catch(Exception ex){if(!IsDisposed)state.Text="No se pudo buscar.";if(!IsDisposed)MessageBox.Show(this,"No se pudo usar el Bluetooth: "+ex.Message,"Conectar DYMO");}
   finally{timer.Stop();timer.Dispose();cts.Dispose();cts=null;if(!IsDisposed){search.Enabled=true;stop.Enabled=false;}}
  }
 }

 // Lista de impresoras guardadas en forma de tarjetas.
 public class PrinterCardList:ListBox {
  public string DefaultId="",InUseId="";
  public PrinterCardList(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=68;IntegralHeight=false;BorderStyle=BorderStyle.None;BackColor=Color.FromArgb(247,250,248);Font=new Font("Segoe UI",9.5f);}
  public static string Status(SavedPrinter p){return p.Connection=="USB"?"Impresión USB pendiente":p.Configured?"Configurada":"Se ajustará en la primera impresión";}
  public static string Describe(SavedPrinter p){return (String.IsNullOrWhiteSpace(p.Location)?"":p.Location+" · ")+(p.Connection=="USB"?"por USB":"por red")+" · "+p.Address;}
  protected override void OnDrawItem(DrawItemEventArgs e){
   if(e.Index<0||e.Index>=Items.Count)return;var p=Items[e.Index] as SavedPrinter;if(p==null)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var bg=new SolidBrush(BackColor))g.FillRectangle(bg,e.Bounds);
   bool selected=(e.State&DrawItemState.Selected)!=0;var card=new Rectangle(e.Bounds.X+4,e.Bounds.Y+4,e.Bounds.Width-10,e.Bounds.Height-8);
   using(var path=StepsBar.Round(card,8))using(var fill=new SolidBrush(Color.White))using(var border=new Pen(selected?Theme.Brand:Theme.Line,selected?2:1)){g.FillPath(fill,path);g.DrawPath(border,path);}
   var iconBox=new Rectangle(card.X+12,card.Y+(card.Height-38)/2,38,38);using(var b=new SolidBrush(p.Id==InUseId?Theme.Soft:Color.FromArgb(238,243,239)))using(var path=StepsBar.Round(iconBox,8))g.FillPath(b,path);using(var im=Ui.Icon(p.Connection=="USB"?"usb":"print",Theme.Brand))g.DrawImage(im,iconBox.X+9,iconBox.Y+9);
   int x=iconBox.Right+14;using(var bold=new Font("Segoe UI",10.5f,FontStyle.Bold)){var size=TextRenderer.MeasureText(p.Alias,bold);TextRenderer.DrawText(g,p.Alias,bold,new Point(x,card.Y+11),Theme.Ink);x+=size.Width+6;}
   if(p.Id==InUseId)x=Badge(g,"En uso",x,card.Y+12,Theme.Brand,Color.White);
   if(p.Id==DefaultId)x=Badge(g,"★ Predeterminada",x,card.Y+12,Color.FromArgb(255,241,214),Color.FromArgb(107,68,0));
   TextRenderer.DrawText(g,Describe(p),Font,new Rectangle(iconBox.Right+14,card.Y+36,card.Width-iconBox.Width-270,20),Theme.Muted,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
   string status=Status(p);var statusSize=TextRenderer.MeasureText(status,Font);TextRenderer.DrawText(g,status,Font,new Point(card.Right-statusSize.Width-16,card.Y+(card.Height-statusSize.Height)/2),p.Configured&&p.Connection!="USB"?Theme.BrandDark:Theme.Warn);
   if((e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(card,-3,-3));
  }
  int Badge(Graphics g,string text,int x,int y,Color back,Color fore){using(var f=new Font("Segoe UI",8.5f,FontStyle.Bold)){var size=TextRenderer.MeasureText(text,f);var r=new Rectangle(x,y,size.Width+8,20);using(var b=new SolidBrush(back))using(var path=StepsBar.Round(r,10))g.FillPath(b,path);TextRenderer.DrawText(g,text,f,r,fore,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return r.Right+6;}}
 }
}
