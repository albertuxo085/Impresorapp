using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace EtiquetaLocal {
 public static class Branding {
  static Icon appIcon;
  public static Icon AppIcon {get {return appIcon??(appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath)??SystemIcons.Application);}}
  static GraphicsPath Rounded(float x,float y,float w,float h,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;}
  public static Bitmap Draw(int size){
   var b=new Bitmap(size,size);using(var g=Graphics.FromImage(b)){
    g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/256f,size/256f);
    using(var shape=Rounded(8,8,240,240,54))using(var gradient=new LinearGradientBrush(new Point(20,20),new Point(235,240),Color.FromArgb(53,136,96),Color.FromArgb(29,83,65)))g.FillPath(gradient,shape);
    using(var shadow=Rounded(43,94,174,106,23))using(var brush=new SolidBrush(Color.FromArgb(45,0,35,20)))g.FillPath(brush,shadow);
    using(var paper=Rounded(74,45,108,76,8))g.FillPath(Brushes.White,paper);
    using(var body=Rounded(39,88,178,101,23))using(var brush=new SolidBrush(Color.FromArgb(226,241,231)))g.FillPath(brush,body);
    using(var slot=Rounded(63,143,130,27,9))using(var brush=new SolidBrush(Color.FromArgb(29,72,52)))g.FillPath(brush,slot);
    using(var label=Rounded(75,155,106,62,10))g.FillPath(Brushes.White,label);
    using(var pen=new Pen(Color.FromArgb(46,92,65),7)){g.DrawLine(pen,94,177,161,177);g.DrawLine(pen,94,193,140,193);}
    using(var brush=new SolidBrush(Color.FromArgb(129,197,151)))g.FillEllipse(brush,177,108,14,14);
    using(var tag=Rounded(166,172,58,44,12))using(var brush=new SolidBrush(Color.FromArgb(155,217,166)))g.FillPath(brush,tag);
    using(var pen=new Pen(Color.FromArgb(32,81,54),5)){g.DrawLine(pen,179,184,179,204);g.DrawLine(pen,188,184,188,204);g.DrawLine(pen,201,184,201,204);g.DrawLine(pen,211,184,211,204);}
   }return b;
  }
 }
}
