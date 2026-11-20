using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
namespace EtiquetaLocal {
 public class LabelFormat {
  public string Name;public float W,H;public bool Dymo,Paper;
  public LabelFormat(string name,float w,float h,bool dymo=false,bool paper=false){Name=name;W=w;H=h;Dymo=dymo;Paper=paper;}
  public override string ToString(){return Name;}
 }
 public static class Formats {
#if SENCILLA
  public const string SizeHint="Etiqueta de rollo o papel A4 / A3.",SizeList="rollos de etiquetas y papel A4 / A3";
#else
  public const string SizeHint="Etiqueta de rollo, cinta DYMO o papel A4 / A3.",SizeList="rollos de etiquetas, cinta DYMO y papel A4 / A3";
#endif
  public static readonly LabelFormat[] All={
   new LabelFormat("Personalizado",0,0),
   new LabelFormat("Pequeña · 30 × 20 mm",30,20),new LabelFormat("Pequeña · 40 × 20 mm",40,20),
   new LabelFormat("Producto · 40 × 30 mm",40,30),new LabelFormat("Producto · 50 × 25 mm",50,25),
   new LabelFormat("Producto · 50 × 30 mm",50,30),new LabelFormat("Producto · 50 × 40 mm",50,40),
   new LabelFormat("Cuadrada · 50 × 50 mm (5 × 5 cm)",50,50),
   new LabelFormat("Producto · 60 × 40 mm",60,40),new LabelFormat("Producto · 60 × 60 mm",60,60),
   new LabelFormat("Alargada · 70 × 30 mm",70,30),new LabelFormat("Alargada · 80 × 40 mm",80,40),
   new LabelFormat("Producto · 80 × 50 mm",80,50),new LabelFormat("Alargada · 100 × 30 mm",100,30),
   new LabelFormat("Alargada · 100 × 50 mm",100,50),new LabelFormat("Cuadrada · 100 × 100 mm (10 × 10 cm)",100,100),
   new LabelFormat("Envío · 100 × 150 mm",100,150),new LabelFormat("Envío · 101,6 × 152,4 mm (4 × 6 pulgadas)",101.6f,152.4f),
   new LabelFormat("Papel A5 · vertical · 148 × 210 mm",148,210,false,true),new LabelFormat("Papel A5 · horizontal · 210 × 148 mm",210,148,false,true),
   new LabelFormat("Papel A4 · vertical · 210 × 297 mm",210,297,false,true),new LabelFormat("Papel A4 · horizontal · 297 × 210 mm",297,210,false,true),
   new LabelFormat("Papel A3 · vertical · 297 × 420 mm",297,420,false,true),new LabelFormat("Papel A3 · horizontal · 420 × 297 mm",420,297,false,true),
   new LabelFormat("DYMO LT · 50 × 12 mm",50,12,true),new LabelFormat("DYMO LT · 75 × 12 mm",75,12,true),
   new LabelFormat("DYMO LT · 100 × 12 mm",100,12,true),new LabelFormat("DYMO LT · longitud personalizada",0,12,true)
  };
  public static LabelFormat Match(Label l){
   return All.FirstOrDefault(f=>f.W>0&&f.Dymo==(l.Media=="DYMO LT12")&&f.Paper==(l.Media=="PAPER")&&Math.Abs(f.W-l.Width)<.05&&Math.Abs(f.H-l.Height)<.05)
    ??All.Last(f=>f.W==0&&f.Dymo==(l.Media=="DYMO LT12"));
  }
  static string Icon(){using(var b=new Bitmap(96,96)){
   using(var g=Graphics.FromImage(b)){g.Clear(Color.White);using(var pen=new Pen(Color.Black,5)){g.DrawRectangle(pen,19,24,58,50);g.DrawLine(pen,19,24,48,10);g.DrawLine(pen,48,10,77,24);g.DrawLine(pen,48,26,48,74);}}
   using(var m=new MemoryStream()){b.Save(m,ImageFormat.Png);return Convert.ToBase64String(m.ToArray());}
  }}
  public static Label Example(Label source){
   var l=new Label{Width=source.Width,Height=source.Height,Media=source.Media};
   float w=l.Width,h=l.Height,m=Math.Min(3,Math.Min(w,h)/10);
   if(l.Media=="PAPER"){
    float margin=15,iconSize=Math.Min(65,w*.27f);
    l.Elements.Add(new Element{Text="ALMACÉN",X=margin,Y=15,W=w-2*margin,H=35,FontSize=36,Bold=true});
    l.Elements.Add(new Element{Kind="Imagen",Text="Símbolo de almacén",ImageData=Icon(),X=(w-iconSize)/2,Y=h*.28f,W=iconSize,H=iconSize,ImageAspect=1});
    l.Elements.Add(new Element{Text="ZONA 01",X=margin,Y=h*.65f,W=w-2*margin,H=25,FontSize=28,Bold=true});
    l.Elements.Add(new Element{Text="Mantén el paso libre",X=margin,Y=h*.81f,W=w-2*margin,H=20,FontSize=18});return l;
   }
   if(l.Media=="DYMO LT12"){
    l.Elements.Add(new Element{Text="AZOR 001",X=3,Y=3,W=w-6,H=5.5f,FontSize=10,Bold=true});return l;
   }
   float icon=Math.Min(12,Math.Min(w*.22f,h*.25f));float titleX=m+icon+2;
   l.Elements.Add(new Element{Kind="Imagen",Text="Ejemplo de imagen",ImageData=Icon(),X=m,Y=m,W=icon,H=icon,ImageAspect=1,LockAspect=true});
   l.Elements.Add(new Element{Text="AZOR",X=titleX,Y=m,W=Math.Max(2,w-titleX-m),H=icon,FontSize=Math.Min(16,Math.Max(6,icon*1.5f)),Bold=true});
   float textY=m+icon+1;
   l.Elements.Add(new Element{Text="REF. 001",X=m,Y=textY,W=w-2*m,H=Math.Max(2,h*.15f),FontSize=Math.Min(12,Math.Max(5,h*.19f))});
   if(w>=40&&h>=25){
    l.Elements.Add(new Element{Kind="Código 39",Text="001",X=m,Y=h*.58f,W=w-2*m,H=h*.23f});
    l.Elements.Add(new Element{Text="001",X=m,Y=h*.83f,W=w-2*m,H=h*.12f,FontSize=Math.Min(10,Math.Max(5,h*.16f))});
   }
   return l;
  }
  public static void SelfTest(){
   foreach(var f in All.Where(x=>x.W>0)){
    var l=Model.Load(Model.Save(Example(new Label{Width=f.W,Height=f.H,Media=f.Dymo?"DYMO LT12":f.Paper?"PAPER":""})));
    if(Model.Outside(l))throw new Exception("Ejemplo fuera del formato: "+f.Name+" / "+Editing.MarginProblem(l));
    if(Match(l)!=f)throw new Exception("Formato no recuperado: "+f.Name);
    if(!f.Dymo&&!f.Paper){var z=System.Text.Encoding.ASCII.GetString(Direct.Build(l,8,1));
     if(!z.Contains("^PW"+(int)Math.Round(f.W*8)+"^LL"+(int)Math.Round(f.H*8)))throw new Exception("Medidas ZPL incorrectas: "+f.Name);}
   }
  }
 }
}
