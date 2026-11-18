using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtiquetaLocal {
 [DataContract] public class Element:ICustomTypeDescriptor {
  [DataMember,DisplayName("Tipo"),ReadOnly(true),Category("Contenido"),Description("Clase de elemento. No se puede cambiar.")] public string Kind {get;set;}
  [DataMember,DisplayName("Contenido"),Category("Contenido"),Description("Texto que se imprime, o datos del código de barras o QR.")] public string Text {get;set;}
  [DataMember,DisplayName("Posición horizontal (mm)"),Category("Posición y tamaño"),Description("Distancia desde el borde izquierdo de la etiqueta.")] public float X {get;set;}
  [DataMember,DisplayName("Posición vertical (mm)"),Category("Posición y tamaño"),Description("Distancia desde el borde superior de la etiqueta.")] public float Y {get;set;}
  [DataMember,DisplayName("Ancho (mm)"),Category("Posición y tamaño"),Description("Ancho del elemento en milímetros.")] public float W {get;set;}
  [DataMember,DisplayName("Alto (mm)"),Category("Posición y tamaño"),Description("Alto del elemento en milímetros.")] public float H {get;set;}
  [DataMember,DisplayName("Tamaño de letra (pt)"),Category("Estilo"),Description("Tamaño del texto en puntos.")] public float FontSize {get;set;}
  [DataMember,DisplayName("Negrita"),Category("Estilo"),TypeConverter(typeof(SiNoConverter))] public bool Bold {get;set;}
  [DataMember,DisplayName("Cursiva"),Category("Estilo"),TypeConverter(typeof(SiNoConverter))] public bool Italic {get;set;}
  [DataMember,DisplayName("Subrayado"),Category("Estilo"),TypeConverter(typeof(SiNoConverter))] public bool Underline {get;set;}
  [DataMember,DisplayName("Alineación del texto"),Category("Estilo"),TypeConverter(typeof(AlignConverter)),Description("Izquierda, Centro o Derecha dentro de su recuadro.")] public string TextAlign {get;set;}
  [DataMember,DisplayName("Fuente"),Category("Estilo"),TypeConverter(typeof(FontNameConverter)),Description("Tipo de letra instalado en Windows.")] public string FontName {get;set;}
  [DataMember,DisplayName("Mantener proporciones"),Category("Estilo"),TypeConverter(typeof(SiNoConverter)),Description("Si es Sí, la imagen no se deforma al cambiar su tamaño.")] public bool LockAspect {get;set;}
  [DataMember,Browsable(false)] public float ImageAspect {get;set;}
  public Element Copy(){return (Element)MemberwiseClone();}
  [DataMember,DisplayName("Grosor de línea (mm)"),Category("Estilo"),Description("Grosor del trazo del marco o de la línea.")] public float Stroke {get;set;}
  // El panel de propiedades solo muestra los campos útiles para cada tipo de elemento.
  static string[] VisibleFor(string kind){
   if(kind=="Texto")return new[]{"Kind","Text","FontName","FontSize","Bold","Italic","Underline","TextAlign","X","Y","W","H"};
   if(kind=="Imagen")return new[]{"Kind","LockAspect","X","Y","W","H"};
   if(kind=="Código 39"||kind=="QR")return new[]{"Kind","Text","X","Y","W","H"};
   if(kind=="Marco"||kind=="Línea")return new[]{"Kind","Stroke","X","Y","W","H"};
   return null;
  }
  AttributeCollection ICustomTypeDescriptor.GetAttributes(){return TypeDescriptor.GetAttributes(this,true);}
  string ICustomTypeDescriptor.GetClassName(){return TypeDescriptor.GetClassName(this,true);}
  string ICustomTypeDescriptor.GetComponentName(){return TypeDescriptor.GetComponentName(this,true);}
  TypeConverter ICustomTypeDescriptor.GetConverter(){return TypeDescriptor.GetConverter(this,true);}
  EventDescriptor ICustomTypeDescriptor.GetDefaultEvent(){return TypeDescriptor.GetDefaultEvent(this,true);}
  PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty(){return TypeDescriptor.GetDefaultProperty(this,true);}
  object ICustomTypeDescriptor.GetEditor(Type editorBaseType){return TypeDescriptor.GetEditor(this,editorBaseType,true);}
  EventDescriptorCollection ICustomTypeDescriptor.GetEvents(){return TypeDescriptor.GetEvents(this,true);}
  EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes){return TypeDescriptor.GetEvents(this,attributes,true);}
  PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(){return ((ICustomTypeDescriptor)this).GetProperties(null);}
  PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes){var all=attributes==null?TypeDescriptor.GetProperties(this,true):TypeDescriptor.GetProperties(this,attributes,true);var names=VisibleFor(Kind);if(names==null)return all;return new PropertyDescriptorCollection(all.Cast<PropertyDescriptor>().Where(p=>names.Contains(p.Name)).ToArray());}
  object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd){return this;}
  [DataMember,Browsable(false)] public string ImageData {get;set;}
  public Element(){Kind="Texto";Text="Nuevo texto";W=35;H=10;FontSize=12;Stroke=.3f;FontName="Arial";LockAspect=true;}
  public override string ToString(){return Kind+" · "+(Text??"");}
 }
 [DataContract] public class Label {
  [DataMember] public int Version=1;
  [DataMember] public string Media="";
  [DataMember] public float Width=100;
  [DataMember] public float Height=50;
  [DataMember] public List<Element> Elements=new List<Element>();
 }
 public static class Model {
  public static string Save(Label l){using(var ms=new MemoryStream()){new DataContractJsonSerializer(typeof(Label)).WriteObject(ms,l);return Encoding.UTF8.GetString(ms.ToArray());}}
  public static Label Load(string s){
   Label l;using(var ms=new MemoryStream(Encoding.UTF8.GetBytes(s)))l=(Label)new DataContractJsonSerializer(typeof(Label)).ReadObject(ms);
   Validate(l);foreach(var e in l.Elements){if(String.IsNullOrEmpty(e.FontName))e.FontName="Arial";if(e.Kind=="Imagen"&&e.ImageAspect<=0){Editing.RestoreImageRatio(e);e.LockAspect=true;}}return l;
  }
  public static void Validate(Label l){
   if(l==null||l.Version!=1||!Valid(l.Width)||!Valid(l.Height)||l.Width<5||l.Height<5||l.Width>1000||l.Height>1000||l.Elements==null||l.Elements.Count>500)throw new Exception("Plantilla no válida: medidas de 5 a 1000 mm y máximo 500 elementos.");
   foreach(var e in l.Elements){
    if(e==null||!new[]{"Texto","Marco","Línea","Imagen","Código 39","QR"}.Contains(e.Kind)||!Valid(e.X)||!Valid(e.Y)||!Valid(e.W)||!Valid(e.H)||e.X<0||e.Y<0||e.W<=0||e.H<=0||e.W>1000||e.H>1000||!Valid(e.FontSize)||e.FontSize<1||e.FontSize>300||!Valid(e.Stroke)||e.Stroke<=0||e.Stroke>10)throw new Exception("Hay un elemento con propiedades no válidas.");
    if(e.Kind=="Texto"&&!String.IsNullOrEmpty(e.TextAlign)&&!new[]{"Izquierda","Centro","Derecha"}.Contains(e.TextAlign))throw new Exception("Alineación no válida.");
    if(e.Kind=="QR")QrSupport.Matrix(e.Text);
    if(e.Kind=="Código 39")Barcode.Check(e.Text);
    if(e.Kind=="Imagen"){if(String.IsNullOrEmpty(e.ImageData)||e.ImageData.Length>14000000)throw new Exception("Imagen ausente o demasiado grande.");using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var im=Image.FromStream(ms)){if(im.Width>12000||im.Height>12000)throw new Exception("Imagen demasiado grande.");}}
   }
  }
  public static bool Valid(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
  public static bool Outside(Label l){return Editing.MarginProblem(l)!=null;}
 }
 public static class Barcode {
  const string Chars="0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
  static readonly int[] Patterns={0x034,0x121,0x061,0x160,0x031,0x130,0x070,0x025,0x124,0x064,0x109,0x049,0x148,0x019,0x118,0x058,0x00d,0x10c,0x04c,0x01c,0x103,0x043,0x142,0x013,0x112,0x052,0x007,0x106,0x046,0x016,0x181,0x0c1,0x1c0,0x091,0x190,0x0d0,0x085,0x184,0x0c4,0x0a8,0x0a2,0x08a,0x02a,0x094};
  public static void Check(string text){if(String.IsNullOrEmpty(text)||text.Length>80||text.Any(c=>c=='*'||Chars.IndexOf(c)<0))throw new Exception("Código 39: utiliza de 1 a 80 caracteres: A-Z, 0-9, espacio y - . $ / + %. Sin asteriscos.");}
  public static float Module(Element e){return e.W/(20+16*(e.Text.Length+2)-1);}
  public static void Draw(Graphics g,Element e){
   Check(e.Text);string value="*"+e.Text+"*";float unit=Module(e);float x=e.X+10*unit;
   foreach(char c in value){int pat=Patterns[Chars.IndexOf(c)];for(int j=0;j<9;j++){float w=((pat&(1<<(8-j)))!=0?3:1)*unit;if(j%2==0)g.FillRectangle(Brushes.Black,x,e.Y,w,e.H);x+=w;}x+=unit;}
  }
 }
 public static class Render {
  public static void Draw(Graphics g,Label label){
   foreach(var e in label.Elements){
    var state=g.Save();g.SetClip(new RectangleF(e.X,e.Y,e.W,e.H));
    try{
     if(e.Kind=="Texto")using(var f=Editing.FontFor(e))using(var sf=new StringFormat()){sf.Trimming=StringTrimming.None;sf.Alignment=e.TextAlign=="Centro"?StringAlignment.Center:e.TextAlign=="Derecha"?StringAlignment.Far:StringAlignment.Near;g.DrawString(e.Text??"",f,Brushes.Black,new RectangleF(e.X,e.Y,e.W,e.H),sf);}
     else if(e.Kind=="Marco")using(var pen=new Pen(Color.Black,e.Stroke))g.DrawRectangle(pen,e.X+e.Stroke/2,e.Y+e.Stroke/2,Math.Max(0,e.W-e.Stroke),Math.Max(0,e.H-e.Stroke));
     else if(e.Kind=="Línea")using(var pen=new Pen(Color.Black,e.Stroke))g.DrawLine(pen,e.X,e.Y+e.H/2,e.X+e.W,e.Y+e.H/2);
     else if(e.Kind=="QR"){g.SmoothingMode=SmoothingMode.None;QrSupport.Draw(g,e);}
     else if(e.Kind=="Código 39"){g.SmoothingMode=SmoothingMode.None;Barcode.Draw(g,e);}
     else if(e.Kind=="Imagen"&&!String.IsNullOrEmpty(e.ImageData))using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var im=Image.FromStream(ms))g.DrawImage(im,new RectangleF(e.X,e.Y,e.W,e.H));
    } finally {g.Restore(state);}
   }
  }
 }
 public class Canvas:Control {
  public Label Label; public Element Selected;public event Action Changed; public event Action SelectionChanged; public event Action BeforeChange;
  float scale=4,ox=24,oy=24;Point start;float sx,sy,sw,sh,startFont;bool drag,resize;public bool Grid=true,Guides=true;float? guideX,guideY;
  public Canvas(){DoubleBuffered=true;BackColor=Color.FromArgb(233,239,235);Dock=DockStyle.Fill;SetStyle(ControlStyles.ResizeRedraw,true);}
  void LayoutLabel(){if(Label==null)return;scale=Math.Max(.1f,Math.Min((ClientSize.Width-60)/Label.Width,(ClientSize.Height-60)/Label.Height));ox=Math.Max(20,(ClientSize.Width-Label.Width*scale)/2);oy=Math.Max(20,(ClientSize.Height-Label.Height*scale)/2);}
  protected override void OnPaint(PaintEventArgs a){base.OnPaint(a);if(Label==null)return;LayoutLabel();var g=a.Graphics;float w=Label.Width*scale,h=Label.Height*scale;
   g.FillRectangle(new SolidBrush(Color.FromArgb(200,214,205)),ox+5,oy+5,w,h);g.FillRectangle(Brushes.White,ox,oy,w,h);
   if(Label.Media=="DYMO LT12"){g.FillRectangle(Brushes.LightGray,ox,oy,w,2.2f*scale);g.FillRectangle(Brushes.LightGray,ox,oy+h-2.2f*scale,w,2.2f*scale);}
   var st=g.Save();g.TranslateTransform(ox,oy);g.ScaleTransform(scale,scale);g.SetClip(new RectangleF(0,0,Label.Width,Label.Height));
   if(Grid&&scale>=1)using(var pen=new Pen(Color.FromArgb(231,238,233),1/scale)){for(float x=5;x<Label.Width;x+=5)g.DrawLine(pen,x,0,x,Label.Height);for(float y=5;y<Label.Height;y+=5)g.DrawLine(pen,0,y,Label.Width,y);}
   try{Render.Draw(g,Label);}catch(Exception ex){g.Restore(st);g.DrawString(ex.Message,Font,Brushes.Red,20,8);return;}
   g.Restore(st);g.DrawRectangle(Pens.Gray,ox,oy,w,h);
   if(drag&&Guides)using(var pen=new Pen(Color.FromArgb(174,65,134),1)){pen.DashStyle=DashStyle.Dash;if(guideX.HasValue)g.DrawLine(pen,ox+guideX.Value*scale,oy-8,ox+guideX.Value*scale,oy+h+8);if(guideY.HasValue)g.DrawLine(pen,ox-8,oy+guideY.Value*scale,ox+w+8,oy+guideY.Value*scale);}
   if(Selected!=null){var e=Selected;using(var p=new Pen(Color.FromArgb(39,126,82),2)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,ox+e.X*scale,oy+e.Y*scale,e.W*scale,e.H*scale);}g.FillRectangle(Brushes.SeaGreen,ox+(e.X+e.W)*scale-4,oy+(e.Y+e.H)*scale-4,8,8);}
   g.DrawString(Label.Width+" × "+Label.Height+" mm  ·  Cuadrícula: 5 mm",Font,Brushes.DimGray,ox,oy+h+8);
  }
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(Label==null||e.Button!=MouseButtons.Left)return;LayoutLabel();
   guideX=guideY=null;resize=Selected!=null&&Math.Abs(e.X-(ox+(Selected.X+Selected.W)*scale))<9&&Math.Abs(e.Y-(oy+(Selected.Y+Selected.H)*scale))<9;
   if(!resize)Selected=Label.Elements.LastOrDefault(v=>new RectangleF(ox+v.X*scale,oy+v.Y*scale,v.W*scale,v.H*scale).Contains(e.Location));
   if(SelectionChanged!=null)SelectionChanged();if(Selected!=null){if(BeforeChange!=null)BeforeChange();start=e.Location;sx=Selected.X;sy=Selected.Y;sw=Selected.W;sh=Selected.H;startFont=Selected.FontSize;drag=true;Capture=true;}Invalidate();
  }
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!drag||Selected==null)return;float dx=(e.X-start.X)/scale,dy=(e.Y-start.Y)/scale;
   if(resize){if(Selected.Kind=="Texto"||Selected.Kind=="QR"||(Selected.Kind=="Imagen"&&Selected.LockAspect)){float factor=Math.Abs(dx/Math.Max(.1f,sw))>=Math.Abs(dy/Math.Max(.1f,sh))?(sw+dx)/sw:(sh+dy)/sh;factor=Math.Max(.5f/Math.Min(sw,sh),Math.Min(factor,1000f/Math.Max(sw,sh)));if(Selected.Kind=="Texto"){Selected.FontSize=Math.Max(1,Math.Min(300,startFont*factor));factor=Selected.FontSize/startFont;}Selected.W=sw*factor;Selected.H=sh*factor;}else{Selected.W=(float)Math.Round(Math.Max(.5,Math.Min(1000,sw+dx)),1);Selected.H=(float)Math.Round(Math.Max(.5,Math.Min(1000,sh+dy)),1);}}else{Selected.X=(float)Math.Round(Math.Max(0,sx+dx),1);Selected.Y=(float)Math.Round(Math.Max(0,sy+dy),1);guideX=guideY=null;if(Guides&&(ModifierKeys&Keys.Alt)==0)SmartGuides.Snap(Selected,Label,5/scale,out guideX,out guideY);}Invalidate();
  }
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(drag){drag=false;guideX=guideY=null;Capture=false;Invalidate();if(Changed!=null)Changed();}}
 }
 public class MainForm:Form {
  PrinterChip printerChip=new PrinterChip();StepsBar steps=new StepsBar();WelcomePanel welcome=new WelcomePanel();ToolTip chipTip=new ToolTip();
  List<Button> selectionButtons=new List<Button>();Button editButton;Control imageGroup;CheckBox guidesToggle;
  LetraTagPanel dymoPanel;Control canvasHost;Label label=new Label();Canvas canvas=new Canvas();PropertyGrid props=new PropertyGrid();ListBox items=new ListBox();ToolStripStatusLabel status=new ToolStripStatusLabel();
  ComboBox fontSelector=new ComboBox();NumericUpDown fontPoints=new NumericUpDown();CheckBox boldToggle=new CheckBox(),italicToggle=new CheckBox(),underlineToggle=new CheckBox(),aspectToggle=new CheckBox();ComboBox alignment=new ComboBox();bool formatSync;ComboBox presets=new ComboBox();NumericUpDown width=new NumericUpDown(),height=new NumericUpDown();bool syncing,dirty;string file="";Stack<string> undo=new Stack<string>();
  public MainForm(){
   Text="Impresorapp "+AppVersion.Text+" · Sin título";Icon=Branding.AppIcon;Size=new Size(1250,820);MinimumSize=new Size(950,650);Font=new Font("Segoe UI",9);StartPosition=FormStartPosition.CenterScreen;
   // Menús: Archivo · Editar · Impresoras · Ayuda.
   var menu=new MenuStrip{Font=new Font("Segoe UI",9.5f)};
   var f=new ToolStripMenuItem("&Archivo");f.DropDownItems.Add(MenuItem("&Nueva","Ctrl+N",()=>New()));f.DropDownItems.Add(MenuItem("&Abrir…","Ctrl+O",()=>Open()));f.DropDownItems.Add(MenuItem("&Guardar","Ctrl+S",()=>Save(false)));f.DropDownItems.Add(MenuItem("Guardar &como…",null,()=>Save(true)));f.DropDownItems.Add(new ToolStripSeparator());
   f.DropDownItems.Add(MenuItem("&Imprimir…","Ctrl+P",()=>Print()));f.DropDownItems.Add(MenuItem("Imprimir en &papel (A4 / A3)…",null,()=>PrintPaper()));f.DropDownItems.Add(new ToolStripSeparator());f.DropDownItems.Add(MenuItem("&Salir",null,()=>Close()));menu.Items.Add(f);
   var ed=new ToolStripMenuItem("&Editar");var undoItem=MenuItem("&Deshacer","Ctrl+Z",()=>Undo());var editItem=MenuItem("&Editar contenido…","Doble clic",()=>EditText());var dupItem=MenuItem("D&uplicar",null,()=>Duplicate());var delItem=MenuItem("E&liminar","Supr",()=>Delete());var centerHItem=MenuItem("Centrar &horizontalmente",null,()=>CenterElement(true));var centerVItem=MenuItem("Centrar &verticalmente",null,()=>CenterElement(false));var guidesItem=new ToolStripMenuItem("&Guías al mover"){CheckOnClick=true,Checked=true};var exampleItem=MenuItem("Cargar un e&jemplo",null,()=>LoadExample());
   ed.DropDownItems.AddRange(new ToolStripItem[]{undoItem,new ToolStripSeparator(),editItem,dupItem,delItem,new ToolStripSeparator(),centerHItem,centerVItem,guidesItem,new ToolStripSeparator(),exampleItem});
   ed.DropDownOpening+=(s,e)=>{var sel=canvas.Selected;undoItem.Enabled=undo.Count>0;editItem.Enabled=sel!=null&&(sel.Kind=="Texto"||sel.Kind=="Código 39"||sel.Kind=="QR");dupItem.Enabled=delItem.Enabled=centerHItem.Enabled=centerVItem.Enabled=sel!=null;guidesItem.Checked=canvas.Guides;};
   guidesItem.CheckedChanged+=(s,e)=>{canvas.Guides=guidesItem.Checked;if(guidesToggle!=null&&guidesToggle.Checked!=guidesItem.Checked)guidesToggle.Checked=guidesItem.Checked;};menu.Items.Add(ed);
   var pr=new ToolStripMenuItem("&Impresoras");pr.DropDownItems.Add(MenuItem("&Mis impresoras y búsqueda…",null,()=>OpenPrinters()));pr.DropDownItems.Add(MenuItem("Conectar &DYMO por Bluetooth…",null,()=>ConnectDymo()));pr.DropDownItems.Add(MenuItem("Impresora para &papel…",null,()=>{PaperPrinting.Select(this,label);UpdatePrinterBanner();}));menu.Items.Add(pr);
   var help=new ToolStripMenuItem("A&yuda");help.DropDownItems.Add(MenuItem("&Guía rápida","F1",()=>Help()));help.DropDownItems.Add(MenuItem("Buscar &actualizaciones…",null,async()=>{if(Updates.Configure(this))await CheckUpdates(false);}));help.DropDownItems.Add(new ToolStripSeparator());help.DropDownItems.Add(MenuItem("A&cerca de Impresorapp",null,()=>About()));menu.Items.Add(help);
   MainMenuStrip=menu;
   BackColor=Color.FromArgb(242,247,244);MinimumSize=new Size(1150,740);
   // Barra superior: archivo a la izquierda; impresora activa e Imprimir a la derecha.
   var actions=new Panel{Dock=DockStyle.Top,Height=66,Padding=new Padding(12,9,14,9),BackColor=Color.White};
   var fileActions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,BackColor=Color.White,Padding=new Padding(0,7,0,0)};
   fileActions.Controls.Add(Ui.Action("file","Nueva",New,90));fileActions.Controls.Add(Ui.Action("open","Abrir",Open,90));fileActions.Controls.Add(Ui.Action("save","Guardar",()=>Save(false),100));
   fileActions.Controls.Add(new Panel{Width=1,Height=30,BackColor=Theme.Line,Margin=new Padding(6,1,10,1)});fileActions.Controls.Add(Ui.Action("undo","Deshacer",Undo,105));
   var printActions=new FlowLayoutPanel{Dock=DockStyle.Right,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,BackColor=Color.White};
   printerChip.Click+=(s,e)=>ChoosePrinter();printActions.Controls.Add(printerChip);
   var printButton=Ui.Action("print","Imprimir",Print,150,true);printButton.Height=48;printButton.Margin=Padding.Empty;printButton.Font=new Font("Segoe UI",11,FontStyle.Bold);printButton.AccessibleName="Imprimir la etiqueta";printActions.Controls.Add(printButton);
   actions.Controls.Add(fileActions);actions.Controls.Add(printActions);
   steps.Tip="Consejo: haz doble clic en un texto para cambiarlo.";
   var tools=new FlowLayoutPanel{Dock=DockStyle.Left,Width=258,Padding=new Padding(10),BackColor=Color.FromArgb(242,247,244),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
   var sizeTools=Ui.Section(tools,"1. Tamaño de la etiqueta");
   sizeTools.Controls.Add(Ui.Hint("Incluye rollos de etiquetas, cinta DYMO y papel A4 / A3."));
   presets.DropDownStyle=ComboBoxStyle.DropDownList;presets.Width=218;presets.DropDownWidth=380;presets.Items.AddRange(Formats.All.Cast<object>().ToArray());presets.SelectedIndex=0;sizeTools.Controls.Add(presets);
   foreach(var n in new[]{width,height}){n.Minimum=5;n.Maximum=1000;n.DecimalPlaces=1;n.Width=95;}width.Value=100;height.Value=50;
   var measures=new FlowLayoutPanel{Width=222,Height=52,Margin=new Padding(0,8,0,4)};
   foreach(var pair in new[]{new{Title="Ancho (mm)",Input=width},new{Title="Alto (mm)",Input=height}}){var col=new FlowLayoutPanel{Width=104,Height=60,FlowDirection=FlowDirection.TopDown,Margin=new Padding(0,0,5,0)};col.Controls.Add(new System.Windows.Forms.Label{Text=pair.Title,AutoSize=true});col.Controls.Add(pair.Input);measures.Controls.Add(col);}sizeTools.Controls.Add(measures);
   sizeTools.Controls.Add(Ui.Action("size","Aplicar medida personalizada",ApplySize));sizeTools.Controls.Add(Ui.Hint("¿No sabes por dónde empezar?"));sizeTools.Controls.Add(Ui.Action("example","Cargar un ejemplo",LoadExample));
   var insertTools=Ui.Section(tools,"2. Añadir contenido");
   insertTools.Controls.Add(Ui.Action("text","Texto",()=>{Add("Texto");}));insertTools.Controls.Add(Ui.Action("image","Imagen o logotipo",()=>Add("Imagen")));
   insertTools.Controls.Add(Ui.Action("barcode","Código de barras",()=>{Add("Código 39");EditText();}));insertTools.Controls.Add(Ui.Action("emoji","Emojis y símbolos",AddEmoji));insertTools.Controls.Add(Ui.Action("qr","Código QR",()=>Add("QR")));
   insertTools.Controls.Add(Ui.Action("frame","Marco",()=>Add("Marco")));insertTools.Controls.Add(Ui.Action("line","Línea",()=>Add("Línea")));
   var editTools=Ui.Section(tools,"3. Editar selección");
   editTools.Controls.Add(Ui.Action("edit","Editar texto o código",EditText));editTools.Controls.Add(Ui.Action("copy","Duplicar elemento",Duplicate));editTools.Controls.Add(Ui.Action("delete","Eliminar elemento",Delete));
   presets.SelectedIndexChanged+=(s,e)=>{
    if(syncing)return;var chosen=presets.SelectedItem as LabelFormat;if(chosen==null)return;if(chosen.W==0){if(chosen.Dymo){height.Value=12;width.Value=Math.Min(200,Math.Max(25,width.Value));ApplySize();}return;}
    width.Value=(decimal)chosen.W;height.Value=(decimal)chosen.H;ApplySize();
   };

   var formatting=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(0,42),Height=42,Padding=new Padding(8,3,8,3),BackColor=Color.FromArgb(234,244,237)};
   formatting.Controls.Add(new System.Windows.Forms.Label{Text="Fuente",AutoSize=true,Margin=new Padding(3,7,3,3)});fontSelector.DropDownStyle=ComboBoxStyle.DropDownList;fontSelector.Width=155;
   string[] basics={"Arial","Segoe UI","Calibri","Verdana","Tahoma","Times New Roman","Courier New","Segoe UI Symbol","Segoe UI Emoji"};var installed=new HashSet<string>(System.Drawing.FontFamily.Families.Select(family=>family.Name),StringComparer.OrdinalIgnoreCase);fontSelector.Items.AddRange(basics.Where(installed.Contains).Cast<object>().ToArray());formatting.Controls.Add(fontSelector);
   formatting.Controls.Add(new System.Windows.Forms.Label{Text="Tamaño",AutoSize=true,Margin=new Padding(3,7,3,3)});fontPoints.Minimum=1;fontPoints.Maximum=300;fontPoints.DecimalPlaces=1;fontPoints.Increment=1;fontPoints.Width=65;fontPoints.Value=12;formatting.Controls.Add(fontPoints);formatting.Controls.Add(new System.Windows.Forms.Label{Text="pt",AutoSize=true,Margin=new Padding(3,7,3,3)});
   AddButton(formatting,"A−",()=>{if(fontPoints.Enabled)fontPoints.Value=Math.Max(1,fontPoints.Value-1);});AddButton(formatting,"A+",()=>{if(fontPoints.Enabled)fontPoints.Value=Math.Min(300,fontPoints.Value+1);});boldToggle.Text="Negrita";boldToggle.AutoSize=true;formatting.Controls.Add(boldToggle);
   italicToggle.Text="Cursiva";italicToggle.AutoSize=true;formatting.Controls.Add(italicToggle);underlineToggle.Text="Subrayado";underlineToggle.AutoSize=true;formatting.Controls.Add(underlineToggle);
   alignment.DropDownStyle=ComboBoxStyle.DropDownList;alignment.Width=105;alignment.Items.AddRange(new object[]{"Izquierda","Centro","Derecha"});alignment.SelectedIndex=0;formatting.Controls.Add(alignment);
   alignment.SelectedIndexChanged+=(s,e)=>{if(formatSync||canvas.Selected==null||canvas.Selected.Kind!="Texto")return;Snapshot();var el=canvas.Selected;el.TextAlign=alignment.Text;el.W=Math.Max(el.W,label.Width-el.X-.5f);Editing.FitText(el,label);RefreshAll();Mark();};
   italicToggle.CheckedChanged+=(s,e)=>ApplyTypography();underlineToggle.CheckedChanged+=(s,e)=>ApplyTypography();
   aspectToggle.Text="Mantener proporciones";aspectToggle.AutoSize=true;formatting.Controls.Add(aspectToggle);AddButton(formatting,"Ajustar imagen",()=>{if(canvas.Selected==null||canvas.Selected.Kind!="Imagen")return;Snapshot();Editing.FitImage(canvas.Selected,label);RefreshAll();Mark();});
   fontSelector.SelectedIndexChanged+=(s,e)=>ApplyTypography();fontPoints.ValueChanged+=(s,e)=>ApplyTypography();boldToggle.CheckedChanged+=(s,e)=>ApplyTypography();
   aspectToggle.CheckedChanged+=(s,e)=>{if(formatSync||canvas.Selected==null||canvas.Selected.Kind!="Imagen")return;Snapshot();canvas.Selected.LockAspect=aspectToggle.Checked;if(aspectToggle.Checked){Editing.RestoreImageRatio(canvas.Selected);canvas.Selected.H=canvas.Selected.W/canvas.Selected.ImageAspect;Editing.FitImage(canvas.Selected,label);}RefreshAll();Mark();};
   var split=new SplitContainer{Size=new Size(950,650),Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel2,SplitterDistance=630};split.Panel1.Controls.Add(canvas);canvasHost=split.Panel1;var right=new SplitContainer{Size=new Size(330,650),Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=170};items.Dock=DockStyle.Fill;props.Dock=DockStyle.Fill;right.Panel1.Controls.Add(items);right.Panel1.Controls.Add(new System.Windows.Forms.Label{Text="Elementos del diseño",Dock=DockStyle.Top,Height=30,Padding=new Padding(6),BackColor=Color.FromArgb(225,238,230)});right.Panel2.Controls.Add(props);right.Panel2.Controls.Add(new System.Windows.Forms.Label{Text="Propiedades del elemento",Dock=DockStyle.Top,Height=30,Padding=new Padding(6),BackColor=Color.FromArgb(225,238,230)});split.Panel2.Controls.Add(right);
   var ribbon=new TabControl{Dock=DockStyle.Top,Height=139,Padding=new Point(18,5)};
   var homePage=new TabPage("Inicio"){BackColor=Color.FromArgb(244,248,245)};var home=RibbonUi.Row();homePage.Controls.Add(home);ribbon.TabPages.Add(homePage);
   var addGroup=RibbonUi.Group(home,"Añadir a la etiqueta",440);var addFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(3,0,0,0)};addGroup.Controls.Add(addFlow);addGroup.Controls.SetChildIndex(addFlow,0);
   string[] insertNames={"Texto","Imagen","Código\nbarras","Emojis","QR","Marco","Línea"};int insertIndex=0;foreach(Button button in insertTools.Controls.OfType<Button>().ToArray()){RibbonUi.Large(button,insertNames[insertIndex++]);addFlow.Controls.Add(button);}
   var fontGroup=RibbonUi.Group(home,"Texto",280);fontSelector.Width=174;fontSelector.DropDownWidth=220;RibbonUi.Place(fontGroup,fontSelector,7,6,174,26);RibbonUi.Place(fontGroup,fontPoints,188,6,59,26);var ptLabel=new System.Windows.Forms.Label{Text="pt",TextAlign=ContentAlignment.MiddleLeft};RibbonUi.Place(fontGroup,ptLabel,251,6,24,26);
   RibbonUi.Toggle(boldToggle,"N","Negrita",FontStyle.Bold);RibbonUi.Toggle(italicToggle,"K","Cursiva",FontStyle.Italic);RibbonUi.Toggle(underlineToggle,"S","Subrayado",FontStyle.Underline);RibbonUi.Place(fontGroup,boldToggle,7,39,32,30);RibbonUi.Place(fontGroup,italicToggle,42,39,32,30);RibbonUi.Place(fontGroup,underlineToggle,77,39,32,30);
   var smaller=Ui.Action("text","A−",()=>{if(fontPoints.Enabled)fontPoints.Value=Math.Max(1,fontPoints.Value-1);},47);smaller.Image=null;RibbonUi.Place(fontGroup,smaller,125,39,47,30);var larger=Ui.Action("text","A+",()=>{if(fontPoints.Enabled)fontPoints.Value=Math.Min(300,fontPoints.Value+1);},47);larger.Image=null;RibbonUi.Place(fontGroup,larger,175,39,47,30);fontPoints.EnabledChanged+=(s,e)=>{smaller.Enabled=larger.Enabled=fontPoints.Enabled;};
   var paragraphGroup=RibbonUi.Group(home,"Alineación del texto",115);RibbonUi.Alignment(paragraphGroup,alignment);
   var editGroup=RibbonUi.Group(home,"Elemento seleccionado",130);int editY=3;foreach(Button button in editTools.Controls.OfType<Button>().ToArray()){button.Text=editY==3?" Editar":editY==27?" Duplicar":" Eliminar";button.AccessibleName=editY==3?"Editar el contenido del elemento seleccionado":editY==27?"Duplicar el elemento seleccionado":"Eliminar el elemento seleccionado";if(editY==3)editButton=button;selectionButtons.Add(button);button.FlatAppearance.BorderSize=0;button.BackColor=Color.FromArgb(244,248,245);RibbonUi.Place(editGroup,button,5,editY,122,24);editY+=24;}
   var imageGroup=RibbonUi.Group(home,"Imagen",180);this.imageGroup=imageGroup;RibbonUi.Place(imageGroup,aspectToggle,5,8,180,26);var fitImage=formatting.Controls.OfType<Button>().FirstOrDefault(c=>c.Text=="Ajustar imagen");if(fitImage!=null){RibbonUi.Place(imageGroup,fitImage,5,40,170,29);fitImage.Enabled=aspectToggle.Enabled;aspectToggle.EnabledChanged+=(s,e)=>fitImage.Enabled=aspectToggle.Enabled;}
   var arrangePage=new TabPage("Posición"){BackColor=Color.FromArgb(244,248,245)};var arrangeFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(10),AutoScroll=true};
   var centerH=Ui.Action("size","Centrar horizontalmente",()=>CenterElement(true),200);var centerV=Ui.Action("size","Centrar verticalmente",()=>CenterElement(false),200);selectionButtons.Add(centerH);selectionButtons.Add(centerV);arrangeFlow.Controls.Add(centerH);arrangeFlow.Controls.Add(centerV);
   guidesToggle=new CheckBox{Text="Guías y ajuste al mover (mantén Alt para mover libre)",Checked=true,AutoSize=true,Margin=new Padding(12,8,8,8)};guidesToggle.CheckedChanged+=(s,e)=>{canvas.Guides=guidesToggle.Checked;if(guidesItem.Checked!=guidesToggle.Checked)guidesItem.Checked=guidesToggle.Checked;};arrangeFlow.Controls.Add(guidesToggle);arrangePage.Controls.Add(arrangeFlow);ribbon.TabPages.Add(arrangePage);
   tools.Controls.Remove(insertTools.Parent);tools.Controls.Remove(editTools.Parent);
   var bar=new StatusStrip();bar.Items.Add(status);Controls.Add(split);Controls.Add(tools);Controls.Add(ribbon);Controls.Add(steps);Controls.Add(actions);Controls.Add(menu);Controls.Add(bar);
   // Panel de propiedades: por secciones, sin barra técnica.
   props.ToolbarVisible=false;props.SizeChanged+=(s,e)=>WidenPropertyNames();props.SelectedObjectsChanged+=(s,e)=>WidenPropertyNames();props.PropertySort=PropertySort.Categorized;props.HelpVisible=true;props.Font=new Font("Segoe UI",9.5f);props.CategoryForeColor=Theme.BrandDark;props.LineColor=Color.FromArgb(234,242,237);
   // Bienvenida sobre el lienzo vacío.
   welcome.Visible=false;canvas.Controls.Add(welcome);canvas.Resize+=(s,e)=>PlaceWelcome();
   welcome.ChooseSize+=()=>{presets.Focus();presets.DroppedDown=true;};welcome.LoadExample+=LoadExample;welcome.OpenDesign+=Open;welcome.UseDymo+=ConnectDymo;
   chipTip.SetToolTip(printerChip,"Cambiar la impresora");
   canvas.MouseDoubleClick+=(s,e)=>{if(canvas.Selected!=null&&(canvas.Selected.Kind=="Texto"||canvas.Selected.Kind=="Código 39"||canvas.Selected.Kind=="QR"))EditText();};items.DoubleClick+=(s,e)=>{if(canvas.Selected!=null&&(canvas.Selected.Kind=="Texto"||canvas.Selected.Kind=="Código 39"||canvas.Selected.Kind=="QR"))EditText();};
   canvas.BeforeChange+=Snapshot;canvas.SelectionChanged+=()=>{syncing=true;items.SelectedItem=canvas.Selected;props.SelectedObject=canvas.Selected;syncing=false;SyncFormatting();};canvas.Changed+=()=>{props.Refresh();SyncFormatting();Mark();RefreshItems();};
   items.SelectedIndexChanged+=(s,e)=>{if(syncing)return;canvas.Selected=items.SelectedItem as Element;props.SelectedObject=canvas.Selected;SyncFormatting();canvas.Invalidate();};
   props.PropertyValueChanged+=(s,e)=>{int index=label.Elements.IndexOf(canvas.Selected);string previous=null;try{var descriptor=e.ChangedItem.PropertyDescriptor;var value=descriptor.GetValue(canvas.Selected);descriptor.SetValue(canvas.Selected,e.OldValue);previous=Model.Save(label);descriptor.SetValue(canvas.Selected,value);var selected=canvas.Selected;
    if(selected.Kind=="Imagen"&&selected.LockAspect){if(selected.ImageAspect<=0)Editing.RestoreImageRatio(selected);if(descriptor.Name=="W")selected.H=selected.W/selected.ImageAspect;if(descriptor.Name=="H")selected.W=selected.H*selected.ImageAspect;}
    if(selected.Kind=="Texto"&&new[]{"FontName","FontSize","Bold","Italic","Underline","TextAlign","Text"}.Contains(descriptor.Name))Editing.FitText(selected,label);Model.Validate(label);undo.Push(previous);RefreshAll();Mark();
   }catch(Exception ex){if(previous!=null){label=Model.Load(previous);canvas.Selected=index>=0?label.Elements[index]:null;RefreshAll();}MessageBox.Show(ex.Message,"Revisa el dato");}};

   FormClosing+=(s,e)=>{if(!Confirm())e.Cancel=true;};KeyPreview=true;KeyDown+=(s,e)=>{if(e.Control&&e.KeyCode==Keys.N){New();e.SuppressKeyPress=true;}if(e.Control&&e.KeyCode==Keys.O){Open();e.SuppressKeyPress=true;}if(e.Control&&e.KeyCode==Keys.P){Print();e.SuppressKeyPress=true;}if(e.KeyCode==Keys.F1){Help();e.SuppressKeyPress=true;}if(e.Control&&e.KeyCode==Keys.S){Save(false);e.SuppressKeyPress=true;}if(e.Control&&e.KeyCode==Keys.Z&&!props.ContainsFocus){Undo();e.SuppressKeyPress=true;}if(e.KeyCode==Keys.Delete&&!props.ContainsFocus){Delete();e.SuppressKeyPress=true;}};
   Activated+=(s,e)=>UpdatePrinterBanner();PaperPrinting.SelectionChanged+=UpdatePrinterBanner;FormClosed+=(s,e)=>PaperPrinting.SelectionChanged-=UpdatePrinterBanner;
   Shown+=async(s,e)=>{if(!Environment.GetCommandLineArgs().Contains("--render-ui"))await CheckUpdates(true);};
   RefreshAll();
  }
  public void RenderPreview(string path){label=new Label();label.Elements.Add(new Element{Text="AZOR AMBIENTAL",X=6,Y=5,W=88,H=10,FontSize=20,Bold=true});label.Elements.Add(new Element{Text="Identificación de muestras · Laboratorio",X=6,Y=16,W=88,H=7,FontSize=10});label.Elements.Add(new Element{Kind="Código 39",Text="AZOR001",X=6,Y=25,W=88,H=15});label.Elements.Add(new Element{Text="AZOR001",X=6,Y=41,W=70,H=7,FontSize=10});canvas.Selected=label.Elements[0];RefreshAll();Show();Application.DoEvents();SyncFormatting();PerformLayout();using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(path);}foreach(var tabs in Controls.OfType<TabControl>()){for(int i=1;i<tabs.TabCount;i++){tabs.SelectedIndex=i;Application.DoEvents();using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(Path.Combine(Path.GetDirectoryName(path),"vista_"+tabs.TabPages[i].Text+".png"));}}}}
  void AddButton(FlowLayoutPanel p,string text,Action a){var b=new Button{Text=text,AutoSize=true,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Color.White};b.Click+=(s,e)=>a();p.Controls.Add(b);}
  string exampleState;
  void ApplySize(){
   var selected=presets.SelectedItem as LabelFormat;bool dymo=selected!=null?selected.Dymo:label.Media=="DYMO LT12";
   if(selected!=null&&selected.W==0)dymo=selected.Dymo;
   if(dymo&&(width.Value<25||width.Value>200||height.Value!=12)){MessageBox.Show("DYMO LetraTag: cinta de 12 mm y longitud de 25 a 200 mm.");return;}
   Snapshot();bool empty=label.Elements.Count==0||Model.Save(label)==exampleState;label.Media=dymo?"DYMO LT12":selected!=null&&selected.Paper?"PAPER":"";label.Width=(float)width.Value;label.Height=(float)height.Value;
   if(empty){label=Formats.Example(label);FitDymoExample();canvas.Selected=label.Elements.FirstOrDefault();}
   RefreshAll();Mark();if(empty)exampleState=Model.Save(label);
  }
  void LoadExample(){
   if(label.Elements.Count>0&&MessageBox.Show("¿Sustituir el diseño por un ejemplo editable de este tamaño? Puedes recuperar el anterior con Deshacer.","Cargar ejemplo",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
   Snapshot();label=Formats.Example(label);FitDymoExample();canvas.Selected=label.Elements.FirstOrDefault();RefreshAll();Mark();exampleState=Model.Save(label);
  }
  void Snapshot(){undo.Push(Model.Save(label));if(undo.Count>60)undo=new Stack<string>(undo.Take(60).Reverse());}
  void Undo(){if(undo.Count==0)return;label=Model.Load(undo.Pop());canvas.Selected=null;RefreshAll();Mark();}
  void Mark(){dirty=true;Text="Impresorapp "+AppVersion.Text+" · "+(String.IsNullOrEmpty(file)?"Sin título":Path.GetFileName(file))+" *";UpdateStatus();}
  void ShowDymoSelection(){if(dymoPanel==null){dymoPanel=new LetraTagPanel(BuildDymoDesign);dymoPanel.SelectionChanged+=UpdatePrinterBanner;canvasHost.Controls.Add(dymoPanel);canvas.BringToFront();canvasHost.PerformLayout();}var saved=DymoStore.Load();if(saved!=null&&!dymoPanel.HasSelection)dymoPanel.UseDevice(saved);dymoPanel.Visible=true;UpdatePrinterBanner();}
  ToolStripMenuItem MenuItem(string text,string keys,Action action){var item=new ToolStripMenuItem(text,null,(s,e)=>action());if(keys!=null)item.ShortcutKeyDisplayString=keys;return item;}
  void UpdatePrinterBanner(){try{
   if(label.Media!="DYMO LT12"&&dymoPanel!=null&&dymoPanel.Visible)dymoPanel.Visible=false;
   if(label.Media=="PAPER"){printerChip.Kind="print";printerChip.Caption="Imprimir en papel";printerChip.Value=PaperPrinting.CurrentName;chipTip.SetToolTip(printerChip,"Impresora de Windows para papel. Pulsa para cambiarla.");return;}
   if(label.Media=="DYMO LT12"){printerChip.Kind="bluetooth";printerChip.Caption="Imprimir en";var saved=DymoStore.Load();string name=dymoPanel!=null&&dymoPanel.HasSelection?dymoPanel.SelectedDevice.Name:saved!=null?saved.Name:null;printerChip.Value=name==null?"DYMO sin conectar":name+" · por Bluetooth";chipTip.SetToolTip(printerChip,"DYMO LetraTag por Bluetooth. Pulsa para conectar otra.");return;}
   printerChip.Kind="network";printerChip.Caption="Imprimir en";var p=SavedPrinters.Load().Preferred;
   if(p==null){printerChip.Value="Sin impresora · Pulsa para elegir";chipTip.SetToolTip(printerChip,"Elige una impresora de etiquetas en Mis impresoras.");return;}
   printerChip.Value=p.Alias+(String.IsNullOrWhiteSpace(p.Location)?"":" · "+p.Location)+(p.Connection=="LAN"?" · por red":" · USB (pendiente)");
   chipTip.SetToolTip(printerChip,p.Alias+" · "+p.Connection+" · "+p.Address+(p.Connection=="LAN"?":"+p.Port:" · impresión USB pendiente")+"\nPulsa para cambiar de impresora.");
  }catch{printerChip.Caption="Imprimir en";printerChip.Value="No se pudo leer la impresora · Revisa Mis impresoras";}}
  void UpdateStatus(){UpdatePrinterBanner();string problem=Editing.MarginProblem(label);status.Text=problem??"Doble clic en un texto para editarlo · Ctrl+S guardar · Ctrl+Z deshacer · Supr eliminar";status.ForeColor=problem!=null?Color.Firebrick:Color.FromArgb(40,75,57);
   steps.Current=label.Elements.Count==0?2:3;bool empty=label.Elements.Count==0;welcome.Wanted=empty;welcome.Visible=empty;if(empty)PlaceWelcome();}
  void PlaceWelcome(){if(!welcome.Wanted)return;welcome.Arrange(canvas.ClientSize.Width-30);welcome.Left=Math.Max(10,(canvas.ClientSize.Width-welcome.Width)/2);welcome.Top=Math.Max(10,(canvas.ClientSize.Height-welcome.Height)/2);welcome.BringToFront();}
  // Deja más espacio a los nombres de las propiedades (por defecto el grid los corta a la mitad).
  void WidenPropertyNames(){try{var view=typeof(PropertyGrid).GetField("gridView",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(props);view.GetType().GetMethod("MoveSplitterTo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(view,new object[]{Math.Max(110,props.Width-125)});}catch{}}
  // En cinta DYMO, el ejemplo usa el mismo tamaño de letra que los textos nuevos y queda centrado.
  void FitDymoExample(){if(label.Media!="DYMO LT12")return;var backup=Model.Save(label);try{foreach(var el in label.Elements){if(el.Kind=="Texto"){el.FontSize=Editing.DymoFontSize();Editing.FitText(el,label);}else Editing.CenterOnTape(el,label);}if(Editing.MarginProblem(label)!=null)label=Model.Load(backup);}catch{label=Model.Load(backup);}}
  void ChoosePrinter(){if(label.Media=="PAPER"){PaperPrinting.Select(this,label);UpdatePrinterBanner();return;}OpenPrinters();}
  void About(){MessageBox.Show(this,"Impresorapp "+AppVersion.Text+"\n\nEditor de etiquetas y carteles para Windows.\nAutor: Alberto Marín (albertuxo85).\nFirmado digitalmente por Azor Ambiental Holdco S.L.U.\n\nhttps://github.com/albertuxo085/Impresorapp","Acerca de Impresorapp");}
  // Cambia el diseño a cinta DYMO con el largo indicado.
  void SwitchToDymo(decimal length){var custom=Formats.All.Last(x=>x.Dymo&&x.W==0);syncing=true;presets.SelectedItem=custom;syncing=false;width.Value=Math.Max(25,Math.Min(200,length));height.Value=12;ApplySize();ShowDymoSelection();}
  void ConnectDymo(){decimal current=label.Media=="DYMO LT12"?(decimal)label.Width:60;using(var d=new DymoConnectForm(current)){if(d.ShowDialog(this)!=DialogResult.OK||d.Device==null)return;try{DymoStore.Save(d.Device);}catch(Exception ex){MessageBox.Show(this,"No se pudo guardar la DYMO en este equipo: "+ex.Message,"DYMO");}if(dymoPanel!=null)dymoPanel.UseDevice(d.Device);SwitchToDymo(d.LabelLength);if(dymoPanel!=null)dymoPanel.UseDevice(d.Device);UpdatePrinterBanner();}}
  // Al pasar de la DYMO a una impresora de rollo, ofrece cambiar el formato en vez de dar error.
  void OfferRollFormat(){if(label.Media!="DYMO LT12")return;if(MessageBox.Show(this,"La impresora elegida usa etiquetas de rollo y el diseño actual es una cinta DYMO de 12 mm.\n\n¿Cambiar a un formato de rollo de 100 × 50 mm?","Cambiar formato",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;var roll=Formats.All.FirstOrDefault(x=>!x.Dymo&&!x.Paper&&x.W==100&&x.H==50);if(roll!=null)presets.SelectedItem=roll;}

  void RefreshItems(){syncing=true;var selected=canvas.Selected;items.DataSource=null;items.DataSource=label.Elements;items.SelectedItem=selected;syncing=false;}
  void RefreshAll(){syncing=true;presets.SelectedItem=Formats.Match(label);syncing=false;canvas.Label=label;width.Value=(decimal)label.Width;height.Value=(decimal)label.Height;RefreshItems();props.SelectedObject=canvas.Selected;SyncFormatting();canvas.Invalidate();UpdateStatus();}
  void Add(string kind){
   if(label.Elements.Count>=500){MessageBox.Show("Máximo 500 elementos por plantilla.");return;}
   var el=new Element{Kind=kind,X=Math.Min(3,label.Width/10),Y=Math.Min(3,label.Height/10),W=Math.Min(35,label.Width*.8f),H=Math.Min(10,label.Height*.5f)};
   if(kind=="QR"){string content=QrContent();if(content==null)return;el.Text=content;el.W=el.H=Math.Min(30,Math.Min(label.Width,label.Height)*.75f);}
   if(kind=="Código 39"){el.Text="AZOR001";el.W=label.Width*.9f;el.H=Math.Min(15,label.Height*.5f);}if(kind=="Marco"){el.Text="";el.H=Math.Min(20,label.Height*.8f);}if(kind=="Línea"){el.Text="";el.H=1;}
   if(kind=="Imagen")using(var d=new OpenFileDialog{Filter="Imágenes|*.png;*.jpg;*.jpeg;*.bmp"}){if(d.ShowDialog()!=DialogResult.OK)return;try{var bytes=File.ReadAllBytes(d.FileName);if(bytes.Length>10000000)throw new Exception("Máximo 10 MB por imagen.");using(var ms=new MemoryStream(bytes))using(var im=Image.FromStream(ms)){if(im.Width>12000||im.Height>12000)throw new Exception("Máximo 12000 píxeles por lado.");el.H=Math.Min(label.Height*.8f,el.W*im.Height/im.Width);}el.ImageData=Convert.ToBase64String(bytes);Editing.RestoreImageRatio(el);el.LockAspect=true;el.Text=Path.GetFileName(d.FileName);}catch(Exception ex){MessageBox.Show(ex.Message);return;}}
   bool dymo=label.Media=="DYMO LT12";
   if(dymo){float content=Editing.DymoContentHeight();el.X=Editing.NextFreeX(label);el.Y=2.5f;el.FontSize=kind=="Texto"?Editing.DymoFontSize():9;if(kind=="QR")el.W=el.H=content;else if(kind=="Código 39"||kind=="Marco")el.H=content;else if(kind=="Imagen")el.W=content*Math.Max(.05f,el.ImageAspect);else if(kind!="Línea")el.H=Math.Min(el.H,content);if(kind!="Texto"&&el.X+el.W>label.Width-.5f)el.X=Math.Max(.5f,label.Width-el.W-.5f);}
   if(el.Kind=="Imagen")Editing.FitImage(el,label);
   if(dymo)Editing.CenterOnTape(el,label);
   Snapshot();label.Elements.Add(el);canvas.Selected=el;RefreshAll();Mark();if(kind=="Texto")EditText();
  }
  void SyncFormatting(){formatSync=true;var e=canvas.Selected;bool text=e!=null&&e.Kind=="Texto";fontSelector.Enabled=fontPoints.Enabled=boldToggle.Enabled=italicToggle.Enabled=underlineToggle.Enabled=alignment.Enabled=text;if(text){string family=String.IsNullOrEmpty(e.FontName)?"Arial":e.FontName;if(!fontSelector.Items.Contains(family))fontSelector.Items.Add(family);fontSelector.SelectedItem=family;fontPoints.Value=(decimal)Math.Max(1,Math.Min(300,e.FontSize));boldToggle.Checked=e.Bold;italicToggle.Checked=e.Italic;underlineToggle.Checked=e.Underline;alignment.SelectedItem=String.IsNullOrEmpty(e.TextAlign)?"Izquierda":e.TextAlign;}aspectToggle.Enabled=e!=null&&e.Kind=="Imagen";aspectToggle.Checked=aspectToggle.Enabled&&e.LockAspect;foreach(var b in selectionButtons)b.Enabled=e!=null;if(editButton!=null)editButton.Enabled=e!=null&&(e.Kind=="Texto"||e.Kind=="Código 39"||e.Kind=="QR");if(imageGroup!=null)imageGroup.Visible=e!=null&&e.Kind=="Imagen";formatSync=false;}
  void ApplyTypography(){if(formatSync||canvas.Selected==null||canvas.Selected.Kind!="Texto")return;Snapshot();var e=canvas.Selected;e.FontName=fontSelector.SelectedItem as string??"Arial";e.FontSize=(float)fontPoints.Value;e.Bold=boldToggle.Checked;e.Italic=italicToggle.Checked;e.Underline=underlineToggle.Checked;Editing.FitText(e,label);RefreshAll();Mark();}
  void AddEmoji(){string symbol=Editing.PickEmoji(this);if(symbol==null)return;Snapshot();bool dymo=label.Media=="DYMO LT12";var e=new Element{Kind="Texto",Text=symbol,FontName="Segoe UI Emoji",FontSize=dymo?Editing.DymoFontSize():20,X=2,Y=2};Editing.FitText(e,label);if(dymo)Editing.PlaceAfterOthers(e,label);label.Elements.Add(e);canvas.Selected=e;RefreshAll();Mark();}
  void CenterElement(bool horizontal){if(canvas.Selected==null)return;Snapshot();var e=canvas.Selected;if(horizontal)e.X=Math.Max(0,(label.Width-e.W)/2);else e.Y=Math.Max(0,(label.Height-e.H)/2);RefreshAll();Mark();}
  string QrContent(string initial=""){using(var d=new Form{Text="Código QR · Texto o enlace",ClientSize=new Size(520,280),Font=Font,StartPosition=FormStartPosition.CenterParent,Icon=Branding.AppIcon,MinimizeBox=false,MaximizeBox=false}){var hint=new System.Windows.Forms.Label{Text="Pega una URL completa (https://...) o escribe texto. El contenido se guarda en el QR y se genera sin Internet. Máximo 1200 bytes UTF-8.",Left=16,Top=12,Width=488,Height=42};var input=new TextBox{Multiline=true,AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,Left=16,Top=60,Width=488,Height=155,Text=initial,MaxLength=1200};var ok=new Button{Text="Crear / aplicar QR",Left=220,Top=232,Width=170};var cancel=new Button{Text="Cancelar",Left=400,Top=232,Width=104,DialogResult=DialogResult.Cancel};ok.Click+=(s,e)=>{try{QrSupport.Matrix(input.Text);d.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(d,ex.Message,"Contenido QR");}};d.Controls.AddRange(new Control[]{hint,input,ok,cancel});d.CancelButton=cancel;return d.ShowDialog(this)==DialogResult.OK?input.Text:null;}}
  void EditText(){
   if(canvas.Selected!=null&&canvas.Selected.Kind=="QR"){var value=QrContent(canvas.Selected.Text);if(value!=null&&value!=canvas.Selected.Text){Snapshot();canvas.Selected.Text=value;RefreshAll();Mark();}return;}
   var selected=canvas.Selected;if(selected==null||(selected.Kind!="Texto"&&selected.Kind!="Código 39")){MessageBox.Show("Selecciona un texto o código de barras y pulsa Editar texto. También puedes hacer doble clic sobre él.","Editar texto");return;}
   using(var dialog=new Form{Text=selected.Kind=="Texto"?"Editar texto de la etiqueta":"Editar contenido del código de barras",ClientSize=new Size(520,250),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false,FormBorderStyle=FormBorderStyle.FixedDialog,Font=Font}){
    var input=new TextBox{Multiline=selected.Kind=="Texto",AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,Text=selected.Text??"",Left=16,Top=34,Width=488,Height=145,MaxLength=4000};
    var hint=new System.Windows.Forms.Label{Text=selected.Kind=="Texto"?"Escribe el texto:":"Código 39: números y MAYÚSCULAS, sin tildes. Ejemplo: AZOR001",Left=16,Top=12,Width=488,Height=20};
    var ok=new Button{Text="Aplicar",Left=320,Top=200,Width=88};var cancel=new Button{Text="Cancelar",Left=416,Top=200,Width=88,DialogResult=DialogResult.Cancel};
    ok.Click+=(s,e)=>{try{if(selected.Kind=="Código 39")Barcode.Check(input.Text);dialog.DialogResult=DialogResult.OK;dialog.Close();}catch(Exception ex){MessageBox.Show(dialog,ex.Message,"Revisa el contenido");}};
    dialog.Controls.AddRange(new Control[]{hint,input,ok,cancel});dialog.CancelButton=cancel;dialog.Shown+=(s,e)=>{input.Focus();input.SelectAll();};
    if(dialog.ShowDialog(this)==DialogResult.OK&&input.Text!=selected.Text){Snapshot();selected.Text=input.Text;if(selected.Kind=="Texto")Editing.FitText(selected,label);RefreshAll();Mark();}
   }
  }
  void Duplicate(){if(canvas.Selected==null||label.Elements.Count>=500)return;Snapshot();var copy=Model.Load(Model.Save(new Label{Elements=new List<Element>{canvas.Selected}})).Elements[0];copy.X+=2;copy.Y+=2;label.Elements.Add(copy);canvas.Selected=copy;RefreshAll();Mark();}
  void Delete(){if(canvas.Selected==null)return;Snapshot();label.Elements.Remove(canvas.Selected);canvas.Selected=null;RefreshAll();Mark();}
  bool Confirm(){if(!dirty)return true;var r=MessageBox.Show("¿Guardar los cambios de la etiqueta?","Cambios pendientes",MessageBoxButtons.YesNoCancel);if(r==DialogResult.Cancel)return false;if(r==DialogResult.Yes)return Save(false);return true;}
  void New(){if(!Confirm())return;label=new Label();file="";dirty=false;undo.Clear();canvas.Selected=null;Text="Impresorapp "+AppVersion.Text+" · Sin título";RefreshAll();}
  void Open(){if(!Confirm())return;using(var d=new OpenFileDialog{Filter="Diseños Impresorapp|*.etiqueta"}){if(d.ShowDialog()!=DialogResult.OK)return;try{if(new FileInfo(d.FileName).Length>30000000)throw new Exception("La plantilla supera 30 MB.");label=Model.Load(File.ReadAllText(d.FileName,Encoding.UTF8));file=d.FileName;dirty=false;undo.Clear();canvas.Selected=null;Text="Impresorapp "+AppVersion.Text+" · "+Path.GetFileName(file);RefreshAll();}catch(Exception ex){MessageBox.Show(ex.Message,"No se pudo abrir");}}}
  bool Save(bool asNew){try{Model.Validate(label);string folder;try{folder=DesignStorage.EnsureDefaultFolder();}catch(Exception ex){MessageBox.Show(this,"No se pudo crear la carpeta de diseños. Puedes elegir otra ubicación.\n"+ex.Message,"Carpeta de diseños");folder=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);}using(var d=new SaveFileDialog{Title="Guardar diseño · Impresorapp",InitialDirectory=folder,RestoreDirectory=true,Filter="Diseños de etiquetas y carteles|*.etiqueta",DefaultExt="etiqueta",AddExtension=true,OverwritePrompt=true,FileName=String.IsNullOrEmpty(file)?"Mi diseño.etiqueta":Path.GetFileName(file)}){if(d.ShowDialog(this)!=DialogResult.OK)return false;DesignStorage.Write(d.FileName,Model.Save(label));file=d.FileName;}dirty=false;Text="Impresorapp "+AppVersion.Text+" · "+Path.GetFileName(file);return true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar");return false;}}
  bool checkingUpdates;
  async Task CheckUpdates(bool automatic){if(checkingUpdates)return;checkingUpdates=true;try{string exe=await Updates.Check(this,automatic);if(exe==null||IsDisposed)return;if(MessageBox.Show(this,"La versión nueva está lista. ¿Guardar los cambios pendientes y abrirla ahora?","Actualizar Impresorapp",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;if(!Confirm())return;Updates.Launch(exe);dirty=false;Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo abrir la actualización");}finally{checkingUpdates=false;}}
  Bitmap BuildDymoDesign(){Model.Validate(label);if(label.Media!="DYMO LT12")throw new Exception("Elige un formato DYMO LT en la columna Formato de etiqueta.");if(label.Elements.Count==0)throw new Exception("Añade texto, imágenes u otros elementos desde el editor.");string problem=Editing.MarginProblem(label);if(problem!=null)throw new Exception(problem);if(label.Width<25||label.Width>200||label.Height!=12)throw new Exception("La LetraTag usa cinta de 12 mm y longitudes de 25 a 200 mm.");int dots=(int)Math.Round(label.Width*200/25.4);var b=new Bitmap(dots+47,30);try{using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;g.ScaleTransform(200f/25.4f,100f/25.4f);g.TranslateTransform(0,-2.2f);Render.Draw(g,label);}return b;}catch{b.Dispose();throw;}}
  async void LetraTag(){try{if(label.Media!="DYMO LT12"){ConnectDymo();return;}ShowDymoSelection();if(!dymoPanel.HasSelection)ConnectDymo();if(dymoPanel!=null&&dymoPanel.HasSelection)await dymoPanel.PrintCurrent();}catch(Exception ex){MessageBox.Show(ex.Message,"DYMO");}}

  void OpenPrinters(){string choice;using(var d=new Discovery()){d.ShowDialog(this);choice=d.Choice;}if(choice=="dymo-connect")ConnectDymo();else if(choice=="dymo-use"){var saved=DymoStore.Load();if(saved==null)ConnectDymo();else{SwitchToDymo(label.Media=="DYMO LT12"?(decimal)label.Width:60);if(dymoPanel!=null)dymoPanel.UseDevice(saved);}}else if(choice=="printer")OfferRollFormat();UpdatePrinterBanner();}
  void PrintPaper(){try{PaperPrinting.Show(this,label);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Imprimir en papel");}}
  void Print(){if(label.Media=="PAPER"){PrintPaper();return;}if(label.Media=="DYMO LT12"){LetraTag();return;}try{Model.Validate(label);if(label.Elements.Count==0)throw new Exception("Añade algún elemento antes de imprimir.");if(Model.Outside(label))throw new Exception("Hay elementos fuera de la etiqueta.");using(var f=new DirectPrintForm(label,"",SavedPrinters.Load().Preferred))f.ShowDialog(this);UpdatePrinterBanner();}catch(Exception ex){MessageBox.Show(ex.Message,"Impresión directa");}}
  void Help(){MessageBox.Show(this,"1. TAMAÑO: en la columna de la izquierda elige un formato (rollo de etiquetas, cinta DYMO o papel A4 / A3). Para una medida libre, escribe ancho y alto y pulsa Aplicar medida personalizada.\n\n2. CONTENIDO: en la pestaña Inicio pulsa Texto, Imagen, Código de barras, QR, Emojis, Marco o Línea. Arrastra para mover y haz doble clic en un texto para cambiarlo. A la derecha puedes ajustar cada elemento por secciones. La pestaña Posición centra elementos y activa las guías.\n\n3. IMPRIMIR: arriba a la derecha verás en qué impresora se imprimirá. Pulsa sobre ella para cambiarla (Mis impresoras, búsqueda en la red o DYMO por Bluetooth) y después pulsa Imprimir.\n\nAtajos: Ctrl+N nueva · Ctrl+O abrir · Ctrl+S guardar · Ctrl+P imprimir · Ctrl+Z deshacer · Supr eliminar · F1 esta ayuda.\n\nEl rollo físico debe coincidir con las medidas del diseño. No se busca en la red automáticamente.","Guía rápida · Impresorapp · Alberto Marín (albertuxo85)");}
 }
 public class Found {public string Name,Address,Detail,Kind,DeviceName,Location,MetadataSource;public bool Verified;public int[] Ports=new int[0];}
 public static class Network {
  public static async Task<bool> Port(string host,int port,CancellationToken ct){using(var c=new TcpClient()){try{var connect=c.ConnectAsync(host,port);var delay=Task.Delay(450,ct);if(await Task.WhenAny(connect,delay)!=connect)return false;await connect;return c.Connected;}catch{return false;}}}
  public static string Run(string exe,string args,int timeout){using(var p=new Process{StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.GetEncoding(850),StandardErrorEncoding=Encoding.GetEncoding(850)}}){p.Start();var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(timeout)){try{p.Kill();}catch{}throw new Exception("El equipo no respondió a tiempo o la consulta está bloqueada.");}Task.WaitAll(output,error);if(p.ExitCode!=0)throw new Exception((output.Result+" "+error.Result).Trim());return output.Result;}}
  public static List<Found> Shares(string host){
   if(!Regex.IsMatch(host,@"^[A-Za-z0-9][A-Za-z0-9.\-]{0,252}$"))throw new Exception("Introduce un nombre de equipo o una dirección IPv4, sin barras ni espacios.");
   string output=Run("net.exe","view \\\\"+host+" /all",6000);var list=new List<Found>();
   foreach(string row in output.Split('\n')){var m=Regex.Match(row.TrimEnd(),@"^(.+?)\s{2,}(Print|Impresora|Imprimir|Impresión|Impresion|Drucker|Impression)\s*(.*)$",RegexOptions.IgnoreCase);if(m.Success)list.Add(new Found{Name=m.Groups[1].Value.Trim(),Address="\\\\"+host+"\\"+m.Groups[1].Value.Trim(),Detail="Recurso compartido de impresión",Kind="Compartida"});}
   return list;
  }
  public static string Prefix(){foreach(var n in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback)){foreach(var a in n.GetIPProperties().UnicastAddresses){if(a.Address.AddressFamily!=AddressFamily.InterNetwork)continue;string ip=a.Address.ToString();if(ip.StartsWith("169.254."))continue;return ip.Substring(0,ip.LastIndexOf('.')+1);}}return "192.168.1.";}
  public static List<IPAddress> Range(string first,string last){IPAddress a,b;if(!IPAddress.TryParse(first,out a)||!IPAddress.TryParse(last,out b)||a.AddressFamily!=AddressFamily.InterNetwork||b.AddressFamily!=AddressFamily.InterNetwork)throw new Exception("Introduce dos direcciones IPv4 válidas.");var aa=a.GetAddressBytes();var bb=b.GetAddressBytes();if(!aa.Take(3).SequenceEqual(bb.Take(3))||aa[3]<1||bb[3]>254||aa[3]>bb[3])throw new Exception("Selecciona un rango de 1 a 254 dentro del mismo bloque, por ejemplo 192.168.1.1 a 192.168.1.254.");var result=new List<IPAddress>();for(int i=aa[3];i<=bb[3];i++)result.Add(IPAddress.Parse(aa[0]+"."+aa[1]+"."+aa[2]+"."+i));return result;}
 }
 public class Discovery:Form {
  CheckBox possible=new CheckBox();List<Found> inventory=new List<Found>();TextBox first=new TextBox(),last=new TextBox(),host=new TextBox();ListView results=new ListView();System.Windows.Forms.Label message=new System.Windows.Forms.Label();Button scan=new Button(),shares=new Button(),cancel=new Button();CancellationTokenSource cts;bool closing;
  public SavedPrinter SelectedPrinter {get;private set;}
  // Qué ha elegido el usuario: "printer" (impresora de red o USB), "dymo-connect" o "dymo-use".
  public string Choice {get;private set;}
  void ChooseNetwork(){try{if(results.SelectedItems.Count==0)return;SelectedPrinter=SavedPrinters.AddNetwork(results.SelectedItems[0].Tag as Found);Choice="printer";DialogResult=DialogResult.OK;Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Seleccionar impresora");}}
  public Discovery(){
   Text="Impresoras";Icon=Branding.AppIcon;Size=new Size(980,640);MinimumSize=new Size(880,560);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);
   // Búsqueda en red: un botón principal; rango, compartidas y puertos en «Opciones avanzadas».
   var top=new Panel{Dock=DockStyle.Top,Height=70,Padding=new Padding(14,12,14,8),BackColor=Color.FromArgb(247,250,248)};
   scan.Text=" Buscar impresoras en la red";scan.Image=Ui.Icon("network",Color.White);scan.TextImageRelation=TextImageRelation.ImageBeforeText;scan.Size=new Size(250,44);scan.Location=new Point(14,12);scan.FlatStyle=FlatStyle.Flat;scan.FlatAppearance.BorderSize=0;scan.BackColor=Theme.Brand;scan.ForeColor=Color.White;scan.Font=new Font("Segoe UI",10.5f,FontStyle.Bold);scan.Click+=async(s,e)=>await Scan();top.Controls.Add(scan);
   scan.EnabledChanged+=(s,e)=>scan.BackColor=scan.Enabled?Theme.Brand:Color.FromArgb(160,180,168);
   cancel.Text="Cancelar búsqueda";cancel.Enabled=false;cancel.Size=new Size(150,44);cancel.Location=new Point(272,12);cancel.Click+=(s,e)=>{if(cts!=null)cts.Cancel();};top.Controls.Add(cancel);
   var intro=new System.Windows.Forms.Label{Text="Busca impresoras de etiquetas conectadas por cable o Wi-Fi. Puede tardar un minuto y puedes cancelarla cuando quieras.",Location=new Point(436,12),Size=new Size(500,44),ForeColor=Theme.Muted};top.Controls.Add(intro);top.Resize+=(s,e)=>intro.Width=Math.Max(200,top.ClientSize.Width-intro.Left-14);
   var advancedToggle=new CheckBox{Text="Opciones avanzadas",AutoSize=true,Font=new Font("Segoe UI",9.5f,FontStyle.Bold),ForeColor=Theme.BrandDark,Margin=new Padding(14,4,4,4)};
   var advancedBar=new Panel{Dock=DockStyle.Top,Height=34,Padding=new Padding(14,4,14,0),BackColor=Color.FromArgb(247,250,248)};advancedToggle.Dock=DockStyle.Left;advancedBar.Controls.Add(advancedToggle);
   var advanced=new FlowLayoutPanel{Dock=DockStyle.Top,Height=104,Padding=new Padding(14,6,14,6),WrapContents=true,Visible=false,BackColor=Color.White};advancedToggle.CheckedChanged+=(s,e)=>advanced.Visible=advancedToggle.Checked;
   string prefix=Network.Prefix();var remembered=AppPreferences.Load();first.Text=remembered==null?prefix+"1":remembered.First;last.Text=remembered==null?prefix+"254":remembered.Last;first.Width=130;last.Width=130;host.Width=180;host.Text="";first.AccessibleName="Buscar desde la IP";last.AccessibleName="Buscar hasta la IP";host.AccessibleName="Nombre o IP del equipo";
   advanced.Controls.Add(new System.Windows.Forms.Label{Text="Buscar desde la IP",AutoSize=true,Margin=new Padding(3,7,3,3)});advanced.Controls.Add(first);advanced.Controls.Add(new System.Windows.Forms.Label{Text="hasta la IP",AutoSize=true,Margin=new Padding(8,7,3,3)});advanced.Controls.Add(last);
   advanced.SetFlowBreak(last,true);advanced.Controls.Add(new System.Windows.Forms.Label{Text="Ver las impresoras compartidas del equipo",AutoSize=true,Margin=new Padding(3,7,3,3)});advanced.Controls.Add(host);shares.Text="Consultar";shares.AutoSize=true;shares.Click+=async(s,e)=>await Shared();advanced.Controls.Add(shares);advanced.SetFlowBreak(shares,true);
   possible.Text="Mostrar también dispositivos sin identificar (puede incluir aparatos que no son impresoras)";possible.AutoSize=true;possible.Checked=false;possible.CheckedChanged+=(s,e)=>RefreshFound();advanced.Controls.Add(possible);
   results.SelectedIndexChanged+=(s,e)=>{if(results.SelectedItems.Count>0&&results.SelectedItems[0].SubItems[2].Text=="Equipo / SMB")host.Text=results.SelectedItems[0].SubItems[1].Text;};results.Dock=DockStyle.Fill;results.View=View.Details;results.FullRowSelect=true;results.DoubleClick+=(s,e)=>ChooseNetwork();results.Columns.Add("Modelo / descripción",220);results.Columns.Add("IP / conexión",145);results.Columns.Add("Tipo",100);results.Columns.Add("Nombre configurado",180);results.Columns.Add("Ubicación / departamento",210);results.Columns.Add("Detalle",310);results.ShowItemToolTips=true;
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=86,Padding=new Padding(14,8,14,6)};var add=Ui.Action("save","Guardar en Mis impresoras y usarla",()=>ChooseNetwork(),280,true);bottom.Controls.Add(add);var copy=new Button{Text="Copiar dirección",AutoSize=true,Height=32};copy.Click+=(s,e)=>{if(results.SelectedItems.Count>0)Clipboard.SetText(results.SelectedItems[0].SubItems[1].Text);};bottom.Controls.Add(copy);message.AutoSize=false;message.Width=920;message.Height=35;message.ForeColor=Theme.Muted;bottom.SetFlowBreak(copy,true);bottom.Controls.Add(message);
   var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new Point(16,6)};var savedTab=new TabPage("Mis impresoras");var saved=new SavedPrinterPanel();saved.SelectedPrinter+=p=>{SelectedPrinter=p;Choice="printer";DialogResult=DialogResult.OK;Close();};saved.DymoConnect+=()=>{Choice="dymo-connect";DialogResult=DialogResult.OK;Close();};saved.DymoUse+=()=>{Choice="dymo-use";DialogResult=DialogResult.OK;Close();};savedTab.Controls.Add(saved);tabs.TabPages.Add(savedTab);var networkTab=new TabPage("Buscar en la red");var usbTab=new TabPage("USB de este equipo");
   networkTab.Controls.Add(results);networkTab.Controls.Add(advanced);networkTab.Controls.Add(advancedBar);networkTab.Controls.Add(top);networkTab.Controls.Add(bottom);results.AccessibleName="Impresoras encontradas";var usb=new UsbPrinterPanel();usbTab.Controls.Add(usb);tabs.TabPages.Add(networkTab);tabs.TabPages.Add(usbTab);tabs.SelectedIndexChanged+=async(s,e)=>{if(tabs.SelectedTab==usbTab)await usb.RefreshDevices();};Controls.Add(tabs);message.Text=remembered==null?"Introduce el rango. Se recordará al cerrar esta ventana; no se busca automáticamente.":"Se recuerda el último rango usado. Pulsa Buscar impresoras en la red cuando quieras.";FormClosing+=(s,e)=>{RememberRange();closing=true;if(cts!=null)cts.Cancel();};
  }
  void RememberRange(){try{if(AppPreferences.Valid(first.Text.Trim(),last.Text.Trim()))AppPreferences.Save(first.Text.Trim(),last.Text.Trim());}catch(Exception ex){MessageBox.Show(this,"No se pudo recordar el rango de IP: "+ex.Message,"Preferencias");}}
  void Found(Found f){if(closing)return;var old=inventory.FirstOrDefault(x=>x.Address==f.Address&&x.Kind==f.Kind);if(old!=null)inventory.Remove(old);inventory.Add(f);RefreshFound();}
  void RefreshFound(){if(closing)return;string selected=results.SelectedItems.Count>0?results.SelectedItems[0].SubItems[1].Text:null;results.BeginUpdate();results.Items.Clear();foreach(var f in inventory.Where(x=>x.Verified||x.Kind=="Compartida"||possible.Checked).OrderBy(x=>x.Address)){var row=new ListViewItem(new[]{f.Name,f.Address,f.Kind,String.IsNullOrWhiteSpace(f.DeviceName)?"Sin nombre informado":f.DeviceName,String.IsNullOrWhiteSpace(f.Location)?"Sin ubicación informada":f.Location,f.Detail}){Tag=f,ToolTipText=f.MetadataSource??f.Detail};results.Items.Add(row);if(f.Address==selected)row.Selected=true;}results.EndUpdate();}
  bool Begin(){if(cts!=null)return false;cts=new CancellationTokenSource();scan.Enabled=shares.Enabled=false;cancel.Enabled=true;return true;}
  void End(){cts.Dispose();cts=null;if(closing)return;scan.Enabled=shares.Enabled=true;cancel.Enabled=false;}
  async Task Shared(){if(!Begin())return;string name=host.Text.Trim();try{message.Text="Consultando impresoras compartidas de "+name+"…";var list=await Task.Run(()=>Network.Shares(name));if(closing)return;foreach(var f in list)Found(f);message.Text=list.Count==0?"La consulta no identificó recursos de impresión. Revisa permisos, idioma de Windows y configuración de uso compartido.":list.Count+" impresoras compartidas identificadas. Compartición detectada; no es impresión directa sin controladores.";}catch(Exception ex){if(!closing)message.Text="No se pudieron consultar las compartidas: "+ex.Message;}finally{End();}}
  async Task Scan(){
   List<IPAddress> addresses;try{addresses=Network.Range(first.Text.Trim(),last.Text.Trim());}catch(Exception ex){MessageBox.Show(ex.Message);return;}RememberRange();if(!Begin())return;var token=cts.Token;int done=0;var gate=new SemaphoreSlim(16);inventory.RemoveAll(x=>x.Kind!="Compartida");RefreshFound();
   try{
    message.Text="Identificando impresoras por SNMP y comprobando puertos de impresión; no se listan equipos SMB…";
    var jobs=addresses.Select(async ip=>{await gate.WaitAsync(token);try{token.ThrowIfCancellationRequested();var printer=await PrinterDiscovery.Probe(ip.ToString(),token);token.ThrowIfCancellationRequested();if(closing)return;if(printer!=null)Found(printer);message.Text="Revisadas "+(++done)+" de "+addresses.Count+" direcciones · "+inventory.Count(x=>x.Verified)+" impresoras identificadas…";}finally{gate.Release();}}).ToArray();await Task.WhenAll(jobs);
    if(!closing)message.Text="Finalizado: "+inventory.Count(x=>x.Verified)+" impresoras identificadas. "+inventory.Count(x=>x.Kind=="Sin verificar")+" candidatos sin verificar ocultos salvo que actives la casilla. SNMP bloqueado puede impedir identificar una impresora.";
   }catch(OperationCanceledException){if(!closing)message.Text="Búsqueda cancelada. Se conservan las impresoras encontradas.";}catch(Exception ex){if(!closing)message.Text="No se completó la búsqueda: "+ex.Message;}finally{gate.Dispose();End();}
  }

 }
 static class Program {
  [STAThread] static void Main(string[] args){
   if(args.Contains("--self-test")){try{SelfTest();}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error_pruebas.txt"),ex.ToString());Environment.ExitCode=1;}return;}
   if(!args.Contains("--render-ui")&&Updates.TryRedirect())return;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Contains("--render-ui")){using(var f=new MainForm())f.RenderPreview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"vista_editor.png"));return;}Application.Run(new MainForm());
  }
  static void Assert(bool b,string message){if(!b)throw new Exception(message);}
  static void SelfTest(){
   var l=new Label();l.Elements.Add(new Element{Text="Ázor · prueba",X=2,Y=3});var loaded=Model.Load(Model.Save(l));Assert(loaded.Elements[0].Text==l.Elements[0].Text,"Round trip UTF-8");Assert(!Model.Outside(loaded),"Bounds");loaded.Elements[0].X=99;Assert(Model.Outside(loaded),"Overflow");
   Assert(Network.Range("192.168.1.1","192.168.1.254").Count==254,"IPv4 range");bool rejected=false;try{Network.Range("192.168.1.1","192.168.2.1");}catch{rejected=true;}Assert(rejected,"Cross block rejected");
   rejected=false;try{Barcode.Check("abc*");}catch{rejected=true;}Assert(rejected,"Invalid barcode");Barcode.Check("AZOR-001");
   l.Elements.Add(new Element{Kind="Código 39",Text="AZOR001",X=2,Y=20,W=80,H=15});using(var bitmap=new Bitmap(800,400))using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.ScaleTransform(8,8);Render.Draw(g,l);bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"prueba_render.png"));}
   DesignStorage.SelfTest();DymoStore.SelfTest();Updates.SelfTest();QrSupport.SelfTest();SmartGuides.SelfTest();DesignerVerification.Run();SavedPrinters.SelfTest();UsbPrinters.SelfTest();AppPreferences.SelfTest();Formats.SelfTest();DirectTests.Run();LTProtocol.SelfTest();Editing.SelfTest();PrinterDiscovery.SelfTest();
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"comprobaciones.txt"),"OK: guardado/carga UTF-8, límites, rango IPv4, validación Code39, renderizado, mapa monocromo y segmentos ZPL. LetraTag: orden de bits, checksum, fragmentación y reconstrucción comprobados. Pruebas TCP/Bluetooth físicas pendientes. No se ha ejecutado barrido de red ni impresión física.");
  }
 }
}
