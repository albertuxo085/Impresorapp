using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace EtiquetaLocal {
 public static class RibbonUi {
  static readonly Color Surface=Color.FromArgb(244,248,245),Active=Color.FromArgb(205,230,214);
  static readonly ToolTip Tips=new ToolTip();
  public static void Tip(Control control,string text){Tips.SetToolTip(control,text);control.AccessibleName=text;}
  public static FlowLayoutPanel Row(){return new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(5,4,5,2),BackColor=Surface,AutoScroll=true};}
  public static Panel Group(FlowLayoutPanel parent,string title,int width){var panel=new Panel{Width=width,Height=100,Margin=new Padding(0,0,5,0),BackColor=Surface};var caption=new System.Windows.Forms.Label{Text=title,Dock=DockStyle.Bottom,Height=21,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(83,103,91)};panel.Controls.Add(caption);panel.Paint+=(s,e)=>{using(var p=new Pen(Color.FromArgb(212,225,216)))e.Graphics.DrawLine(p,panel.Width-1,6,panel.Width-1,panel.Height-8);};parent.Controls.Add(panel);return panel;}
  public static void Place(Control parent,Control child,int x,int y,int w,int h){child.AutoSize=false;child.Dock=DockStyle.None;child.Margin=Padding.Empty;parent.Controls.Add(child);child.SetBounds(x,y,w,h);child.BringToFront();}
  public static void Large(Button button,string text){button.Text=text;button.Width=60;button.Height=75;button.Image=ResizeIcon(button.Image,28);button.TextImageRelation=TextImageRelation.ImageAboveText;button.ImageAlign=ContentAlignment.TopCenter;button.TextAlign=ContentAlignment.BottomCenter;button.Padding=new Padding(2,7,2,5);button.FlatAppearance.BorderSize=0;button.BackColor=Surface;button.Margin=new Padding(1,0,1,0);}
  static Bitmap ResizeIcon(Image im,int size){var b=new Bitmap(size,size);using(var g=Graphics.FromImage(b)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;if(im!=null)g.DrawImage(im,0,0,size,size);}return b;}
  public static void Toggle(CheckBox box,string caption,string hint,FontStyle style){box.Appearance=Appearance.Button;box.AutoSize=false;box.Text=caption;box.TextAlign=ContentAlignment.MiddleCenter;box.FlatStyle=FlatStyle.Flat;box.FlatAppearance.BorderSize=0;box.Font=new Font("Segoe UI",11,style);box.BackColor=box.Checked?Active:Surface;box.CheckedChanged+=(s,e)=>box.BackColor=box.Checked?Active:Surface;Tips.SetToolTip(box,hint);}
  static Bitmap AlignIcon(int index){var b=new Bitmap(20,20);using(var g=Graphics.FromImage(b))using(var p=new Pen(Color.FromArgb(42,85,65),1.6f)){for(int i=0;i<4;i++){int w=i%2==0?16:10,x=index==0?2:index==1?(20-w)/2:18-w;g.DrawLine(p,x,4+i*4,x+w,4+i*4);}}return b;}
  public static void Alignment(Panel parent,ComboBox choice){for(int i=0;i<3;i++){int index=i;var b=new Button{Image=AlignIcon(i),FlatStyle=FlatStyle.Flat,BackColor=Surface};b.FlatAppearance.BorderSize=0;Place(parent,b,7+i*33,7,31,30);Tips.SetToolTip(b,new[]{"Alinear a la izquierda","Centrar texto","Alinear a la derecha"}[i]);b.Click+=(s,e)=>choice.SelectedIndex=index;Action sync=()=>{b.Enabled=choice.Enabled;b.BackColor=choice.SelectedIndex==index?Active:Surface;};choice.SelectedIndexChanged+=(s,e)=>sync();choice.EnabledChanged+=(s,e)=>sync();sync();}choice.Visible=false;parent.Controls.Add(choice);}
 }
}
