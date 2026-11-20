using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

// Interfaz 0.30: barra superior en verde azulado con grupos de iconos, herramientas a la izquierda,
// barra de formato según la selección, propiedades a la derecha y pantalla Nuevo / Abrir con plantillas.
// C# 5 (csc de .NET Framework): sin interpolación ni operador ?.
namespace EtiquetaLocal {
 public static class Look {
  public static readonly Color Teal=Color.FromArgb(0,101,115),TealDark=Color.FromArgb(0,78,89),TealHover=Color.FromArgb(0,124,140),TealSoft=Color.FromArgb(226,240,242),TealLine=Color.FromArgb(190,218,222),Canvas=Color.FromArgb(214,221,223);
  public static readonly string IconFont=FontFamily.Families.Any(f=>f.Name=="Segoe Fluent Icons")?"Segoe Fluent Icons":"Segoe MDL2 Assets";
  // Botón de icono: glifo del sistema (Segoe MDL2/Fluent) o un dibujo propio.
  public static Button Icon(string glyph,string tip,Action click,Color fore,Color back,Color hover,int size=40,Action<Graphics,Rectangle,Color> paint=null){
   var b=new Button{Text=paint==null?glyph:"",Font=new Font(IconFont,15),ForeColor=fore,BackColor=back,FlatStyle=FlatStyle.Flat,Size=new Size(size,size),Margin=new Padding(1),TabStop=true,AccessibleName=tip,AccessibleRole=AccessibleRole.PushButton,UseVisualStyleBackColor=false,Cursor=Cursors.Hand};
   b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=hover;b.FlatAppearance.MouseDownBackColor=hover;
   if(paint!=null)b.Paint+=(s,e)=>{e.Graphics.SmoothingMode=SmoothingMode.None;var r=new Rectangle((b.Width-20)/2,(b.Height-20)/2,20,20);paint(e.Graphics,r,b.Enabled?fore:Color.FromArgb(120,fore));};
   Tips.SetToolTip(b,tip);b.Click+=(s,e)=>click();return b;
  }
  public static readonly ToolTip Tips=new ToolTip{InitialDelay=300};
  public static void Barcode(Graphics g,Rectangle r,Color c){using(var br=new SolidBrush(c)){int x=r.X;int[] w={2,1,1,3,1,2,1,1,2,3,1,2};bool ink=true;foreach(int v in w){if(ink)g.FillRectangle(br,x,r.Y+2,v,r.Height-4);x+=v+(ink?0:0);ink=!ink;x+=0;if(x>r.Right)break;}}}
  // Grupo de la barra superior: título pequeño arriba y botones debajo.
  public static FlowLayoutPanel Group(Control parent,string title){
   var box=new Panel{Height=64,Margin=new Padding(0),Padding=new Padding(8,0,8,0),BackColor=Teal,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink};
   var caption=new System.Windows.Forms.Label{Text=title,ForeColor=Color.FromArgb(205,230,234),Font=new Font("Segoe UI",8),AutoSize=true,Location=new Point(8,2)};
   var row=new FlowLayoutPanel{Location=new Point(4,18),AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,BackColor=Teal,Margin=Padding.Empty,Padding=Padding.Empty};
   box.Controls.Add(caption);box.Controls.Add(row);
   box.Paint+=(s,e)=>{using(var p=new Pen(Color.FromArgb(70,255,255,255)))e.Graphics.DrawLine(p,box.Width-1,8,box.Width-1,box.Height-6);};
   parent.Controls.Add(box);return row;
  }
 }

 public partial class MainForm {
  Stack<string> redo=new Stack<string>();
  Panel topBar,toolStripLeft;FlowLayoutPanel formatBar;
  void Redo(){if(redo.Count==0)return;undo.Push(Model.Save(label));label=Model.Load(redo.Pop());canvas.Selected=null;RefreshAll();Mark();}

