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
  public static void FitText(Element e,Label l){if(e.Kind!="Texto")return;using(var b=new Bitmap(1,1))using(var g=Graphics.FromImage(b))using(var f=FontFor(e))using(var sf=new StringFormat()){g.PageUnit=GraphicsUnit.Millimeter;float room=Math.Max(1,l.Width-e.X);var natural=g.MeasureString(e.Text??"",f,new SizeF(10000,10000),sf);e.W=String.IsNullOrEmpty(e.TextAlign)||e.TextAlign=="Izquierda"?Math.Max(1,Math.Min(room,natural.Width+.5f)):Math.Max(1,Math.Min(room,e.W));var fitted=g.MeasureString(e.Text??"",f,new SizeF(e.W,10000),sf);e.H=Math.Min(1000,Math.Max(1,fitted.Height+.3f));}}
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
   var loaded=Model.Load(Model.Save(new Label{Elements=new List<Element>{new Element{FontName="Verdana",LockAspect=true,ImageAspect=2}}}));if(loaded.Elements[0].FontName!="Verdana")throw new Exception("Persistencia de fuentes.");
  }
 }
}
