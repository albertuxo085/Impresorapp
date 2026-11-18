using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
namespace EtiquetaLocal {
 public static class DesignerVerification {
  static float InkCenter(Label l){using(var b=new Bitmap(800,240))using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;g.ScaleTransform(8,8);Render.Draw(g,l);int left=800,right=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).R<128){left=Math.Min(left,x);right=Math.Max(right,x);}if(right<0)throw new Exception("Texto sin tinta");return (left+right)/2f;}}
  public static void Run(){var e=new Element{X=5,Y=3,W=80,H=20,Text="PRUEBA",FontSize=12,TextAlign="Izquierda",Bold=true,Italic=true,Underline=true};var l=new Label{Width=100,Height=30,Elements=new List<Element>{e}};float a=InkCenter(l);e.TextAlign="Centro";float b=InkCenter(l);e.TextAlign="Derecha";float c=InkCenter(l);if(!(b>a+80&&c>b+80))throw new Exception("Alineación de tinta incorrecta");var loaded=Model.Load(Model.Save(l)).Elements[0];if(!loaded.Bold||!loaded.Italic||!loaded.Underline||loaded.TextAlign!="Derecha")throw new Exception("No se conservan estilos y alineación");
   var qr=new Label{Width=50,Height=50,Elements=new List<Element>{new Element{Kind="QR",Text="https://example.com/almacen?id=42",X=5,Y=5,W=35,H=35}}};using(var image=new Bitmap(400,400,PixelFormat.Format24bppRgb))using(var g=Graphics.FromImage(image)){g.Clear(Color.White);g.ScaleTransform(8,8);Render.Draw(g,qr);image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qr-203dpi-test.png"));}var bytes=Direct.Build(qr,8,1);File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qr-payload-test.zpl"),bytes);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qr-example.etiqueta"),Model.Save(qr));if(bytes.Length==0||QrSupport.Warning(qr,8)!="")throw new Exception("Salida QR ZPL incorrecta");qr.Elements[0].W=qr.Elements[0].H=5;if(QrSupport.Warning(qr,8)=="")throw new Exception("Falta advertencia de QR pequeño");
  }
 }
}