  void ApplyShell(){
   SuspendLayout();
   var ribbon=Controls.OfType<TabControl>().FirstOrDefault();
   var oldActions=printerChip.Parent!=null?printerChip.Parent.Parent:null;
   var printButton=printerChip.Parent!=null?printerChip.Parent.Controls.OfType<Button>().FirstOrDefault(b=>b!=printerChip):null;
   var leftTools=Controls.OfType<FlowLayoutPanel>().FirstOrDefault(p=>p.Dock==DockStyle.Left);
   var bar=Controls.OfType<StatusStrip>().FirstOrDefault();

   // ---- Barra superior ----
   topBar=new Panel{Dock=DockStyle.Top,Height=68,BackColor=Look.Teal,Padding=new Padding(4,2,10,2)};
   var groups=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,BackColor=Look.Teal,AutoScroll=false};
   Color w=Color.White,bk=Look.Teal,hv=Look.TealHover;
   var fileGroup=Look.Group(groups,"Archivo");
   fileGroup.Controls.Add(Look.Icon("","Nueva etiqueta (Ctrl+N)",NewFromGallery,w,bk,hv));
   fileGroup.Controls.Add(Look.Icon("","Abrir un diseño (Ctrl+O)",Open,w,bk,hv));
   fileGroup.Controls.Add(Look.Icon("","Guardar (Ctrl+S)",()=>Save(false),w,bk,hv));
   fileGroup.Controls.Add(Look.Icon("","Plantillas y tamaños",ShowGallery,w,bk,hv));
   fileGroup.Controls.Add(Look.Icon("","Catálogo de etiquetas Azor",ShowCatalog,w,bk,hv,40,(g,r,c)=>{using(var p=new Pen(c,1.6f)){g.DrawRectangle(p,r.X+1,r.Y+2,16,14);g.DrawLine(p,r.X+9,r.Y+2,r.X+9,r.Y+16);g.DrawLine(p,r.X+3,r.Y+6,r.X+7,r.Y+6);g.DrawLine(p,r.X+3,r.Y+10,r.X+7,r.Y+10);g.DrawLine(p,r.X+11,r.Y+6,r.X+15,r.Y+6);g.DrawLine(p,r.X+11,r.Y+10,r.X+15,r.Y+10);}}));
   var edit=Look.Group(groups,"Editar");
   edit.Controls.Add(Look.Icon("","Deshacer (Ctrl+Z)",Undo,w,bk,hv));
   edit.Controls.Add(Look.Icon("","Rehacer (Ctrl+Y)",Redo,w,bk,hv));
   edit.Controls.Add(Look.Icon("","Cortar (Ctrl+X)",()=>CopySelected(true),w,bk,hv));
   edit.Controls.Add(Look.Icon("","Copiar (Ctrl+C)",()=>CopySelected(false),w,bk,hv));
   edit.Controls.Add(Look.Icon("","Pegar (Ctrl+V)",()=>Paste(null),w,bk,hv));
   edit.Controls.Add(Look.Icon("","Eliminar (Supr)",Delete,w,bk,hv));
   var layout=Look.Group(groups,"Diseño");
   layout.Controls.Add(DropIcon("","Colocar en la etiqueta",()=>new ToolStripItem[]{Item("Centrar en la etiqueta",null,()=>AlignTo("center")),new ToolStripSeparator(),Item("A la izquierda",null,()=>AlignTo("left")),Item("Centrar horizontalmente",null,()=>AlignTo("hcenter")),Item("A la derecha",null,()=>AlignTo("right")),new ToolStripSeparator(),Item("Arriba",null,()=>AlignTo("top")),Item("Centrar verticalmente",null,()=>AlignTo("vcenter")),Item("Abajo",null,()=>AlignTo("bottom"))},true));
   layout.Controls.Add(DropIcon("","Girar y voltear la imagen",()=>new ToolStripItem[]{Item("Girar 90° a la derecha",null,()=>TransformImage(RotateFlipType.Rotate90FlipNone)),Item("Girar 90° a la izquierda",null,()=>TransformImage(RotateFlipType.Rotate270FlipNone)),Item("Girar 180°",null,()=>TransformImage(RotateFlipType.Rotate180FlipNone)),new ToolStripSeparator(),Item("Voltear horizontalmente",null,()=>TransformImage(RotateFlipType.RotateNoneFlipX)),Item("Voltear verticalmente",null,()=>TransformImage(RotateFlipType.RotateNoneFlipY)),new ToolStripSeparator(),Item("Reemplazar imagen…",null,ReplaceImage)},false));
   layout.Controls.Add(DropIcon("","Orden: delante o detrás",()=>new ToolStripItem[]{Item("Traer al frente",null,()=>Reorder(2)),Item("Traer adelante",null,()=>Reorder(1)),Item("Enviar atrás",null,()=>Reorder(-1)),Item("Enviar al fondo",null,()=>Reorder(-2))},true));
   layout.Controls.Add(Look.Icon("","Duplicar (Ctrl+D)",Duplicate,w,bk,hv,40,(g,r,c)=>{using(var p=new Pen(c,1.6f)){g.DrawRectangle(p,r.X+1,r.Y+1,12,12);g.DrawRectangle(p,r.X+6,r.Y+6,12,12);}}));
   // Impresora e Imprimir a la derecha.
   var right=new FlowLayoutPanel{Dock=DockStyle.Right,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,BackColor=Look.Teal,Padding=new Padding(0,6,0,0)};
   printerChip.Margin=new Padding(0,0,8,0);right.Controls.Add(printerChip);
   // Número de etiquetas que imprime el botón Imprimir.
   var copiesPanel=new Panel{Width=72,Height=48,BackColor=Look.Teal,Margin=new Padding(0,0,8,0)};
   copiesPanel.Controls.Add(new System.Windows.Forms.Label{Text="Copias",ForeColor=Color.FromArgb(205,230,234),Font=new Font("Segoe UI",8),AutoSize=false,Width=72,Height=16,Location=new Point(0,0),TextAlign=ContentAlignment.MiddleCenter});
   copiesBox.Location=new Point(7,18);copiesPanel.Controls.Add(copiesBox);right.Controls.Add(copiesPanel);
   Look.Tips.SetToolTip(copiesBox,"Número de etiquetas que se imprimirán al pulsar Imprimir");
   printerChip.Width=280;Shown+=(s3,e3)=>{if(copiesBox.ContainsFocus)canvas.Focus();};
   if(printButton!=null){printButton.BackColor=Color.White;printButton.ForeColor=Look.Teal;printButton.FlatStyle=FlatStyle.Flat;printButton.FlatAppearance.BorderSize=0;printButton.Height=48;right.Controls.Add(printButton);Look.Tips.SetToolTip(printButton,"Imprimir ya en la impresora elegida (Ctrl+P)");}
   // Al lado de Imprimir: la ventana completa con vista previa, impresora, copias y preferencias.
   var options=new Button{Text="Vista previa\ny opciones",Width=112,Height=48,FlatStyle=FlatStyle.Flat,BackColor=Look.Teal,ForeColor=Color.White,Font=new Font("Segoe UI",9.5f),Margin=new Padding(8,0,0,0),Cursor=Cursors.Hand,AccessibleName="Vista previa y más opciones de impresión",AccessibleRole=AccessibleRole.PushButton};
   options.FlatAppearance.BorderColor=Color.White;options.FlatAppearance.BorderSize=1;options.FlatAppearance.MouseOverBackColor=Look.TealHover;options.Click+=(s2,e2)=>PrintOptions();right.Controls.Add(options);
   Look.Tips.SetToolTip(options,"Ver la vista previa y elegir impresora, copias y preferencias (Ctrl+Mayús+P)");
   topBar.Controls.Add(groups);topBar.Controls.Add(right);

