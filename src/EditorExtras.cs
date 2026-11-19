using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Nota: csc de .NET Framework compila C# 5 (sin interpolación ni operador ?.).
namespace EtiquetaLocal {
 // Operaciones sobre imágenes. Modifican los propios píxeles (PNG sin pérdida), así la vista previa,
 // el envío ZPL, el controlador de Windows y la DYMO imprimen exactamente lo mismo, y los diseños
 // siguen abriéndose en versiones anteriores.
 public static class ImageTools {
  public const string Filter="Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
  static Bitmap Decode(Element e){using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var im=Image.FromStream(ms))return new Bitmap(im);}
  static void Store(Element e,Bitmap b){using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);string data=Convert.ToBase64String(ms.ToArray());if(data.Length>14000000)throw new Exception("La imagen resultante es demasiado grande.");e.ImageData=data;}}
  public static byte[] ReadFile(string path){
   var bytes=File.ReadAllBytes(path);if(bytes.Length>10000000)throw new Exception("Máximo 10 MB por imagen.");
   using(var ms=new MemoryStream(bytes))using(var im=Image.FromStream(ms)){if(im.Width>12000||im.Height>12000)throw new Exception("Máximo 12000 píxeles por lado.");}
   return bytes;
  }
  public static byte[] FromClipboard(){var im=Clipboard.GetImage();if(im==null)return null;using(im)using(var b=new Bitmap(im))using(var ms=new MemoryStream()){if(b.Width>12000||b.Height>12000)throw new Exception("Máximo 12000 píxeles por lado.");b.Save(ms,ImageFormat.Png);return ms.ToArray();}}
  public static bool IsImageFile(string path){string ext=(Path.GetExtension(path)??"").ToLowerInvariant();return ext==".png"||ext==".jpg"||ext==".jpeg"||ext==".bmp"||ext==".gif";}
  // Voltea o gira. Al girar 90°, el recuadro también gira sobre su centro.
  public static void Transform(Element e,Label l,RotateFlipType t){
   if(e==null||e.Kind!="Imagen"||String.IsNullOrEmpty(e.ImageData))return;
   using(var b=Decode(e)){b.RotateFlip(t);Store(e,b);}
   if(t==RotateFlipType.Rotate90FlipNone||t==RotateFlipType.Rotate270FlipNone){float cx=e.X+e.W/2,cy=e.Y+e.H/2,w=e.W;e.W=e.H;e.H=w;e.X=cx-e.W/2;e.Y=cy-e.H/2;}
   Editing.RestoreImageRatio(e);if(e.LockAspect)Editing.FitImage(e,l);
  }
  // Cambia la imagen conservando el sitio: la nueva se encaja dentro del recuadro actual, centrada.
  public static void Replace(Element e,Label l,byte[] bytes,string name){
   float cx=e.X+e.W/2,cy=e.Y+e.H/2,boxW=e.W,boxH=e.H;
   e.ImageData=Convert.ToBase64String(bytes);if(name!=null)e.Text=name;Editing.RestoreImageRatio(e);e.LockAspect=true;
   float w=boxW,h=w/e.ImageAspect;if(h>boxH){h=boxH;w=h*e.ImageAspect;}
   e.W=Math.Max(.5f,w);e.H=Math.Max(.5f,h);e.X=cx-e.W/2;e.Y=cy-e.H/2;Editing.FitImage(e,l);
  }
 }

 // Nitidez al imprimir por controlador: la etiqueta llega ya en blanco y negro puro a la resolución
 // exacta de la impresora, así el controlador no la suaviza ni la trama a su manera.
 public class ImageModeConverter:System.ComponentModel.StringConverter {
  public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext c){return true;}
  public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext c){return true;}
  public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext c){return new StandardValuesCollection(Sharpness.Modes);}
  public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext c,System.Globalization.CultureInfo culture,object value,Type t){if(t==typeof(string)&&(value==null||(string)value==""))return "Automática";return base.ConvertTo(c,culture,value,t);}
 }
 public static class Sharpness {
  public static readonly string[] Modes={"Automática","Nítida","Fotografía"};
  // Copia del diseño con cada imagen ya convertida a puntos blancos y negros de su tamaño final.
  public static Label Prepare(Label label,int dpi){
   var copy=Model.Load(Model.Save(label));
   foreach(var e in copy.Elements)if(e.Kind=="Imagen"&&!String.IsNullOrEmpty(e.ImageData))e.ImageData=ToDots(e,dpi);
   return copy;
  }
  static string ToDots(Element e,int dpi){
   int w=Math.Max(1,(int)Math.Round(e.W*dpi/25.4)),h=Math.Max(1,(int)Math.Round(e.H*dpi/25.4));
   if((long)w*h>40000000)return e.ImageData;
   using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var src=Image.FromStream(ms))using(var b=new Bitmap(w,h,PixelFormat.Format24bppRgb)){
    using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
     using(var at=new ImageAttributes()){at.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);g.DrawImage(src,new Rectangle(0,0,w,h),0,0,src.Width,src.Height,GraphicsUnit.Pixel,at);}}
    var lum=Luminance(b);bool photo=e.ImageMode=="Fotografía"||(e.ImageMode!="Nítida"&&LooksLikePhoto(lum));
    if(photo)Dither(lum,w,h);else Threshold(lum,Otsu(lum));
    Write(b,lum);using(var o=new MemoryStream()){b.Save(o,ImageFormat.Png);return Convert.ToBase64String(o.ToArray());}
   }
  }
  static float[] Luminance(Bitmap b){var r=new Rectangle(0,0,b.Width,b.Height);var d=b.LockBits(r,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);try{var bytes=new byte[d.Stride*b.Height];System.Runtime.InteropServices.Marshal.Copy(d.Scan0,bytes,0,bytes.Length);var l=new float[b.Width*b.Height];for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){int i=y*d.Stride+x*3;l[y*b.Width+x]=.114f*bytes[i]+.587f*bytes[i+1]+.299f*bytes[i+2];}return l;}finally{b.UnlockBits(d);}}
  static void Write(Bitmap b,float[] l){var r=new Rectangle(0,0,b.Width,b.Height);var d=b.LockBits(r,ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);try{var bytes=new byte[d.Stride*b.Height];for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){byte v=l[y*b.Width+x]<128?(byte)0:(byte)255;int i=y*d.Stride+x*3;bytes[i]=bytes[i+1]=bytes[i+2]=v;}System.Runtime.InteropServices.Marshal.Copy(bytes,0,d.Scan0,bytes.Length);}finally{b.UnlockBits(d);}}
  // Un logotipo tiene casi todo claro u oscuro; una foto, muchos tonos intermedios.
  public static bool LooksLikePhoto(float[] l){if(l.Length==0)return false;int mid=0;foreach(var v in l)if(v>45&&v<210)mid++;return mid>l.Length*.22;}
  // Umbral de Otsu: separa tinta y fondo aunque el logotipo sea gris o esté desvaído.
  static float Otsu(float[] l){var hist=new int[256];foreach(var v in l)hist[Math.Max(0,Math.Min(255,(int)v))]++;double total=l.Length,sum=0;for(int i=0;i<256;i++)sum+=i*(double)hist[i];double sumB=0,wB=0,best=-1;int t=128;for(int i=0;i<256;i++){wB+=hist[i];if(wB==0)continue;double wF=total-wB;if(wF==0)break;sumB+=i*(double)hist[i];double mB=sumB/wB,mF=(sum-sumB)/wF,between=wB*wF*(mB-mF)*(mB-mF);if(between>best){best=between;t=i;}}
   // Imagen casi uniforme: no inventar tinta.
   if(best<=0)return 128;return Math.Max(40,Math.Min(215,t+.5f));}
  static void Threshold(float[] l,float t){for(int i=0;i<l.Length;i++)l[i]=l[i]<t?0:255;}
  // Floyd–Steinberg: reparte el error para conservar los grises de una fotografía.
  static void Dither(float[] l,int w,int h){for(int y=0;y<h;y++)for(int x=0;x<w;x++){int i=y*w+x;float old=l[i],nv=old<128?0:255,err=old-nv;l[i]=nv;
   if(x+1<w)l[i+1]+=err*7/16;if(y+1<h){if(x>0)l[i+w-1]+=err*3/16;l[i+w]+=err*5/16;if(x+1<w)l[i+w+1]+=err/16;}}}
  // Último paso: todo el raster en blanco o negro puro (texto, barras, líneas e imágenes).
  public static void Binarize(Bitmap b){var lum=Luminance(b);Write(b,lum);}
 }

 // Pruebas de --self-test para voltear, girar y reemplazar.
 public static class ImageToolsVerification {
  static string Png(Bitmap b){using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);return Convert.ToBase64String(ms.ToArray());}}
  static Color Pixel(Element e,int x,int y){using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var b=new Bitmap(ms))return b.GetPixel(x,y);}
  public static void Run(){
   // Imagen 4x2: negra salvo el píxel (0,0) blanco.
   string data;using(var b=new Bitmap(4,2)){using(var g=Graphics.FromImage(b))g.Clear(Color.Black);b.SetPixel(0,0,Color.White);data=Png(b);}
   var l=new Label{Width=100,Height=50};var e=new Element{Kind="Imagen",ImageData=data,X=10,Y=15,W=40,H=20,LockAspect=true};Editing.RestoreImageRatio(e);l.Elements.Add(e);
   ImageTools.Transform(e,l,RotateFlipType.RotateNoneFlipX);if(Pixel(e,3,0).R!=255||Pixel(e,0,0).R!=0)throw new Exception("Voltear horizontal incorrecto.");
   ImageTools.Transform(e,l,RotateFlipType.RotateNoneFlipY);if(Pixel(e,3,1).R!=255)throw new Exception("Voltear vertical incorrecto.");
   float cx=e.X+e.W/2,cy=e.Y+e.H/2;ImageTools.Transform(e,l,RotateFlipType.Rotate90FlipNone);
   if(Math.Abs(e.W-20)>.01||Math.Abs(e.H-40)>.01||Math.Abs(e.ImageAspect-.5f)>.001||Math.Abs(e.X+e.W/2-cx)>.01||Math.Abs(e.Y+e.H/2-cy)>.01)throw new Exception("Girar 90° incorrecto.");
   string square;using(var b=new Bitmap(10,10)){using(var g=Graphics.FromImage(b))g.Clear(Color.Black);square=Png(b);}
   ImageTools.Replace(e,l,Convert.FromBase64String(square),"cuadrado.png");
   if(Math.Abs(e.W-e.H)>.01||e.W>20.01||e.Text!="cuadrado.png"||Math.Abs(e.X+e.W/2-cx)>.01)throw new Exception("Reemplazar imagen incorrecto.");
   Model.Validate(l);Model.Load(Model.Save(l));
  }
 }

 public partial class MainForm {
  const string ClipFormat="Impresorapp.Elemento";
  ContextMenuStrip contextMenu=new ContextMenuStrip();

  // Se llama al final del constructor.
  void InitExtras(){
   contextMenu.Font=new Font("Segoe UI",9.5f);contextMenu.ShowImageMargin=true;contextMenu.ShowCheckMargin=false;
   canvas.MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Right)ShowContextMenu(canvas,e.Location);};
   items.MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Right)return;int i=items.IndexFromPoint(e.Location);if(i>=0){items.SelectedIndex=i;ShowContextMenu(items,e.Location);}};
   canvas.AllowDrop=true;
   canvas.DragEnter+=(s,e)=>{e.Effect=DroppedImage(e.Data)!=null?DragDropEffects.Copy:DragDropEffects.None;};
   canvas.DragDrop+=(s,e)=>{string path=DroppedImage(e.Data);if(path!=null)DropImage(path,canvas.PointToClient(new Point(e.X,e.Y)));};
   AddImageRibbonButtons();
   ApplyShell();
  }

  static string DroppedImage(IDataObject data){var files=data.GetData(DataFormats.FileDrop) as string[];return files!=null&&files.Length==1&&ImageTools.IsImageFile(files[0])?files[0]:null;}
  // Soltar una imagen sobre otra la reemplaza; en una zona libre, la añade en ese punto.
  void DropImage(string path,Point where){
   try{var bytes=ImageTools.ReadFile(path);var target=canvas.ElementAt(where);
    if(target!=null&&target.Kind=="Imagen"){Snapshot();ImageTools.Replace(target,label,bytes,Path.GetFileName(path));canvas.Selected=target;RefreshAll();Mark();return;}
    AddImageBytes(bytes,Path.GetFileName(path),canvas.ToLabel(where));
   }catch(Exception ex){MessageBox.Show(this,ex.Message,"Imagen");}
  }
  void AddImageBytes(byte[] bytes,string name,PointF? at){
   if(label.Elements.Count>=500){MessageBox.Show(this,"Máximo 500 elementos por plantilla.");return;}
   var el=new Element{Kind="Imagen",ImageData=Convert.ToBase64String(bytes),Text=name,LockAspect=true};Editing.RestoreImageRatio(el);
   el.W=Math.Min(35,label.Width*.8f);el.H=el.W/el.ImageAspect;if(el.H>label.Height*.8f){el.H=label.Height*.8f;el.W=el.H*el.ImageAspect;}
   if(at.HasValue){el.X=at.Value.X-el.W/2;el.Y=at.Value.Y-el.H/2;}else{el.X=Math.Min(3,label.Width/10);el.Y=Math.Min(3,label.Height/10);}
   Editing.FitImage(el,label);if(label.Media=="DYMO LT12")Editing.CenterOnTape(el,label);
   Snapshot();label.Elements.Add(el);canvas.Selected=el;RefreshAll();Mark();
  }

  // ---- Acciones de imagen ----
  void TransformImage(RotateFlipType t){var e=canvas.Selected;if(e==null||e.Kind!="Imagen")return;Snapshot();try{ImageTools.Transform(e,label,t);}catch(Exception ex){Undo();MessageBox.Show(this,ex.Message,"Imagen");return;}RefreshAll();Mark();}
  void ReplaceImage(){var e=canvas.Selected;if(e==null||e.Kind!="Imagen")return;
   using(var d=new OpenFileDialog{Title="Reemplazar imagen",Filter=ImageTools.Filter}){if(d.ShowDialog(this)!=DialogResult.OK)return;
    try{var bytes=ImageTools.ReadFile(d.FileName);Snapshot();ImageTools.Replace(e,label,bytes,Path.GetFileName(d.FileName));RefreshAll();Mark();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Imagen");}}}
  void SetImageMode(string mode){var e=canvas.Selected;if(e==null||e.Kind!="Imagen")return;Snapshot();e.ImageMode=mode==""?null:mode;RefreshAll();Mark();}
  void RestoreImageShape(){var e=canvas.Selected;if(e==null||e.Kind!="Imagen")return;Snapshot();e.LockAspect=true;Editing.RestoreImageRatio(e);e.H=e.W/e.ImageAspect;Editing.FitImage(e,label);RefreshAll();Mark();}
  void FillLabelWithImage(){var e=canvas.Selected;if(e==null||e.Kind!="Imagen")return;Snapshot();e.LockAspect=true;Editing.RestoreImageRatio(e);float m=1,w=label.Width-2*m,h=w/e.ImageAspect;if(h>label.Height-2*m){h=label.Height-2*m;w=h*e.ImageAspect;}e.W=w;e.H=h;e.X=(label.Width-w)/2;e.Y=(label.Height-h)/2;Editing.FitImage(e,label);RefreshAll();Mark();}

  // ---- Texto ----
  void ToggleStyle(string which){var e=canvas.Selected;if(e==null||e.Kind!="Texto")return;Snapshot();if(which=="b")e.Bold=!e.Bold;else if(which=="i")e.Italic=!e.Italic;else e.Underline=!e.Underline;Editing.FitText(e,label);RefreshAll();Mark();}
  void SetAlign(string a){var e=canvas.Selected;if(e==null||e.Kind!="Texto")return;Snapshot();e.TextAlign=a;e.W=Math.Max(e.W,label.Width-e.X-.5f);Editing.FitText(e,label);RefreshAll();Mark();}

  // ---- Portapapeles ----
  void CopySelected(bool cut){var e=canvas.Selected;if(e==null)return;
   try{var data=new DataObject();data.SetData(ClipFormat,Model.Save(new Label{Elements=new List<Element>{e}}));if(e.Kind=="Texto"&&!String.IsNullOrEmpty(e.Text))data.SetText(e.Text);Clipboard.SetDataObject(data,true);}catch(Exception ex){MessageBox.Show(this,"No se pudo copiar: "+ex.Message,"Copiar");return;}
   if(cut)Delete();status.Text=cut?"Elemento cortado. Ctrl+V para pegarlo.":"Elemento copiado. Ctrl+V para pegarlo.";}
  bool CanPaste(){try{return Clipboard.ContainsData(ClipFormat)||Clipboard.ContainsImage()||Clipboard.ContainsText();}catch{return false;}}
  // Pega un elemento de Impresorapp, una imagen (captura, logotipo copiado…) o un texto.
  void Paste(PointF? at){
   try{
    if(Clipboard.ContainsData(ClipFormat)){var copy=Model.Load((string)Clipboard.GetData(ClipFormat)).Elements[0];if(label.Elements.Count>=500)return;Snapshot();
     if(at.HasValue){copy.X=at.Value.X;copy.Y=at.Value.Y;}else{copy.X+=2;copy.Y+=2;}
     if(copy.Kind=="Imagen")Editing.FitImage(copy,label);label.Elements.Add(copy);canvas.Selected=copy;RefreshAll();Mark();return;}
    if(Clipboard.ContainsImage()){var bytes=ImageTools.FromClipboard();if(bytes!=null){var sel=canvas.Selected;if(sel!=null&&sel.Kind=="Imagen"&&!at.HasValue&&MessageBox.Show(this,"¿Reemplazar la imagen seleccionada por la del portapapeles?\n\nSí: reemplazar · No: añadirla como imagen nueva","Pegar imagen",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){Snapshot();ImageTools.Replace(sel,label,bytes,"Imagen pegada");RefreshAll();Mark();}else AddImageBytes(bytes,"Imagen pegada",at);}return;}
    if(Clipboard.ContainsText()){string text=Clipboard.GetText();if(String.IsNullOrWhiteSpace(text))return;if(text.Length>4000)text=text.Substring(0,4000);
     var el=new Element{Kind="Texto",Text=text,FontSize=label.Media=="DYMO LT12"?Editing.DymoFontSize():12,X=at.HasValue?at.Value.X:2,Y=at.HasValue?at.Value.Y:2};Editing.FitText(el,label);Snapshot();label.Elements.Add(el);canvas.Selected=el;RefreshAll();Mark();}
   }catch(Exception ex){MessageBox.Show(this,"No se pudo pegar: "+ex.Message,"Pegar");}
  }

  // ---- Orden y alineación ----
  void Reorder(int mode){var e=canvas.Selected;if(e==null)return;int i=label.Elements.IndexOf(e),n=label.Elements.Count;int to=mode==2?n-1:mode==1?Math.Min(n-1,i+1):mode==-1?Math.Max(0,i-1):0;if(to==i)return;Snapshot();label.Elements.RemoveAt(i);label.Elements.Insert(to,e);RefreshAll();Mark();}
  void AlignTo(string where){var e=canvas.Selected;if(e==null)return;Snapshot();
   if(where=="left")e.X=0;else if(where=="right")e.X=Math.Max(0,label.Width-e.W);else if(where=="hcenter")e.X=Math.Max(0,(label.Width-e.W)/2);
   else if(where=="top")e.Y=0;else if(where=="bottom")e.Y=Math.Max(0,label.Height-e.H);else if(where=="vcenter")e.Y=Math.Max(0,(label.Height-e.H)/2);
   else if(where=="center"){e.X=Math.Max(0,(label.Width-e.W)/2);e.Y=Math.Max(0,(label.Height-e.H)/2);}
   RefreshAll();Mark();}
  void Nudge(float dx,float dy){var e=canvas.Selected;if(e==null)return;if(!nudging){Snapshot();nudging=true;}e.X=(float)Math.Round(Math.Max(-1000,Math.Min(2000,e.X+dx)),1);e.Y=(float)Math.Round(Math.Max(-1000,Math.Min(2000,e.Y+dy)),1);props.Refresh();canvas.Invalidate();Mark();}
  bool nudging;

  // Atajos extra. Devuelve true si la tecla se ha usado.
  bool HandleExtraKeys(KeyEventArgs e){
   if(e.Control&&e.KeyCode==Keys.Y&&!props.ContainsFocus){Redo();return true;}
   if(e.Control&&e.KeyCode==Keys.N){NewFromGallery();return true;}
   bool design=canvas.Focused||items.Focused;if(!design)return false;
   if(e.KeyCode!=Keys.Left&&e.KeyCode!=Keys.Right&&e.KeyCode!=Keys.Up&&e.KeyCode!=Keys.Down&&nudging){nudging=false;RefreshItems();}
   if(e.Control&&e.KeyCode==Keys.C){CopySelected(false);return true;}
   if(e.Control&&e.KeyCode==Keys.X){CopySelected(true);return true;}
   if(e.Control&&e.KeyCode==Keys.V){Paste(null);return true;}
   if(e.Control&&e.KeyCode==Keys.D){Duplicate();return true;}
   if(!canvas.Focused||canvas.Selected==null)return false;
   float step=e.Shift?5f:.5f;
   if(e.KeyCode==Keys.Left){Nudge(-step,0);return true;}if(e.KeyCode==Keys.Right){Nudge(step,0);return true;}
   if(e.KeyCode==Keys.Up){Nudge(0,-step);return true;}if(e.KeyCode==Keys.Down){Nudge(0,step);return true;}
   if(e.KeyCode==Keys.Enter&&(canvas.Selected.Kind=="Texto"||canvas.Selected.Kind=="Código 39"||canvas.Selected.Kind=="QR")){EditText();return true;}
   if(e.KeyCode==Keys.Escape){canvas.Selected=null;RefreshAll();return true;}
   return false;
  }

  // ---- Menú del botón derecho ----
  static ToolStripMenuItem Item(string text,string keys,Action action,bool enabled=true){var i=new ToolStripMenuItem(text,null,(s,e)=>action()){Enabled=enabled};if(keys!=null)i.ShortcutKeyDisplayString=keys;return i;}
  static ToolStripMenuItem Check(string text,bool on,Action action){return new ToolStripMenuItem(text,null,(s,e)=>action()){Checked=on};}
  static ToolStripMenuItem Sub(string text,params ToolStripItem[] children){var i=new ToolStripMenuItem(text);i.DropDownItems.AddRange(children);return i;}
  void ShowContextMenu(Control host,Point at){
   PointF? spot=host==canvas&&canvas.Selected==null?(PointF?)canvas.ToLabel(at):null;
   contextMenu.Items.Clear();var e=canvas.Selected;var list=new List<ToolStripItem>();
   if(e==null){
    list.Add(Sub("Añadir aquí",Item("Texto",null,()=>AddAt("Texto",spot)),Item("Imagen o logotipo…",null,()=>AddAt("Imagen",spot)),Item("Código QR…",null,()=>AddAt("QR",spot)),Item("Código de barras",null,()=>AddAt("Código 39",spot)),Item("Marco",null,()=>AddAt("Marco",spot)),Item("Línea",null,()=>AddAt("Línea",spot))));
    list.Add(Item("Pegar",  "Ctrl+V",()=>Paste(spot),CanPaste()));list.Add(new ToolStripSeparator());
    list.Add(Item("Deshacer","Ctrl+Z",Undo,undo.Count>0));
   }else{
    var title=new ToolStripMenuItem(e.Kind+(String.IsNullOrEmpty(e.Text)?"":" · "+Short(e.Text))){Enabled=false};title.Font=new Font(contextMenu.Font,FontStyle.Bold);list.Add(title);list.Add(new ToolStripSeparator());
    if(e.Kind=="Texto"||e.Kind=="Código 39"||e.Kind=="QR")list.Add(Item(e.Kind=="Texto"?"Editar texto…":"Editar contenido…","Doble clic",EditText));
    if(e.Kind=="Texto"){list.Add(Check("Negrita",e.Bold,()=>ToggleStyle("b")));list.Add(Check("Cursiva",e.Italic,()=>ToggleStyle("i")));list.Add(Check("Subrayado",e.Underline,()=>ToggleStyle("u")));
     string a=String.IsNullOrEmpty(e.TextAlign)?"Izquierda":e.TextAlign;list.Add(Sub("Alineación del texto",Check("Izquierda",a=="Izquierda",()=>SetAlign("Izquierda")),Check("Centro",a=="Centro",()=>SetAlign("Centro")),Check("Derecha",a=="Derecha",()=>SetAlign("Derecha"))));}
    if(e.Kind=="Imagen"){
     list.Add(Item("Reemplazar imagen…",null,ReplaceImage));
     list.Add(Item("Voltear horizontalmente  ⇆",null,()=>TransformImage(RotateFlipType.RotateNoneFlipX)));
     list.Add(Item("Voltear verticalmente  ⇅",null,()=>TransformImage(RotateFlipType.RotateNoneFlipY)));
     list.Add(Item("Girar 90° a la derecha  ↻",null,()=>TransformImage(RotateFlipType.Rotate90FlipNone)));
     list.Add(Item("Girar 90° a la izquierda  ↺",null,()=>TransformImage(RotateFlipType.Rotate270FlipNone)));
     list.Add(Item("Girar 180°",null,()=>TransformImage(RotateFlipType.Rotate180FlipNone)));
     list.Add(new ToolStripSeparator());
     list.Add(Check("Mantener proporciones",e.LockAspect,()=>{aspectToggle.Checked=!e.LockAspect;}));
     list.Add(Item("Recuperar la forma original",null,RestoreImageShape));
     list.Add(Item("Ocupar toda la etiqueta",null,FillLabelWithImage));
     string q=String.IsNullOrEmpty(e.ImageMode)?"Automática":e.ImageMode;
     list.Add(Sub("Calidad al imprimir",Check("Automática",q=="Automática",()=>SetImageMode("")),Check("Nítida · logotipos e iconos",q=="Nítida",()=>SetImageMode("Nítida")),Check("Fotografía · tramado",q=="Fotografía",()=>SetImageMode("Fotografía"))));
    }
    list.Add(new ToolStripSeparator());
    list.Add(Item("Cortar","Ctrl+X",()=>CopySelected(true)));list.Add(Item("Copiar","Ctrl+C",()=>CopySelected(false)));list.Add(Item("Pegar","Ctrl+V",()=>Paste(null),CanPaste()));list.Add(Item("Duplicar","Ctrl+D",Duplicate));
    list.Add(new ToolStripSeparator());
    list.Add(Sub("Colocar en la etiqueta",Item("Centrar en la etiqueta",null,()=>AlignTo("center")),new ToolStripSeparator(),Item("A la izquierda",null,()=>AlignTo("left")),Item("Centrar horizontalmente",null,()=>AlignTo("hcenter")),Item("A la derecha",null,()=>AlignTo("right")),new ToolStripSeparator(),Item("Arriba",null,()=>AlignTo("top")),Item("Centrar verticalmente",null,()=>AlignTo("vcenter")),Item("Abajo",null,()=>AlignTo("bottom"))));
    int i=label.Elements.IndexOf(e),n=label.Elements.Count;
    list.Add(Sub("Orden (delante / detrás)",Item("Traer al frente",null,()=>Reorder(2),i<n-1),Item("Traer adelante",null,()=>Reorder(1),i<n-1),Item("Enviar atrás",null,()=>Reorder(-1),i>0),Item("Enviar al fondo",null,()=>Reorder(-2),i>0)));
    list.Add(new ToolStripSeparator());
    var del=Item("Eliminar","Supr",Delete);del.ForeColor=Color.Firebrick;list.Add(del);
   }
   contextMenu.Items.AddRange(list.ToArray());contextMenu.Show(host,at);
  }
  static string Short(string s){s=(s??"").Replace("\r","").Replace("\n"," ");return s.Length>28?s.Substring(0,27)+"…":s;}
  void AddAt(string kind,PointF? spot){int before=label.Elements.Count;Add(kind);if(!spot.HasValue||label.Elements.Count==before||canvas.Selected==null)return;var e=canvas.Selected;e.X=(float)Math.Round(Math.Max(0,Math.Min(label.Width-e.W,spot.Value.X)),1);e.Y=(float)Math.Round(Math.Max(0,Math.Min(label.Height-e.H,spot.Value.Y)),1);if(e.Kind=="Imagen")Editing.FitImage(e,label);RefreshAll();}

  // Cinta contextual: con una imagen seleccionada se ocultan los grupos de texto (no aplican) y se ve el de imagen.
  List<Control> textGroups;
  void SyncRibbon(Element e){
   if(formatBar!=null){SyncShell(e);return;}
   if(imageGroup==null||imageGroup.Parent==null)return;
   if(textGroups==null)textGroups=imageGroup.Parent.Controls.OfType<Panel>().Where(p=>p.Controls.OfType<System.Windows.Forms.Label>().Any(c=>c.Text=="Texto"||c.Text=="Alineación del texto")).Cast<Control>().ToList();
   bool image=e!=null&&e.Kind=="Imagen";foreach(var g in textGroups)g.Visible=!image;
  }
  // ---- Botones de imagen en la cinta "Inicio" ----
  void AddImageRibbonButtons(){
   if(imageGroup==null)return;imageGroup.Width=440;
   Func<string,string,Action,int,int,Button> make=(glyph,text,action,x,y)=>{var b=new Button{Text=glyph+"  "+text,TextAlign=ContentAlignment.MiddleLeft,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(244,248,245),Font=new Font("Segoe UI",9)};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(220,238,227);b.Click+=(s,e)=>action();RibbonUi.Place(imageGroup,b,x,y,125,24);RibbonUi.Tip(b,text);return b;};
   make("⇋","Reemplazar…",ReplaceImage,185,3);
   make("⇆","Voltear horiz.",()=>TransformImage(RotateFlipType.RotateNoneFlipX),185,28);
   make("⇅","Voltear vert.",()=>TransformImage(RotateFlipType.RotateNoneFlipY),185,53);
   make("↻","Girar derecha",()=>TransformImage(RotateFlipType.Rotate90FlipNone),312,3);
   make("↺","Girar izquierda",()=>TransformImage(RotateFlipType.Rotate270FlipNone),312,28);
   make("□","Toda la etiqueta",FillLabelWithImage,312,53);
  }
 }
}
