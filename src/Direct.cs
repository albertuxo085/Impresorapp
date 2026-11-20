using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtiquetaLocal {
 public static class Direct {
  public static byte[] Raster(Bitmap bitmap){
   int stride=(bitmap.Width+7)/8;byte[] result=new byte[stride*bitmap.Height];
   var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
   try {byte[] row=new byte[Math.Abs(data.Stride)];for(int y=0;y<bitmap.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<bitmap.Width;x++){int i=x*3;if(row[i]*114+row[i+1]*587+row[i+2]*299<128000)result[y*stride+x/8]|=(byte)(128>>(x%8));}}}finally{bitmap.UnlockBits(data);}return result;
  }
  public static byte[] Build(Label label,int dpmm,int copies){
   Model.Validate(label);if(dpmm!=8&&dpmm!=12&&dpmm!=24)throw new Exception("Resolución no válida.");if(copies<1||copies>999)throw new Exception("Selecciona entre 1 y 999 copias.");
   int width=(int)Math.Round(label.Width*dpmm),height=(int)Math.Round(label.Height*dpmm);if(width<1||height<1||(long)width*height>20000000)throw new Exception("El diseño supera el límite de 20 millones de puntos. Reduce tamaño o resolución.");
   using(var bitmap=new Bitmap(width,height,PixelFormat.Format24bppRgb)){
    using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;g.ScaleTransform(dpmm,dpmm);Render.Draw(g,label);}
    return Zpl(Raster(bitmap),width,height,copies);
   }
  }
  public static byte[] Zpl(byte[] bits,int width,int height,int copies){
   int row=(width+7)/8;if(width<1||height<1||bits.Length!=row*height)throw new Exception("Tamaño de mapa de bits no válido.");
   // Each ^GF block stays below the documented 99999-byte field limit.
   int rows=Math.Max(1,60000/row);var b=new StringBuilder();b.Append("^XA^PON^PMN^LH0,0^LT0^LS0^PW").Append(width).Append("^LL").Append(height);
   for(int y=0;y<height;y+=rows){int count=Math.Min(rows,height-y)*row;b.Append("^FO0,").Append(y).Append("^GFA,").Append(count).Append(',').Append(count).Append(',').Append(row).Append(',');for(int i=0;i<count;i++)b.Append(bits[y*row+i].ToString("X2"));b.Append("^FS");}
   b.Append("^PQ").Append(copies).Append(",0,1,N^XZ");return Encoding.ASCII.GetBytes(b.ToString());
  }
  public static void Send(string host,int port,byte[] payload){
   if(String.IsNullOrWhiteSpace(host)||port<1||port>65535)throw new Exception("Introduce una dirección y un puerto válidos.");
   using(var client=new TcpClient()){var task=client.ConnectAsync(host.Trim(),port);if(!task.Wait(3000))throw new TimeoutException("No se pudo conectar en 3 segundos. Comprueba IP, puerto y conexión.");task.GetAwaiter().GetResult();client.SendTimeout=5000;using(var stream=client.GetStream()){stream.WriteTimeout=5000;stream.Write(payload,0,payload.Length);stream.Flush();}}
  }
 }
 public class DirectPrintForm:Form {
  ComboBox savedChoice=new ComboBox();System.Collections.Generic.List<SavedPrinter> favorites;TableLayoutPanel settingsTable;CheckBox advanced=new CheckBox();bool loadingPrinter;
  readonly Label label;TextBox address=new TextBox();NumericUpDown port=new NumericUpDown(),copies=new NumericUpDown(),maxWidth=new NumericUpDown();ComboBox profile=new ComboBox(),resolution=new ComboBox();CheckBox confirmed=new CheckBox();Button send=new Button(),export=new Button();System.Windows.Forms.Label info=new System.Windows.Forms.Label();
  public DirectPrintForm(Label source,string host,SavedPrinter selected=null){
   label=Model.Load(Model.Save(source));Text="Impresión directa · sin controladores";ClientSize=new Size(690,630);Font=new Font("Segoe UI",9);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterParent;
   var table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=14,Padding=new Padding(18)};table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   profile.DropDownStyle=ComboBoxStyle.DropDownList;profile.Items.AddRange(SavedPrinters.Languages.Cast<object>().ToArray());profile.SelectedIndex=0;profile.Width=400;resolution.DropDownStyle=ComboBoxStyle.DropDownList;resolution.Items.AddRange(new object[]{"8 puntos/mm (aprox. 203 dpi)","12 puntos/mm (aprox. 300 dpi)","24 puntos/mm (aprox. 600 dpi)"});resolution.SelectedIndex=0;resolution.Width=300;
   address.Text=host??"";address.Width=300;port.Minimum=1;port.Maximum=65535;port.Value=9100;copies.Minimum=1;copies.Maximum=999;copies.Value=1;maxWidth.Minimum=5;maxWidth.Maximum=1000;maxWidth.DecimalPlaces=1;maxWidth.Value=104;
   settingsTable=table;int r=0;savedChoice.DropDownStyle=ComboBoxStyle.DropDownList;savedChoice.Width=460;favorites=SavedPrinters.Load().Printers;savedChoice.Items.Add("Configuración manual / nueva impresora");foreach(var p in favorites)savedChoice.Items.Add(p);savedChoice.SelectedIndex=0;Add(table,r++,"Mis impresoras",savedChoice);advanced.Text="Ver ajustes de conexión";advanced.AutoSize=true;Add(table,r++,"Configuración",advanced);Add(table,r++,"Lenguaje (emulación Zebra)",profile);Add(table,r++,"Dirección IP o nombre",address);Add(table,r++,"Puerto TCP",port);Add(table,r++,"Resolución del cabezal",resolution);Add(table,r++,"Ancho imprimible (mm)",maxWidth);Add(table,r++,"Copias",copies);
   profile.SelectedIndexChanged+=(s,e)=>{bool toshiba=profile.SelectedIndex==1;resolution.SelectedIndex=0;resolution.Enabled=!toshiba;maxWidth.Value=toshiba?101.6m:104m;maxWidth.Enabled=!toshiba;};
   confirmed.Text="He comprobado que acepta ZPL y que el rollo/sensor están configurados.";confirmed.AutoSize=false;confirmed.Height=40;confirmed.Dock=DockStyle.Fill;table.Controls.Add(confirmed,0,r++);table.SetColumnSpan(confirmed,2);
   info.Text="Etiqueta: "+label.Width+" × "+label.Height+" mm. Estas medidas se envían en cada trabajo. Usa un rollo del mismo tamaño. Se imprime sin escalar. La Toshiba requiere emulación ZPL activa. No cambia firmware, sensores, velocidad ni oscuridad. Prueba primero una copia.";info.AutoSize=false;info.Height=70;info.Dock=DockStyle.Fill;table.Controls.Add(info,0,r++);table.SetColumnSpan(info,2);
   var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};send.Text="Enviar a impresora";send.AutoSize=true;send.Click+=async(s,e)=>await Print();export.Text="Exportar ZPL…";export.AutoSize=true;export.Click+=(s,e)=>Export();actions.Controls.Add(send);var savePrinter=new Button{Text="Guardar en Mis impresoras",AutoSize=true};savePrinter.Click+=(s,e)=>SavePrinter();actions.Controls.Add(savePrinter);actions.Controls.Add(export);table.Controls.Add(actions,0,r++);table.SetColumnSpan(actions,2);
   // Si la impresora no acepta ZPL (sin emulación Zebra) o no imprime: controlador de Windows.
   var driverBox=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,BackColor=Color.FromArgb(255,246,230),Padding=new Padding(8,6,8,6),Margin=new Padding(0,6,0,0)};
   var driverHint=new System.Windows.Forms.Label{Text="¿No tiene emulación Zebra o no imprime? Usa el controlador de la impresora:",AutoSize=true,ForeColor=Color.FromArgb(107,68,0),Margin=new Padding(0,4,0,6)};driverBox.Controls.Add(driverHint);driverBox.SetFlowBreak(driverHint,true);
   var viaWindows=new Button{Text="Imprimir con el controlador de Windows",AutoSize=true};viaWindows.Click+=(s,e)=>PrintWithWindows();driverBox.Controls.Add(viaWindows);
   var viaUpdate=new Button{Text="Buscar en Windows Update",AutoSize=true};viaUpdate.Click+=(s,e)=>DriverHelp.OpenWindowsUpdate(this);driverBox.Controls.Add(viaUpdate);
   var viaWeb=new Button{Text="Buscar controlador en Internet",AutoSize=true};viaWeb.Click+=(s,e)=>DriverHelp.SearchOnline(this,PrinterModel());driverBox.Controls.Add(viaWeb);
   table.Controls.Add(driverBox,0,r++);table.SetColumnSpan(driverBox,2);
   var status=new System.Windows.Forms.Label{Text="Una impresora USB compartida no es una conexión TCP directa. Esta versión imprime por Ethernet o Wi-Fi.",AutoSize=false,Height=40,Dock=DockStyle.Fill};table.Controls.Add(status,0,r);table.SetColumnSpan(status,2);Controls.Add(table);
   savedChoice.SelectedIndexChanged+=(s,e)=>LoadPrinter(savedChoice.SelectedItem as SavedPrinter);
   advanced.CheckedChanged+=(s,e)=>AdvancedRows();
   address.TextChanged+=(s,e)=>{if(!loadingPrinter)confirmed.Checked=false;};port.ValueChanged+=(s,e)=>{if(!loadingPrinter)confirmed.Checked=false;};
   if(selected!=null){var match=favorites.FirstOrDefault(p=>p.Id==selected.Id);if(match!=null)savedChoice.SelectedItem=match;}

  }
  void LoadPrinter(SavedPrinter p){
   loadingPrinter=true;
   bool usb=p!=null&&p.Connection=="USB";send.Enabled=!usb;
   if(p==null){confirmed.Checked=false;loadingPrinter=false;AdvancedRows();return;}
   var book=SavedPrinters.Load();book.SelectedId=p.Id;SavedPrinters.Save(book);
   profile.SelectedIndex=p.Profile;address.Text=usb?"":p.Address;port.Value=p.Port;resolution.SelectedIndex=Array.IndexOf(new[]{8,12,24},p.Dots);if(profile.SelectedIndex==0)maxWidth.Value=p.MaxWidth;confirmed.Checked=p.Configured&&!usb;
   info.Text=usb?"Esta conexión USB está guardada, pero la impresión directa USB aún no está implementada. Selecciona su conexión LAN para imprimir.":"Seleccionada: "+p.Alias+". Diseño: "+label.Width+" × "+label.Height+" mm. Comprueba que el rollo coincide.";
   loadingPrinter=false;AdvancedRows();
  }
  void AdvancedRows(){var p=savedChoice.SelectedItem as SavedPrinter;bool show=advanced.Checked||p==null||!p.Configured;settingsTable.SuspendLayout();while(settingsTable.RowStyles.Count<14)settingsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));for(int r=2;r<=6;r++){for(int c=0;c<2;c++){var control=settingsTable.GetControlFromPosition(c,r);if(control!=null)control.Visible=show;}settingsTable.RowStyles[r].SizeType=show?SizeType.AutoSize:SizeType.Absolute;settingsTable.RowStyles[r].Height=0;}settingsTable.ResumeLayout();}
  void SavePrinter(){try{
   if((savedChoice.SelectedItem as SavedPrinter)!=null&&(savedChoice.SelectedItem as SavedPrinter).Connection=="USB")throw new Exception("Selecciona una conexión LAN para guardar los ajustes de impresión de red.");
   var prior=savedChoice.SelectedItem as SavedPrinter;
   var p=new SavedPrinter{Alias=prior==null?"Mi impresora":prior.Alias,Model=prior==null?profile.Text:prior.Model,Location=prior==null?"":prior.Location,Address=address.Text.Trim(),Port=(int)port.Value,Profile=profile.SelectedIndex,Dots=new[]{8,12,24}[resolution.SelectedIndex],MaxWidth=maxWidth.Value,Configured=confirmed.Checked};
   SavedPrinters.Validate(p);if(!SavedPrinters.EditName(this,p))return;var book=SavedPrinters.Load();p=SavedPrinters.Put(book,p,true);SavedPrinters.Save(book);favorites=book.Printers;savedChoice.Items.Clear();savedChoice.Items.Add("Configuración manual / nueva impresora");foreach(var item in favorites)savedChoice.Items.Add(item);savedChoice.SelectedItem=p;info.Text="Guardada: "+p.Alias+". Sus ajustes se recuperarán al abrir de nuevo el programa.";
  }catch(Exception ex){MessageBox.Show(this,ex.Message,"Guardar impresora");}}
  void Add(TableLayoutPanel t,int row,string text,Control c){t.Controls.Add(new System.Windows.Forms.Label{Text=text,AutoSize=true,Margin=new Padding(0,6,5,0)},0,row);t.Controls.Add(c,1,row);}
  byte[] Build(){if(label.Media=="PAPER")throw new Exception("Para A4/A3 utiliza Imprimir en papel con una impresora instalada.");int dots=new[]{8,12,24}[resolution.SelectedIndex];if(label.Width>(float)maxWidth.Value)throw new Exception("La etiqueta supera el ancho imprimible seleccionado. Reduce el diseño o comprueba las especificaciones del modelo.");if(profile.SelectedIndex==1&&(label.Width<25.4f||label.Height<7.8f||label.Height>995))throw new Exception("El perfil B-FV4D-GS admite aquí anchos desde 25,4 mm y longitudes de 7,8 a 995 mm. Comprueba también el rollo.");return Direct.Build(label,dots,(int)copies.Value);}
  async Task Print(){try{
   if(!confirmed.Checked)throw new Exception("Comprueba el lenguaje, el rollo y el sensor y marca la casilla antes de enviar.");
   string target=address.Text.Trim();if(!System.Text.RegularExpressions.Regex.IsMatch(target,@"^[A-Za-z0-9][A-Za-z0-9.\-]{0,252}$"))throw new Exception("Introduce la IP o nombre de la impresora, sin barras ni puerto.");
   byte[] payload=Build();int dots=new[]{8,12,24}[resolution.SelectedIndex];string warning=label.Elements.Any(e=>e.Kind=="Código 39"&&Barcode.Module(e)*dots<2)?"\nAviso: un Código 39 tiene barras de menos de 2 puntos. Amplía su ancho y valida la lectura.":"";
   warning+=QrSupport.Warning(label,dots);
   if(MessageBox.Show("Enviar "+copies.Value+" etiqueta(s) a "+target+":"+port.Value+" con "+profile.Text+"?\nHaz primero una prueba de una copia."+warning,"Confirmar envío",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
   send.Enabled=export.Enabled=false;ControlBox=false;info.Text="Enviando…";int destinationPort=(int)port.Value;await Task.Run(()=>Direct.Send(target,destinationPort,payload));info.Text="Datos enviados por TCP. Esto no confirma que se haya impreso: comprueba la salida física y la lectura de los códigos.";
  }catch(Exception ex){info.Text="Envío no confirmado. No se reintenta automáticamente para evitar duplicados.";bool sendFailed=!send.Enabled;send.Enabled=export.Enabled=true;ControlBox=true;
    if(sendFailed){if(MessageBox.Show(this,ex.GetBaseException().Message+"\n\nSi hubo un envío parcial, revisa la impresora antes de repetir.\n\n¿Quieres imprimir con el controlador de Windows o buscar el controlador de la impresora?","Impresión directa",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)DriverHelp.Offer(this,CurrentPrinter(),PrintWithWindows,"No se ha podido enviar la etiqueta a "+address.Text.Trim()+" por la red (ZPL). Puede que la impresora no tenga activada la emulación Zebra o no esté accesible.");}
    else MessageBox.Show(this,ex.GetBaseException().Message,"Impresión directa");}finally{send.Enabled=export.Enabled=true;ControlBox=true;}}
  SavedPrinter CurrentPrinter(){var p=savedChoice.SelectedItem as SavedPrinter;return p??new SavedPrinter{Alias=String.IsNullOrWhiteSpace(address.Text)?"La impresora":address.Text.Trim(),Model=""};}
  string PrinterModel(){var p=CurrentPrinter();return String.IsNullOrWhiteSpace(p.Model)?p.Alias:p.Model;}
  void PrintWithWindows(){try{DriverPrinting.Show(this,label);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Imprimir con el controlador de Windows");}}
  void Export(){try{byte[] data=Build();using(var d=new SaveFileDialog{Filter="Instrucciones ZPL|*.zpl",FileName="etiqueta.zpl"})if(d.ShowDialog()==DialogResult.OK){File.WriteAllBytes(d.FileName,data);info.Text="ZPL exportado. No se ha enviado nada a la impresora.";}}catch(Exception ex){MessageBox.Show(ex.Message);}}
 }
}