   // ---- Barra de formato (cambia según lo seleccionado, como en Word) ----
   formatBar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=104,WrapContents=false,BackColor=Color.White,Padding=new Padding(8,2,8,0),AutoScroll=true};
   if(ribbon!=null){var home=ribbon.TabPages[0].Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
    if(home!=null)foreach(var g in home.Controls.OfType<Panel>().ToArray()){var t=g.Controls.OfType<System.Windows.Forms.Label>().Select(c=>c.Text).ToList();if(t.Contains("Texto")||t.Contains("Alineación del texto")||t.Contains("Imagen")){g.BackColor=Color.White;foreach(Control c in g.Controls)if(c is Button||c is CheckBox)c.BackColor=Color.White;formatBar.Controls.Add(g);}}}
   // Sin selección: ajustes de la etiqueta, como el panel de cinta de los editores de etiquetas.
   var sizeGroup=RibbonUi.Group(formatBar,"Tamaño de la etiqueta",470);sizeGroup.Name="sizeGroup";sizeGroup.BackColor=Color.White;
   presets.Width=250;RibbonUi.Place(sizeGroup,presets,6,10,250,26);
   foreach(var n in new[]{width,height})n.Width=62;
   RibbonUi.Place(sizeGroup,new System.Windows.Forms.Label{Text="Ancho",TextAlign=ContentAlignment.MiddleLeft},264,10,44,26);RibbonUi.Place(sizeGroup,width,310,12,62,24);
   RibbonUi.Place(sizeGroup,new System.Windows.Forms.Label{Text="Alto",TextAlign=ContentAlignment.MiddleLeft},264,44,44,26);RibbonUi.Place(sizeGroup,height,310,46,62,24);
   RibbonUi.Place(sizeGroup,new System.Windows.Forms.Label{Text="mm",TextAlign=ContentAlignment.MiddleLeft},376,28,30,26);
   var apply=new Button{Text="Aplicar",FlatStyle=FlatStyle.Flat,BackColor=Look.Teal,ForeColor=Color.White};apply.FlatAppearance.BorderSize=0;apply.Click+=(s2,e2)=>ApplySize();RibbonUi.Place(sizeGroup,apply,404,26,60,28);RibbonUi.Tip(apply,"Aplicar ancho y alto personalizados");
   var gallery=new Button{Text="Ver todas las plantillas…",FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Look.Teal};gallery.FlatAppearance.BorderColor=Look.TealLine;gallery.Click+=(s2,e2)=>ShowGallery();RibbonUi.Place(sizeGroup,gallery,6,44,250,28);
   var hint=new System.Windows.Forms.Label{Text="Selecciona un elemento de la etiqueta para cambiar su formato.\nDoble clic en un texto para escribir · Clic derecho para más opciones.",ForeColor=Color.FromArgb(90,110,115),AutoSize=true,Margin=new Padding(16,26,0,0),Font=new Font("Segoe UI",9.5f)};
   formatBar.Controls.Add(hint);hint.Name="formatHint";
   var line=new Panel{Dock=DockStyle.Top,Height=1,BackColor=Look.TealLine};

