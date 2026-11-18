using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EtiquetaLocal {
 public static class Editing {
  public static Font FontFor(Element e){string name=String.IsNullOrEmpty(e.FontName)?"Arial":e.FontName;try{return new Font(name,e.FontSize*25.4f/72f,(e.Bold?FontStyle.Bold:FontStyle.Regular)|(e.Italic?FontStyle.Italic:FontStyle.Regular)|(e.Underline?FontStyle.Underline:FontStyle.Regular),GraphicsUnit.World);}catch{return new Font("Arial",e.FontSize*25.4f/72f,FontStyle.Regular,GraphicsUnit.World);}}
  public static void FitText(Element e,Label l){if(e.Kind!="Texto")return;using(var b=new Bitmap(1,1))using(var g=Graphics.FromImage(b))using(var f=FontFor(e))using(var sf=new StringFormat()){g.PageUnit=GraphicsUnit.Millimeter;float room=Math.Max(1,l.Width-e.X);var natural=g.MeasureString(e.Text??"",f,new SizeF(10000,10000),sf);e.W=String.IsNullOrEmpty(e.TextAlign)||e.TextAlign=="Izquierda"?Math.Max(1,Math.Min(room,natural.Width+.5f)):Math.Max(1,Math.Min(room,e.W));var fitted=g.MeasureString(e.Text??"",f,new SizeF(e.W,10000),sf);e.H=Math.Min(1000,Math.Max(1,fitted.Height+.3f));}if(IsDymo(l))CenterOnTape(e,l);}
  // DYMO LetraTag: cinta de 12 mm con zona imprimible de 2,2 a 9,8 mm. Texto y emojis comparten tamaño y se centran con un margen mínimo.
  const float DymoTop=2.2f,DymoBottom=9.8f,DymoMargin=.3f;
  static float dymoPoints;
  static bool IsDymo(Label l){return l!=null&&l.Media=="DYMO LT12";}
  static bool IsSymbolFont(Element e){return e.FontName!=null&&e.FontName.IndexOf("Emoji",StringComparison.OrdinalIgnoreCase)>=0;}
  public static float DymoContentHeight(){return DymoBottom-DymoTop-2*DymoMargin;}
  const float DymoGap=1.5f;
  // Posición donde empieza el dibujo del siguiente elemento: tras el dibujo más a la derecha, con un hueco constante.
  public static float NextFreeX(Label l){if(l.Elements.Count==0)return Math.Min(2,l.Width/10);float x=0;foreach(var e in l.Elements){float right=e.X+e.W,a,b,c,d;if(e.Kind=="Texto"&&!String.IsNullOrEmpty(e.Text)&&InkBox(e,e.Text,out a,out b,out c,out d))right=e.X+c;x=Math.Max(x,right);}x+=DymoGap;return x>l.Width-5?Math.Min(2,l.Width/10):x;}
  // Coloca un texto o emoji nuevo para que su dibujo empiece justo en la siguiente posición libre.
  public static void PlaceAfterOthers(Element e,Label l){float x=NextFreeX(l),a,b,c,d;if(e.Kind=="Texto"&&!String.IsNullOrEmpty(e.Text)&&InkBox(e,e.Text,out a,out b,out c,out d))x-=a;e.X=Math.Max(.3f,x);FitText(e,l);}
  static bool TextInk(Element source,string text,out float top,out float bottom){float left,right;return InkBox(source,text,out left,out top,out right,out bottom);}
  // Zona con tinta (mm desde la esquina superior izquierda del elemento) al dibujar el texto indicado con la fuente del elemento.
  static bool InkBox(Element source,string text,out float left,out float top,out float right,out float bottom){
   left=top=right=bottom=0;var e=source.Copy();e.Text=text;e.X=e.Y=0;e.TextAlign=null;e.W=1000;FitText(e,new Label{Width=1000,Height=100});
   float scale=Math.Min(40,4000/Math.Max(1,e.W));int width=Math.Max(1,(int)Math.Ceiling(e.W*scale)+2),height=Math.Max(1,(int)Math.Ceiling(e.H*scale)+2);
   using(var b=new Bitmap(width,height,PixelFormat.Format24bppRgb)){
    using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;g.ScaleTransform(scale,scale);Render.Draw(g,new Label{Elements=new List<Element>{e}});}
    var data=b.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);int first=-1,last=-1,minX=int.MaxValue,maxX=-1;
    try{byte[] row=new byte[Math.Abs(data.Stride)];for(int y=0;y<height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<width;x++){int i=x*3;if(row[i]*114+row[i+1]*587+row[i+2]*299>=128000)continue;if(first<0)first=y;last=y;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}}}finally{b.UnlockBits(data);}
    if(first<0)return false;left=minX/scale;right=(maxX+1)/scale;top=first/scale;bottom=(last+1)/scale;return true;
   }
  }
  // Mayor tamaño (pt) con el que texto con tildes y descendentes y los emojis caben en la cinta con el margen mínimo.
  public static float DymoFontSize(){
   if(dymoPoints>0)return dymoPoints;float mid=(DymoTop+DymoBottom)/2,room=DymoContentHeight();
   for(float pt=24;pt>=4;pt-=.5f){
    bool ok=true;
    foreach(bool bold in new[]{false,true}){var t=new Element{Kind="Texto",FontName="Arial",FontSize=pt,Bold=bold};float capTop,capBottom,inkTop,inkBottom;if(!TextInk(t,"H",out capTop,out capBottom)||!TextInk(t,"ÁÉÑgjpqy",out inkTop,out inkBottom)){ok=false;break;}float y=mid-(capTop+capBottom)/2;if(y+inkTop<DymoTop+DymoMargin-.01f||y+inkBottom>DymoBottom-DymoMargin+.01f){ok=false;break;}}
    if(ok)foreach(string s in new[]{"😀","⚠","♻","👍","🔥","★"}){var t=new Element{Kind="Texto",FontName="Segoe UI Emoji",FontSize=pt};float a,b;if(TextInk(t,s,out a,out b)&&b-a>room+.01f){ok=false;break;}}
    if(ok){dymoPoints=pt;return pt;}
   }
   dymoPoints=9;return dymoPoints;
  }
  // Centra el elemento en la zona imprimible: el texto por la altura de sus mayúsculas (misma línea base para todos) y los emojis por su propio dibujo.
  public static void CenterOnTape(Element e,Label l){
   if(!IsDymo(l))return;float mid=(DymoTop+DymoBottom)/2;
   if(e.Kind=="Texto"&&!String.IsNullOrEmpty(e.Text)&&e.Text.IndexOf('\n')<0){float a,b;if(IsSymbolFont(e)?TextInk(e,e.Text,out a,out b):TextInk(e,"H",out a,out b)){e.Y=mid-(a+b)/2;return;}}
   e.Y=mid-e.H/2;
  }
  public static void RestoreImageRatio(Element e){if(e.Kind!="Imagen"||String.IsNullOrEmpty(e.ImageData))return;using(var ms=new MemoryStream(Convert.FromBase64String(e.ImageData)))using(var im=Image.FromStream(ms))e.ImageAspect=(float)im.Width/im.Height;}
  public static void FitImage(Element e,Label l){if(e.Kind!="Imagen")return;if(e.ImageAspect<=0)RestoreImageRatio(e);float top=l.Media=="DYMO LT12"?2.2f:0,bottom=l.Media=="DYMO LT12"?9.8f:l.Height;e.X=Math.Max(.2f,Math.Min(e.X,l.Width-.7f));e.Y=Math.Max(top+.2f,Math.Min(e.Y,bottom-.7f));float w=Math.Min(e.W,Math.Max(.5f,l.Width-e.X-.2f)),h=w/e.ImageAspect;if(h>bottom-e.Y-.2f){h=Math.Max(.5f,bottom-e.Y-.2f);w=h*e.ImageAspect;}e.W=w;e.H=h;}
  public static string MarginProblem(Label label){
   float top=label.Media=="DYMO LT12"?2.2f:0,bottom=label.Media=="DYMO LT12"?9.8f:label.Height;
   foreach(var e in label.Elements){
    if(e.X>=0&&e.Y>=top&&e.X+e.W<=label.Width+.01&&e.Y+e.H<=bottom+.01)continue;
    // Check rendered ink, rather than the empty part of a text/image bounding box.
    var local=e.Copy();local.X=local.Y=0;float scale=Math.Min(8,Math.Min(1600/Math.Max(1,e.W),1600/Math.Max(1,e.H)));
    int width=Math.Max(1,(int)Math.Ceiling(e.W*scale)),height=Math.Max(1,(int)Math.Ceiling(e.H*scale));
    using(var b=new Bitmap(width,height,PixelFormat.Format24bppRgb)){
     using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;g.ScaleTransform(scale,scale);Render.Draw(g,new Label{Elements=new List<Element>{local}});}
     var data=b.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
     try{byte[] row=new byte[Math.Abs(data.Stride)];for(int y=0;y<height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<width;x++){int i=x*3;if(row[i]*114+row[i+1]*587+row[i+2]*299>=128000)continue;float px=e.X+(x+.5f)/scale,py=e.Y+(y+.5f)/scale;if(px<-.06||px>label.Width+.06||py<top-.06||py>bottom+.06){string title=e.Kind+" «"+(e.Text??"")+"»";if(title.Length>65)title=title.Substring(0,62)+"…";return title+": parte del contenido visible queda fuera de la zona imprimible (X="+px.ToString("0.0")+", Y="+py.ToString("0.0")+" mm).";}}}}finally{b.UnlockBits(data);}
    }
   }return null;
  }
  public static string PickEmoji(IWin32Window owner){
   string selected=null;using(var form=new Form{Text="Emojis y símbolos",ClientSize=new Size(520,340),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
    var tabs=new TabControl{Dock=DockStyle.Fill};string[][] groups={new[]{"Caras","😀","😊","😎","😉","😍","🤔","😴","😢","😡","👍","👎","👏"},new[]{"Señales","⚠","✓","✕","❗","❓","⛔","♻","☢","☣","⚡","🔥","🚧"},new[]{"Objetos","🔧","🔨","📦","🏷","🔋","💡","🔒","🔑","☎","✉","💻","🏭"},new[]{"Flechas y formas","←","→","↑","↓","↔","↕","★","☆","♥","●","■","▲"}};
    foreach(var group in groups){var page=new TabPage(group[0]);var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),AutoScroll=true};foreach(string symbol in group.Skip(1)){string value=symbol;var button=new Button{Text=value,Width=70,Height=65,Font=new Font("Segoe UI Emoji",22)};button.Click+=(s,e)=>{selected=value;form.DialogResult=DialogResult.OK;form.Close();};flow.Controls.Add(button);}page.Controls.Add(flow);tabs.TabPages.Add(page);}var note=new System.Windows.Forms.Label{Text="Se imprimen en monocromo. El aspecto depende de las fuentes instaladas y del cabezal.",Dock=DockStyle.Bottom,Height=40,Padding=new Padding(8)};form.Controls.Add(tabs);form.Controls.Add(note);form.ShowDialog(owner);
   }return selected;
  }
  public static void SelfTest(){
   var l=new Label{Media="DYMO LT12",Width=50,Height=12};var e=new Element{Kind="Texto",Text="TEST",X=2,Y=2.5f,W=40,H=15,FontSize=8,FontName="Arial"};l.Elements.Add(e);if(MarginProblem(l)!=null)throw new Exception("Un recuadro vacío no debe bloquear tinta dentro de márgenes.");e.Y=9;if(MarginProblem(l)==null)throw new Exception("La tinta realmente fuera del margen debe detectarse.");e.Y=2.5f;e.FontSize=10;FitText(e,l);if(e.W<=1||e.H<=1)throw new Exception("Ajuste de texto.");
   using(var b=new Bitmap(20,10))using(var ms=new MemoryStream()){using(var g=Graphics.FromImage(b))g.Clear(Color.Black);b.Save(ms,ImageFormat.Png);var image=new Element{Kind="Imagen",ImageData=Convert.ToBase64String(ms.ToArray()),X=1,Y=2.4f,W=30,H=10,LockAspect=true};RestoreImageRatio(image);FitImage(image,l);if(Math.Abs(image.W/image.H-2)>.001)throw new Exception("Proporciones de imagen.");}
   // Un logotipo con fondo blanco puede sobresalir por cualquier lado si lo que se pinta queda dentro.
   using(var b=new Bitmap(40,20))using(var ms=new MemoryStream()){using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.FillRectangle(Brushes.Black,20,5,10,10);}b.Save(ms,ImageFormat.Png);var paper=new Label{Width=100,Height=50};var logo=new Element{Kind="Imagen",ImageData=Convert.ToBase64String(ms.ToArray()),X=-8,Y=-4,W=40,H=20,LockAspect=true,ImageAspect=2};paper.Elements.Add(logo);Model.Validate(Model.Load(Model.Save(paper)));if(MarginProblem(paper)!=null)throw new Exception("El fondo blanco fuera de la etiqueta no debe bloquear la impresión.");logo.X=-25;if(MarginProblem(paper)==null)throw new Exception("La tinta fuera por la izquierda debe detectarse.");}
   var dymo=new Label{Media="DYMO LT12",Width=60,Height=12};float points=DymoFontSize();if(points<12)throw new Exception("El texto DYMO debería aprovechar la cinta de 12 mm.");
   var words=new Element{Kind="Texto",Text="ALMACÉN 01 gj",FontName="Arial",FontSize=points,X=2,Y=0};FitText(words,dymo);var symbol=new Element{Kind="Texto",Text="⚠",FontName="Segoe UI Emoji",FontSize=points,X=45,Y=0};FitText(symbol,dymo);dymo.Elements.Add(words);dymo.Elements.Add(symbol);
   string dymoProblem=MarginProblem(dymo);if(dymoProblem!=null)throw new Exception("Texto y emoji DYMO deben caber en la cinta: "+dymoProblem);
   float capTop,capBottom;TextInk(words,"H",out capTop,out capBottom);if(Math.Abs(words.Y+(capTop+capBottom)/2-6)>.15f)throw new Exception("El texto DYMO debe quedar centrado en la cinta.");
   var loaded=Model.Load(Model.Save(new Label{Elements=new List<Element>{new Element{FontName="Verdana",LockAspect=true,ImageAspect=2}}}));if(loaded.Elements[0].FontName!="Verdana")throw new Exception("Persistencia de fuentes.");
  }
 }
}
