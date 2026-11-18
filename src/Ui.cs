using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace EtiquetaLocal {
 public static class Ui {
  static readonly ToolTip Tips=new ToolTip();
  public static Button Action(string icon,string text,Action action,int width=218,bool primary=false){
   var b=new Button{Text=" "+text,Image=Icon(icon,primary?Color.White:Color.FromArgb(42,85,65)),TextImageRelation=TextImageRelation.ImageBeforeText,ImageAlign=ContentAlignment.MiddleLeft,TextAlign=ContentAlignment.MiddleLeft,Width=width,Height=32,Margin=new Padding(0,2,5,2),Padding=new Padding(7,0,3,0),FlatStyle=FlatStyle.Flat,BackColor=primary?Color.FromArgb(37,108,76):Color.White,ForeColor=primary?Color.White:Color.FromArgb(33,55,43),Cursor=Cursors.Hand};
   b.FlatAppearance.BorderColor=Color.FromArgb(202,220,209);b.Click+=(s,e)=>action();Tips.SetToolTip(b,text);return b;
  }
  public static FlowLayoutPanel Section(FlowLayoutPanel parent,string title){
   var group=new GroupBox{Text=title,Width=232,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(6,14,6,6),Margin=new Padding(0,0,0,12),BackColor=Color.White};
   var content=new FlowLayoutPanel{Width=220,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Dock=DockStyle.Fill,Margin=new Padding(0)};
   group.Controls.Add(content);parent.Controls.Add(group);return content;
  }
  public static System.Windows.Forms.Label Hint(string text){return new System.Windows.Forms.Label{Text=text,Width=214,AutoSize=true,MaximumSize=new Size(214,0),ForeColor=Color.FromArgb(85,100,120),Margin=new Padding(0,1,0,7)};}
  public static Bitmap Icon(string kind){return Icon(kind,Color.FromArgb(42,85,65));}
  public static Bitmap Icon(string kind,Color color){
   var b=new Bitmap(20,20);using(var g=Graphics.FromImage(b))using(var p=new Pen(color,1.7f))using(var fill=new SolidBrush(color)){
    g.SmoothingMode=SmoothingMode.AntiAlias;
    if(kind=="print"){g.DrawRectangle(p,5,1,10,6);g.DrawRectangle(p,2,7,16,8);g.FillRectangle(Brushes.White,5,11,10,8);g.DrawRectangle(p,5,11,10,8);g.DrawLine(p,7,14,13,14);}
    else if(kind=="usb"){g.DrawLine(p,10,2,10,17);g.DrawLines(p,new[]{new Point(7,5),new Point(10,1),new Point(13,5)});g.DrawLines(p,new[]{new Point(10,13),new Point(4,9),new Point(4,6)});g.DrawEllipse(p,2,3,4,4);g.DrawLines(p,new[]{new Point(10,10),new Point(16,7),new Point(16,4)});g.DrawRectangle(p,14,1,4,4);g.DrawEllipse(p,8,15,4,4);}
    else if(kind=="open"){g.DrawLines(p,new[]{new Point(2,17),new Point(2,4),new Point(8,4),new Point(10,7),new Point(18,7),new Point(16,17),new Point(2,17)});g.DrawLine(p,2,9,17,9);}
    else if(kind=="qr"){g.DrawRectangle(p,1,1,6,6);g.DrawRectangle(p,12,1,6,6);g.DrawRectangle(p,1,12,6,6);g.FillRectangle(fill,3,3,2,2);g.FillRectangle(fill,14,3,2,2);g.FillRectangle(fill,3,14,2,2);g.FillRectangle(fill,11,11,3,3);g.FillRectangle(fill,16,13,3,3);g.FillRectangle(fill,11,16,3,3);}
    else if(kind=="barcode"){for(int x=2;x<19;x+=3){g.FillRectangle(fill,x,3,x%2+1,14);}}
    else if(kind=="text"){g.DrawLine(p,3,3,17,3);g.DrawLine(p,10,3,10,17);g.DrawLine(p,6,17,14,17);}
    else if(kind=="network"){g.DrawRectangle(p,6,1,8,5);g.DrawLine(p,10,6,10,10);g.DrawLine(p,3,10,17,10);g.DrawLine(p,3,10,3,14);g.DrawLine(p,17,10,17,14);g.DrawRectangle(p,0,14,6,5);g.DrawRectangle(p,14,14,5,5);}
    else if(kind=="image"||kind=="example"){g.DrawRectangle(p,2,2,16,16);g.DrawEllipse(p,5,5,3,3);g.DrawLines(p,new[]{new Point(3,16),new Point(8,10),new Point(12,14),new Point(15,10),new Point(18,15)});}
    else if(kind=="save"){g.DrawRectangle(p,2,2,16,16);g.DrawRectangle(p,5,2,9,5);g.DrawRectangle(p,5,11,10,7);}
    else if(kind=="delete"){g.DrawLine(p,3,5,17,5);g.DrawLine(p,7,2,13,2);g.DrawRectangle(p,5,5,10,13);g.DrawLine(p,8,8,8,15);g.DrawLine(p,12,8,12,15);}
    else if(kind=="undo"){g.DrawArc(p,5,5,12,12,210,270);g.DrawLines(p,new[]{new Point(2,5),new Point(7,5),new Point(7,0)});}
    else if(kind=="bluetooth"){g.DrawLines(p,new[]{new Point(9,1),new Point(16,6),new Point(4,15),new Point(9,19),new Point(9,1),new Point(16,14),new Point(4,5)});}
    else if(kind=="copy"){g.DrawRectangle(p,2,2,12,12);g.FillRectangle(Brushes.White,6,6,12,12);g.DrawRectangle(p,6,6,12,12);}
    else if(kind=="emoji"){g.DrawEllipse(p,2,2,16,16);g.FillEllipse(fill,6,6,2,2);g.FillEllipse(fill,12,6,2,2);g.DrawArc(p,5,7,10,8,10,160);}
    else if(kind=="edit"){g.DrawLines(p,new[]{new Point(3,14),new Point(13,2),new Point(17,6),new Point(7,17),new Point(3,18),new Point(3,14)});}
    else if(kind=="line"){g.DrawLine(p,2,17,18,3);}
    else if(kind=="frame"||kind=="size"){g.DrawRectangle(p,2,3,16,14);if(kind=="size"){g.DrawLine(p,6,6,14,6);g.DrawLine(p,14,6,14,13);}}
    else {g.DrawRectangle(p,4,1,12,18);g.DrawLine(p,7,7,13,7);g.DrawLine(p,7,11,13,11);}
   }return b;
  }
 }
}