   // ---- Herramientas a la izquierda ----
   toolStripLeft=new Panel{Dock=DockStyle.Left,Width=52,BackColor=Color.White,Padding=new Padding(4,8,4,4)};
   var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White};
   Color tf=Look.Teal,tb=Color.White,th=Look.TealSoft;
   tools.Controls.Add(Look.Icon("","Seleccionar (Esc quita la selección)",()=>{canvas.Selected=null;RefreshAll();canvas.Focus();},tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir texto",()=>Add("Texto"),tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir imagen o logotipo",()=>Add("Imagen"),tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir código de barras",()=>{Add("Código 39");EditText();},tf,tb,th,42,Look.Barcode));
   tools.Controls.Add(Look.Icon("","Añadir código QR",()=>Add("QR"),tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir emoji o símbolo",AddEmoji,tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir marco",()=>Add("Marco"),tf,tb,th,42));
   tools.Controls.Add(Look.Icon("","Añadir línea",()=>Add("Línea"),tf,tb,th,42,(g,r,c)=>{using(var p=new Pen(c,2f))g.DrawLine(p,r.X+1,r.Bottom-2,r.Right-1,r.Y+2);}));
   tools.Controls.Add(new Panel{Height=1,Width=40,BackColor=Look.TealLine,Margin=new Padding(1,6,1,6)});
   tools.Controls.Add(Look.Icon("","Plantillas y tamaños",ShowGallery,tf,tb,th,42));
   toolStripLeft.Controls.Add(tools);toolStripLeft.Paint+=(s,e)=>{using(var p=new Pen(Look.TealLine))e.Graphics.DrawLine(p,toolStripLeft.Width-1,0,toolStripLeft.Width-1,toolStripLeft.Height);};

   // ---- Quitar la interfaz anterior y montar la nueva ----
   foreach(var c in new Control[]{ribbon,oldActions,leftTools,steps}){if(c!=null){Controls.Remove(c);c.Visible=false;}}
   welcomeDone=true;welcome.Wanted=false;welcome.Visible=false;
   var split=Controls.OfType<SplitContainer>().FirstOrDefault();
   Controls.Add(toolStripLeft);Controls.Add(line);Controls.Add(formatBar);Controls.Add(topBar);
   // Orden de acoplado: menú arriba del todo, luego barra verde, barra de formato, línea; el lienzo ocupa el resto.
   if(MainMenuStrip!=null)MainMenuStrip.SendToBack();
   if(split!=null)split.BringToFront();
   if(bar!=null){bar.BackColor=Look.TealSoft;bar.SendToBack();}
   canvas.BackColor=Look.Canvas;
   props.CategoryForeColor=Look.Teal;props.LineColor=Look.TealSoft;
   foreach(var caption in items.Parent.Controls.OfType<System.Windows.Forms.Label>().Concat(props.Parent.Controls.OfType<System.Windows.Forms.Label>())){caption.BackColor=Look.Teal;caption.ForeColor=Color.White;caption.Font=new Font("Segoe UI",9,FontStyle.Bold);}
   items.BorderStyle=BorderStyle.None;
   ResumeLayout(true);
   Shown+=(s,e)=>{if(label.Elements.Count==0&&String.IsNullOrEmpty(file)&&!Environment.GetCommandLineArgs().Contains("--render-ui"))BeginInvoke((Action)ShowGallery);};
  }

  // Menú desplegable desde un botón de la barra superior. needsSelection: solo con algo seleccionado.
  Button DropIcon(string glyph,string tip,Func<ToolStripItem[]> build,bool any){
   Button b=null;b=Look.Icon(glyph,tip+" ▾",()=>{var e=canvas.Selected;if(e==null||(!any&&e.Kind!="Imagen")){status.Text=any?"Selecciona primero un elemento.":"Selecciona primero una imagen.";return;}var m=new ContextMenuStrip{Font=new Font("Segoe UI",9.5f)};m.Items.AddRange(build());m.Show(b,new Point(0,b.Height));},Color.White,Look.Teal,Look.TealHover);
   return b;
  }
  void SyncShell(Element e){if(formatBar==null)return;foreach(Control g in formatBar.Controls){if(g.Name=="formatHint"||g.Name=="sizeGroup")g.Visible=e==null;else if(g is Panel&&e==null)g.Visible=false;}if(e!=null)SyncRibbonShell(e);}
  void SyncRibbonShell(Element e){bool image=e.Kind=="Imagen",text=e.Kind=="Texto";foreach(Control g in formatBar.Controls){if(!(g is Panel)||g.Name=="sizeGroup")continue;var t=g.Controls.OfType<System.Windows.Forms.Label>().Select(c=>c.Text).ToList();g.Visible=t.Contains("Imagen")?image:text;}}

  void NewFromGallery(){if(!Confirm())return;dirty=false;New();ShowGallery();}
  // Pantalla Nuevo / Abrir con los tamaños agrupados como plantillas.
  void ShowGallery(){
   using(var d=new GalleryForm(Formats.All.Where(f=>
#if SENCILLA
    !f.Dymo&&
#endif
    true).ToArray())){
    var r=d.ShowDialog(this);
    if(r!=DialogResult.OK)return;
    if(d.Action=="open"){Open();return;}
    if(d.Action=="example"){LoadExample();return;}
    if(d.Action=="catalog"){ShowCatalog();return;}
    var f=d.Chosen;if(f==null)return;
    if(label.Elements.Count>0&&dirty&&MessageBox.Show(this,"¿Cambiar el tamaño de la etiqueta actual a «"+f.Name+"»? Los elementos se mantienen; puedes deshacerlo con Ctrl+Z.","Tamaño de etiqueta",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
    Snapshot();label.Media=f.Dymo?"DYMO LT12":f.Paper?"PAPER":"";
    if(f.W>0){label.Width=f.W;label.Height=f.H;}else if(f.Dymo){label.Width=60;label.Height=12;}else{width.Focus();}
    canvas.Selected=null;RefreshAll();if(label.Elements.Count>0)Mark();
    status.Text="Etiqueta de "+label.Width+" × "+label.Height+" mm. Añade contenido con las herramientas de la izquierda.";
   }
  }
 }

 public class GalleryForm:Form {
  public LabelFormat Chosen;public string Action="";
  public GalleryForm(LabelFormat[] formats){
   Text="Nuevo / Abrir · "+AppVersion.Name;Icon=Branding.AppIcon;ClientSize=new Size(920,600);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;Font=new Font("Segoe UI",9.5f);BackColor=Color.White;KeyPreview=true;
   KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)Close();};
   var nav=new Panel{Dock=DockStyle.Left,Width=200,BackColor=Look.Teal,Padding=new Padding(10,16,10,10)};
   var navFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Look.Teal};
   Func<string,string,bool,Action,Button> navButton=(glyph,text,active,act)=>{var b=new Button{Text="  "+text,TextAlign=ContentAlignment.MiddleLeft,Width=178,Height=46,FlatStyle=FlatStyle.Flat,ForeColor=active?Look.Teal:Color.White,BackColor=active?Color.White:Look.TealDark,Font=new Font("Segoe UI",11),Margin=new Padding(0,0,0,8),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=active?Color.White:Look.TealHover;b.Click+=(s,e)=>act();return b;};
   navFlow.Controls.Add(navButton("","Nueva etiqueta",true,()=>{}));
   navFlow.Controls.Add(navButton("","Abrir diseño…",false,()=>{Action="open";DialogResult=DialogResult.OK;}));
   navFlow.Controls.Add(navButton("","Cargar un ejemplo",false,()=>{Action="example";DialogResult=DialogResult.OK;}));
   navFlow.Controls.Add(navButton("","Catálogo de etiquetas",false,()=>{Action="catalog";DialogResult=DialogResult.OK;}));
   nav.Controls.Add(navFlow);
   var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(22,14,10,10),BackColor=Color.White};
   var title=new System.Windows.Forms.Label{Text="Nueva etiqueta",Dock=DockStyle.Top,Height=40,Font=new Font("Segoe UI",17),ForeColor=Color.FromArgb(30,45,48)};
   var sub=new System.Windows.Forms.Label{Text="Elige el tamaño del rollo, la cinta o el papel. Podrás cambiarlo después en la barra superior.",Dock=DockStyle.Top,Height=26,ForeColor=Color.FromArgb(90,110,115)};
   var list=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White,Padding=new Padding(0,4,0,10)};
   AddSection(list,"Etiquetas de rollo",formats.Where(f=>!f.Dymo&&!f.Paper&&f.W>0));
   AddSection(list,"Cinta DYMO LetraTag (12 mm)",formats.Where(f=>f.Dymo));
   AddSection(list,"Papel A5 · A4 · A3",formats.Where(f=>f.Paper));
   AddSection(list,"Otra medida",formats.Where(f=>!f.Dymo&&!f.Paper&&f.W==0));
   body.Controls.Add(list);body.Controls.Add(sub);body.Controls.Add(title);
   Controls.Add(body);Controls.Add(nav);
  }
  void AddSection(FlowLayoutPanel list,string title,IEnumerable<LabelFormat> formats){
   var items=formats.ToArray();if(items.Length==0)return;
   var head=new System.Windows.Forms.Label{Text=title,Width=660,Height=34,Font=new Font("Segoe UI",12),ForeColor=Look.Teal,TextAlign=ContentAlignment.BottomLeft,Margin=new Padding(0,8,0,6)};
   head.Paint+=(s,e)=>{using(var p=new Pen(Look.TealLine))e.Graphics.DrawLine(p,0,head.Height-1,head.Width,head.Height-1);};
   list.Controls.Add(head);list.SetFlowBreak(head,true);
   Control last=null;foreach(var f in items){last=Tile(f);list.Controls.Add(last);}
   if(last!=null)list.SetFlowBreak(last,true);
  }
  Control Tile(LabelFormat f){
   var b=new Button{Width=152,Height=128,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,10,10),Cursor=Cursors.Hand,AccessibleName="Etiqueta "+f.Name,Text=""};
   b.FlatAppearance.BorderColor=Look.TealLine;b.FlatAppearance.MouseOverBackColor=Look.TealSoft;
   b.Paint+=(s,e)=>{var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
    var area=new RectangleF(16,10,b.Width-32,64);float w=f.W>0?f.W:(f.Dymo?60:100),h=f.H>0?f.H:60;float k=Math.Min(area.Width/w,area.Height/h);var r=new RectangleF(area.X+(area.Width-w*k)/2,area.Y+(area.Height-h*k)/2,w*k,h*k);
    using(var fill=new SolidBrush(Look.Teal))g.FillRectangle(fill,r);
    using(var paper=new SolidBrush(Color.White)){var inner=RectangleF.Inflate(r,-Math.Min(4,r.Width/6),-Math.Min(4,r.Height/4));g.FillRectangle(paper,inner);
     using(var tf=new Font("Segoe UI",Math.Max(6,Math.Min(10,inner.Height/2.2f)),FontStyle.Bold))using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(f.W>0?"ABC":"✎",tf,new SolidBrush(Look.TealDark),inner,sf);}
    string name=f.Name;int dot=name.IndexOf(" · ");string top=dot>0?name.Substring(0,dot):name,bottom=dot>0?name.Substring(dot+3):"";
    using(var sf=new StringFormat{Alignment=StringAlignment.Center}){g.DrawString(top,b.Font,Brushes.Black,new RectangleF(4,80,b.Width-8,20),sf);using(var small=new Font("Segoe UI",8))g.DrawString(bottom,small,Brushes.DimGray,new RectangleF(4,99,b.Width-8,28),sf);}};
   b.Click+=(s,e)=>{Chosen=f;DialogResult=DialogResult.OK;};Look.Tips.SetToolTip(b,f.Name);return b;
  }
 }
}
