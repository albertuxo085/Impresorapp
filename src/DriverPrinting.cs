using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace EtiquetaLocal {
 [DataContract] public class DriverSelection { [DataMember] public string Printer="";[DataMember] public bool Enabled; }
 public static class DriverPrinting {
  public static event Action SelectionChanged;
  static string SettingsPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtiquetaLocal","driver-printer.json");}}
  public static DriverSelection Load(){try{using(var f=File.OpenRead(SettingsPath))return (DriverSelection)new DataContractJsonSerializer(typeof(DriverSelection)).ReadObject(f)??new DriverSelection();}catch{return new DriverSelection();}}
  static void Save(DriverSelection state){using(var ms=new MemoryStream()){new DataContractJsonSerializer(typeof(DriverSelection)).WriteObject(ms,state);Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));DesignStorage.Write(SettingsPath,System.Text.Encoding.UTF8.GetString(ms.ToArray()));}if(SelectionChanged!=null)SelectionChanged();}
  public static void UseDirect(){var state=Load();state.Enabled=false;Save(state);}
  public static void Select(IWin32Window owner,Label label){try{ChooseLocal(owner,label,true);}catch(Exception ex){MessageBox.Show(owner,ex.Message,"Impresoras instaladas");}}
  public static int Units(float mm){return (int)Math.Round(mm/25.4*100);}
  public static bool SizeMatches(float widthMm,float heightMm,float pageWidthMm,float pageHeightMm){return Math.Abs(widthMm-pageWidthMm)<=1&&Math.Abs(heightMm-pageHeightMm)<=1;}
  public static PaperSize MatchPaper(PaperSize[] papers,Label label){return papers.FirstOrDefault(p=>SizeMatches(label.Width,label.Height,p.Width*.254f,p.Height*.254f));}
  static void SetSize(PrintDocument doc,Label label){doc.DefaultPageSettings.Landscape=false;doc.DefaultPageSettings.Margins=new Margins(0,0,0,0);doc.OriginAtMargins=false;doc.DefaultPageSettings.PaperSize=new PaperSize("Impresorapp "+label.Width+" x "+label.Height+" mm",Units(label.Width),Units(label.Height));}
  public static void DrawPage(Graphics graphics,Label label,float hardXmm,float hardYmm){var saved=graphics.Save();try{graphics.PageUnit=GraphicsUnit.Millimeter;graphics.TranslateTransform(Model.Valid(hardXmm)?-hardXmm:0,Model.Valid(hardYmm)?-hardYmm:0);graphics.SetClip(new RectangleF(0,0,label.Width,label.Height),CombineMode.Intersect);Render.Draw(graphics,label);}finally{graphics.Restore(saved);}}
  // Flatten only the physical label, not the entire oversized source image. Preview and spooler
  // receive exactly the same cropped pixels; driver-private offsets still apply afterwards.
  public static Bitmap Rasterize(Label label,int dpi){
   int width=(int)Math.Round(label.Width*dpi/25.4),height=(int)Math.Round(label.Height*dpi/25.4);
   if(width<1||height<1||(long)width*height>40000000)throw new Exception("El formato es demasiado grande para esa resolución.");
   var bitmap=new Bitmap(width,height,PixelFormat.Format24bppRgb);bitmap.SetResolution(dpi,dpi);
   try{using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.PageUnit=GraphicsUnit.Millimeter;g.SetClip(new RectangleF(0,0,label.Width,label.Height));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;g.SmoothingMode=SmoothingMode.None;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;Render.Draw(g,Sharpness.Prepare(label,dpi));}Sharpness.Binarize(bitmap);return bitmap;}catch{bitmap.Dispose();throw;}
  }
  public static void DrawRaster(Graphics graphics,Bitmap bitmap,Label label,float hardXmm,float hardYmm){var state=graphics.Save();try{graphics.PageUnit=GraphicsUnit.Millimeter;graphics.TranslateTransform(Model.Valid(hardXmm)?-hardXmm:0,Model.Valid(hardYmm)?-hardYmm:0);graphics.SetClip(new RectangleF(0,0,label.Width,label.Height),CombineMode.Intersect);graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(bitmap,new RectangleF(0,0,label.Width,label.Height),new RectangleF(0,0,bitmap.Width,bitmap.Height),GraphicsUnit.Pixel);}finally{graphics.Restore(state);}}
  static void TracePrint(string text){try{var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtiquetaLocal");Directory.CreateDirectory(folder);var path=Path.Combine(folder,"ultima-impresion.log");File.AppendAllText(path,DateTime.Now.ToString("O")+" "+text+Environment.NewLine);}catch{}}
  [DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr hdc,int index);
  public static float OffsetMm(int pixels,int dpi){if(pixels<0||dpi<=0)throw new Exception("El controlador no ha proporcionado un origen físico válido.");return pixels*25.4f/dpi;}
  static PointF DeviceOrigin(Graphics graphics){var hdc=graphics.GetHdc();try{return new PointF(OffsetMm(GetDeviceCaps(hdc,112),GetDeviceCaps(hdc,88)),OffsetMm(GetDeviceCaps(hdc,113),GetDeviceCaps(hdc,90)));}finally{graphics.ReleaseHdc(hdc);}}
  // Botón Imprimir: envía directamente a la impresora guardada (1 copia), sin vista previa.
  // Si todavía no hay impresora elegida, abre la ventana de opciones.
  public static void PrintNow(IWin32Window owner,Label original){
   Model.Validate(original);if(original.Elements.Count==0||Model.Outside(original))throw new Exception("Añade contenido y comprueba que queda dentro de la etiqueta.");
   var saved=Load();if(!saved.Enabled||String.IsNullOrWhiteSpace(saved.Printer)){Show(owner,original);return;}
   var label=Model.Load(Model.Save(original));
   using(var pixels=Rasterize(label,203))SendResponsive(owner,new PrintChoice{Printer=saved.Printer,Copies=1},label,pixels);
  }
  public static void Show(IWin32Window owner,Label original){
   Model.Validate(original);if(original.Elements.Count==0||Model.Outside(original))throw new Exception("Añade contenido y comprueba que queda dentro de la etiqueta.");
   var label=Model.Load(Model.Save(original));var selection=ChooseLocal(owner,label,false);if(selection==null)return;
   using(var f=new Form{Text="Vista previa del diseño · "+selection.Printer,Width=1000,Height=800,StartPosition=FormStartPosition.CenterParent,Icon=Branding.AppIcon,BackColor=Color.FromArgb(242,247,244)}){
    var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=78,Padding=new Padding(10),AutoSize=false};
    var info=new System.Windows.Forms.Label{AutoSize=false,Width=690,Height=48,Text=label.Width+" × "+label.Height+" mm · Impresión al 100 % · "+selection.Copies+" copia(s)\nVista del diseño sin consultar la red. El controlador y sus desplazamientos se comprueban al enviar."};
    var print=new Button{Text="Imprimir",Width=130,Height=40,BackColor=Color.FromArgb(35,109,78),ForeColor=Color.White};
    var page=new PreviewPage(label){Dock=DockStyle.Fill};bar.Controls.Add(info);bar.Controls.Add(print);f.Controls.Add(page);f.Controls.Add(bar);
    print.Click+=(s,e)=>{print.Enabled=false;try{SendResponsive(f,selection,label,page.Pixels);}finally{print.Enabled=true;}};
    f.ShowDialog(owner);
   }
  }
  sealed class PrintChoice {public string Printer;public short Copies;}
  static string[] cachedNames;
  // Choosing a queue must not create a PrintDocument: that can contact the default server.
  static PrintChoice ChooseLocal(IWin32Window owner,Label label,bool selectOnly){
   PrintChoice result=null;var saved=Load();
   using(var f=new Form{Text="Imprimir etiquetas con controlador",ClientSize=new Size(620,330),StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",9.5f),Icon=Branding.AppIcon,BackColor=Color.FromArgb(242,247,244),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
    var title=new System.Windows.Forms.Label{Text="Impresora instalada en este equipo",Left=18,Top=15,Width=580,Height=24};
    var queue=new ComboBox{Left=18,Top=43,Width=582,DropDownStyle=ComboBoxStyle.DropDownList};
    if(!String.IsNullOrWhiteSpace(saved.Printer)){queue.Items.Add(saved.Printer);queue.SelectedIndex=0;}
    var copies=new NumericUpDown{Left=78,Top=88,Width=75,Minimum=1,Maximum=999,Value=1};
    var status=new System.Windows.Forms.Label{Text="Cargando lista de impresoras… Puedes cancelar la espera.",Left=18,Top=224,Width=580,Height=30};
    var configure=new Button{Text="Preferencias de Toshiba / fabricante…",Left=282,Top=82,Width=318,Height=36};
    configure.Click+=(s,e)=>{var name=queue.SelectedItem as string;if(String.IsNullOrWhiteSpace(name))return;try{
     if(name.IndexOfAny(new[]{'\"','\r','\n'})>=0)throw new Exception("Nombre de impresora no válido.");
     Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"rundll32.exe"),"printui.dll,PrintUIEntry /e /n \""+name+"\""){UseShellExecute=true});
     status.Text="Preferencias abiertas en otra ventana. Guarda allí antes de imprimir.";
    }catch(Exception ex){MessageBox.Show(f,ex.Message,"Preferencias de impresión");}};
    var accept=new Button{Text=selectOnly?"Usar esta impresora":"Ver vista previa",Left=281,Top=279,Width=195,Height=34};
    accept.Click+=(s,e)=>{var name=queue.SelectedItem as string;if(String.IsNullOrWhiteSpace(name)){MessageBox.Show(f,"Selecciona una impresora instalada.");return;}try{Save(new DriverSelection{Printer=name,Enabled=true});result=new PrintChoice{Printer=name,Copies=(short)copies.Value};f.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(f,ex.Message);}};
    var cancel=new Button{Text="Cancelar",Left=487,Top=279,Width=112,Height=34,DialogResult=DialogResult.Cancel};
    f.Controls.AddRange(new Control[]{title,queue,copies,configure,accept,cancel,status,new System.Windows.Forms.Label{Text="Copias",Left=18,Top=94,Width=60},new System.Windows.Forms.Label{Text="Etiqueta: "+label.Width+" × "+label.Height+" mm · Tamaño real (100 %)\n\nLa vista previa se genera en el equipo, sin esperar al servidor. Las preferencias del fabricante se abren por separado.",Left=18,Top=135,Width=580,Height=85}});f.CancelButton=cancel;
    Action<string[],string> showNames=(names,error)=>{if(f.IsDisposed)return;var chosen=queue.SelectedItem as string;queue.Items.Clear();queue.Items.AddRange(names);if(!String.IsNullOrWhiteSpace(chosen)&&!names.Contains(chosen))queue.Items.Add(chosen);if(queue.Items.Count>0)queue.SelectedItem=queue.Items.Contains(chosen??"")?chosen:queue.Items[0];status.Text=error??(names.Length==0?"No hay impresoras instaladas. Conecta la compartida desde Windows.":"Lista de impresoras lista.");};
    f.Shown+=(s,e)=>{if(cachedNames!=null){showNames(cachedNames,null);return;}var worker=new Thread(()=>{string[] names=new string[0];string error=null;try{names=PrinterSettings.InstalledPrinters.Cast<string>().ToArray();cachedNames=names;}catch(Exception ex){error=ex.Message;}try{if(!f.IsDisposed)f.BeginInvoke(new Action(()=>showNames(names,error)));}catch(InvalidOperationException){}}){IsBackground=true};worker.SetApartmentState(ApartmentState.STA);worker.Start();};
    return f.ShowDialog(owner)==DialogResult.OK?result:null;
   }
  }
  sealed class PreviewPage:Panel {
   readonly Label label;
   public readonly Bitmap Pixels;
   public PreviewPage(Label value){label=value;Pixels=Rasterize(label,203);DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.FromArgb(222,229,225);}
   protected override void Dispose(bool disposing){if(disposing)Pixels.Dispose();base.Dispose(disposing);}
   protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);float scale=Math.Min(Math.Max(1,ClientSize.Width-36)/label.Width,Math.Max(1,ClientSize.Height-36)/label.Height);var state=e.Graphics.Save();try{
    // Set units before the transform: changing PageUnit later also scales the translation.
    float pxPerMmX=e.Graphics.DpiX/25.4f,pxPerMmY=e.Graphics.DpiY/25.4f;e.Graphics.PageUnit=GraphicsUnit.Millimeter;
    e.Graphics.TranslateTransform((ClientSize.Width-label.Width*scale)/2/pxPerMmX,(ClientSize.Height-label.Height*scale)/2/pxPerMmY);
    e.Graphics.ScaleTransform(scale/pxPerMmX,scale/pxPerMmY);e.Graphics.FillRectangle(Brushes.White,0,0,label.Width,label.Height);e.Graphics.SetClip(new RectangleF(0,0,label.Width,label.Height));DrawRaster(e.Graphics,Pixels,label,0,0);
   }finally{e.Graphics.Restore(state);}}
  }
  // Cancellation is allowed only before the job is committed to the Windows spooler.
  public sealed class SubmissionGate {
   readonly object sync=new object();bool cancelled,submitted;
   public bool Cancel(){lock(sync){if(submitted)return false;cancelled=true;return true;}}
   public bool Commit(){lock(sync){if(cancelled||submitted)return false;submitted=true;return true;}}
  }
  static void ResponsiveSelfTest(){if(Math.Abs(OffsetMm(8,203)-1.000985f)>.001f||OffsetMm(0,203)!=0)throw new Exception("Origen del contexto GDI");
   var cancelled=new SubmissionGate();if(!cancelled.Cancel()||cancelled.Commit())throw new Exception("Cancelar debe impedir un envío posterior");
   var committed=new SubmissionGate();if(!committed.Commit()||committed.Cancel())throw new Exception("Un trabajo enviado no admite una cancelación ficticia");
   var label=new Label{Width=50,Height=30};label.Elements.Add(new Element{Kind="Marco",X=5,Y=5,W=10,H=10,Stroke=1});
   using(var panel=new PreviewPage(label){Size=new Size(400,280)})using(var bitmap=new Bitmap(400,280)){
    panel.DrawToBitmap(bitmap,new Rectangle(0,0,400,280)); bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview-driver-test.png"));
    // Fit scale is 7.28 px/mm. The frame's left edge is at 54.4 px, inside a white page.
    if(bitmap.GetPixel(56,90).R>128||bitmap.GetPixel(80,90).R<240||bitmap.GetPixel(140,90).R<240)throw new Exception("Escala y posición de la vista previa local");
   }
   var crop=new Label{Width=50,Height=30};using(var source=new Bitmap(20,20))using(var stream=new MemoryStream()){
    using(var g=Graphics.FromImage(source))g.Clear(Color.Black);source.Save(stream,ImageFormat.Png);
    crop.Elements.Add(new Element{Kind="Imagen",X=-10,Y=-10,W=70,H=50,ImageData=Convert.ToBase64String(stream.ToArray())});
   }
   using(var panel=new PreviewPage(crop){Size=new Size(400,280)})using(var bitmap=new Bitmap(400,280)){
    panel.DrawToBitmap(bitmap,new Rectangle(0,0,400,280));
    if(bitmap.GetPixel(10,90).R!=222||bitmap.GetPixel(200,15).R!=222||bitmap.GetPixel(200,265).R!=222||bitmap.GetPixel(200,140).R>10)throw new Exception("Imagen sobredimensionada sin recorte al contorno");
   }
   using(var raster=Rasterize(label,203))using(var target=new Bitmap(raster.Width,raster.Height)){
    target.SetResolution(203,203);using(var g=Graphics.FromImage(target)){g.Clear(Color.White);g.PageUnit=GraphicsUnit.Millimeter;g.TranslateTransform(7,3);DrawRaster(g,raster,label,7,3);}
    if(target.GetPixel(42,80).R>128||target.GetPixel(100,80).R<240)throw new Exception("Compensación del origen físico sin mover el diseño");
   }
  }
  static void SendResponsive(IWin32Window owner,PrintChoice selection,Label label,Bitmap previewPixels){
   // Own a copy: cancelling preparation may dispose the preview before the worker returns.
   var pixels=(Bitmap)previewPixels.Clone();
   using(var f=new Form{Text="Enviando etiqueta",ClientSize=new Size(560,190),StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",10),Icon=Branding.AppIcon,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
    var gate=new SubmissionGate();bool finished=false;var status=new System.Windows.Forms.Label{Left=18,Top=18,Width=524,Height=95,Text="Consultando el controlador y el servidor…\n"+selection.Printer+"\nPuedes cancelar antes de que se envíe el trabajo."};
    var cancel=new Button{Text="Cancelar espera",Left=350,Top=138,Width=190,Height=33};f.Controls.AddRange(new Control[]{status,cancel});
    f.FormClosing+=(s,e)=>{if(!finished&&!gate.Cancel()){e.Cancel=true;status.Text="Windows está enviando el trabajo. Espera el resultado para evitar duplicados. Para detenerlo, utiliza la cola de impresión.";}};
    cancel.Click+=(s,e)=>f.Close();
    f.Shown+=(s,e)=>{var worker=new Thread(()=>{Exception failure=null;bool sent=false;try{
     TracePrint("INICIO "+selection.Printer+" formato="+label.Width+"x"+label.Height+" raster="+pixels.Width+"x"+pixels.Height);
     using(pixels)using(var doc=new PrintDocument()){
      doc.DocumentName="Etiqueta · Impresorapp";doc.PrinterSettings.PrinterName=selection.Printer;
      // PrintDocument validates the queue when printing. Do not add an extra remote validation
      // or enumerate server forms: this job's layout is explicitly supplied by the application.
      TracePrint("Aplicando medidas del diseño, sin consultar formatos del servidor");
      SetSize(doc,label);doc.PrinterSettings.Copies=selection.Copies;doc.PrintController=new StandardPrintController();
      doc.PrintPage+=(ps,pe)=>{TracePrint("PrintPage entrada pagina="+pe.PageBounds);if(!SizeMatches(label.Width,label.Height,pe.PageBounds.Width*.254f,pe.PageBounds.Height*.254f))throw new Exception("El controlador ha devuelto un tamaño distinto. Configura "+label.Width+" × "+label.Height+" mm en sus preferencias. No se escala automáticamente.");var origin=DeviceOrigin(pe.Graphics);TracePrint("Origen del contexto mm="+origin);DrawRaster(pe.Graphics,pixels,label,origin.X,origin.Y);pe.HasMorePages=false;TracePrint("PrintPage dibujada");};
      if(gate.Commit()){try{f.BeginInvoke(new Action(()=>{cancel.Enabled=false;status.Text="Enviando a la cola de Windows… No repitas la impresión.\n"+selection.Printer;}));}catch(InvalidOperationException){}TracePrint("Print llamada");doc.Print();sent=true;TracePrint("Print terminada");}
     }
    }catch(Exception ex){pixels.Dispose();failure=ex;TracePrint("ERROR "+ex);}
    try{if(!f.IsDisposed)f.BeginInvoke(new Action(()=>{finished=true;if(failure!=null)MessageBox.Show(f,failure.Message+"\nComprueba la cola antes de reintentar: el envío podría haber comenzado.","No se ha confirmado el envío");else if(sent)MessageBox.Show(f,"Trabajo entregado a Windows. Comprueba la salida física de la impresora.","Etiqueta enviada");f.Close();}));}catch(InvalidOperationException){}
    }){IsBackground=true};worker.SetApartmentState(ApartmentState.STA);worker.Start();};f.ShowDialog(owner);
   }
  }
  public static void SelfTest(){ResponsiveSelfTest();var selected=new DriverSelection{Printer=@"\\servidor\Toshiba almacén",Enabled=true};using(var ms=new MemoryStream()){var serializer=new DataContractJsonSerializer(typeof(DriverSelection));serializer.WriteObject(ms,selected);ms.Position=0;var restored=(DriverSelection)serializer.ReadObject(ms);if(!restored.Enabled||restored.Printer!=selected.Printer)throw new Exception("Persistencia de cola compartida");}if(Units(50)!=197||!SizeMatches(50,50,50.038f,50.038f)||SizeMatches(50,50,210,297))throw new Exception("Medidas del controlador");var l=new Label{Width=50,Height=30};var custom=MatchPaper(new[]{new PaperSize("A4",827,1169),new PaperSize("50x30",197,118)},l);if(custom==null||custom.PaperName!="50x30")throw new Exception("Formato de etiqueta");l.Elements.Add(new Element{Kind="Marco",X=5,Y=5,W=10,H=10});using(var b=new Bitmap(400,240))using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.PageUnit=GraphicsUnit.Millimeter;g.ScaleTransform(8*25.4f/g.DpiX,8*25.4f/g.DpiY);DrawPage(g,l,0,0);if(b.GetPixel(40,80).R>128||b.GetPixel(120,80).R<128)throw new Exception("Impresión GDI al tamaño real");}}
 }
}
