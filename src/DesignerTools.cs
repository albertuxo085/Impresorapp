using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using QRCoder;
namespace EtiquetaLocal {
 public static class QrSupport {
  static Dictionary<string,bool[,]> cache=new Dictionary<string,bool[,]>();
  public static bool[,] Matrix(string text){
   if(String.IsNullOrWhiteSpace(text))throw new Exception("Escribe un enlace o texto para el QR.");
   if(Encoding.UTF8.GetByteCount(text)>1200)throw new Exception("El contenido del QR supera 1200 bytes UTF-8. Reduce el texto.");
   bool[,] value;if(cache.TryGetValue(text,out value))return value;
   using(var generator=new QRCodeGenerator())using(var data=generator.CreateQrCode(text,QRCodeGenerator.ECCLevel.Q,true)){
    int n=data.ModuleMatrix.Count;value=new bool[n,n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)value[x,y]=data.ModuleMatrix[y][x];
   }
   if(cache.Count>=32)cache.Clear();cache[text]=value;return value;
  }
  public static void Draw(Graphics g,Element e){var m=Matrix(e.Text);int n=m.GetLength(0);float side=Math.Min(e.W,e.H),step=side/n,x0=e.X+(e.W-side)/2,y0=e.Y+(e.H-side)/2;g.FillRectangle(Brushes.White,x0,y0,side,side);for(int y=0;y<n;y++)for(int x=0;x<n;x++)if(m[x,y])g.FillRectangle(Brushes.Black,x0+x*step,y0+y*step,step,step);}
  public static string Warning(Label l,int dots){return l.Elements.Any(e=>e.Kind=="QR"&&Math.Min(e.W,e.H)*dots/Matrix(e.Text).GetLength(0)<3)?"\nAviso: un QR tiene módulos menores de 3 puntos. Amplíalo y comprueba su lectura con el móvil.":"";}
  public static void SelfTest(){foreach(string text in new[]{"https://example.com/almacen?id=42","Ubicación: Almacén · Niño 😀\nLote 123"}){var m=Matrix(text);int n=m.GetLength(0);if(n<29||n!=m.GetLength(1))throw new Exception("Matriz QR incorrecta");for(int y=0;y<n;y++)for(int x=0;x<n;x++)if((x<4||y<4||x>=n-4||y>=n-4)&&m[x,y])throw new Exception("Margen QR incorrecto");var l=new Label{Elements=new List<Element>{new Element{Kind="QR",Text=text,W=30,H=30}}};if(Model.Load(Model.Save(l)).Elements[0].Text!=text)throw new Exception("Persistencia QR");using(var b=new Bitmap(n*8,n*8))using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.ScaleTransform(n*8/30f,n*8/30f);Render.Draw(g,l);b.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,text.StartsWith("https")?"qr-url-test.png":"qr-text-test.png"));}}
   bool rejected=false;try{Matrix(new string('a',1201));}catch{rejected=true;}if(!rejected)throw new Exception("Límite QR");
  }
 }
 public static class SmartGuides {
  public static void Snap(Element e,Label l,float tolerance,out float? vertical,out float? horizontal){
   float dx=0,dy=0,bestX=tolerance,bestY=tolerance;vertical=horizontal=null;
   var xs=new List<float>{0,l.Width/2,l.Width};var ys=new List<float>{0,l.Height/2,l.Height};foreach(var other in l.Elements.Where(v=>v!=e)){xs.AddRange(new[]{other.X,other.X+other.W/2,other.X+other.W});ys.AddRange(new[]{other.Y,other.Y+other.H/2,other.Y+other.H});}
   foreach(float anchor in new[]{e.X,e.X+e.W/2,e.X+e.W})foreach(float target in xs){float d=target-anchor;if(Math.Abs(d)<bestX&&e.X+d>=0){bestX=Math.Abs(d);dx=d;vertical=target;}}
   foreach(float anchor in new[]{e.Y,e.Y+e.H/2,e.Y+e.H})foreach(float target in ys){float d=target-anchor;if(Math.Abs(d)<bestY&&e.Y+d>=0){bestY=Math.Abs(d);dy=d;horizontal=target;}}
   e.X+=dx;e.Y+=dy;
  }
  public static void SelfTest(){var l=new Label{Width=100,Height=50};var e=new Element{X=39.7f,Y=19.8f,W=20,H=10};l.Elements.Add(e);float? x,y;Snap(e,l,.6f,out x,out y);if(x!=50||y!=25||Math.Abs(e.X-40)>.001||Math.Abs(e.Y-20)>.001)throw new Exception("Centrado con guías");var peer=new Element{X=10,Y=10,W=20,H=8};l.Elements.Add(peer);e.X=10.1f;e.Y=10.1f;Snap(e,l,.2f,out x,out y);if(e.X!=10||e.Y!=10)throw new Exception("Alineación con otro elemento");l.Elements.Remove(peer);e.X=12;e.Y=6;Snap(e,l,.2f,out x,out y);if(x.HasValue||y.HasValue||e.X!=12||e.Y!=6)throw new Exception("Guías fuera de tolerancia");}
 }
}
