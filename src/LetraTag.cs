using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace EtiquetaLocal {
 public static class LTProtocol {
  public static byte[] Pixels(Bitmap b){if(b.Height!=30||b.Width<1||b.Width>2000)throw new Exception("LetraTag: mapa de 30 filas y hasta 2000 columnas.");var output=new byte[b.Width*4];for(int x=0;x<b.Width;x++)for(int y=0;y<30;y++){var c=b.GetPixel(x,y);if(c.R*299+c.G*587+c.B*114<128000){int row=y+1;output[x*4+3-row/8]|=(byte)(128>>(row%8));}}return output;}
  public static byte[] Body(Bitmap b){var pixels=Pixels(b);var data=new List<byte>();data.AddRange(new byte[]{27,115,154,2,0,0,27,35,1,27,68,129,2});data.AddRange(BitConverter.GetBytes(b.Width));data.AddRange(BitConverter.GetBytes(32));data.AddRange(pixels);data.AddRange(new byte[]{27,112,48,27,65,27,81});return data.ToArray();}
  public static List<byte[]> Pack(byte[] body,int maxWrite){if(maxWrite<20)throw new Exception("El enlace Bluetooth no admite paquetes de 20 bytes.");int size=Math.Min(500,maxWrite-3);int count=(body.Length+size-1)/size;if(count>255)throw new Exception("Etiqueta demasiado larga para el enlace Bluetooth negociado. Reduce la longitud de cinta.");var packets=new List<byte[]>();var header=new byte[]{255,240,18,52,0,0,0,0,0};Array.Copy(BitConverter.GetBytes(body.Length),0,header,4,4);header[8]=(byte)(header.Take(8).Sum(x=>(int)x)&255);packets.Add(header);for(int i=0;i<count;i++){bool last=i==count-1;int n=Math.Min(size,body.Length-i*size);byte[] packet=new byte[n+1+(last?2:0)];packet[0]=(byte)(i>=27?i+1:i);Array.Copy(body,i*size,packet,1,n);if(last){packet[packet.Length-2]=18;packet[packet.Length-1]=52;}packets.Add(packet);}return packets;}
  public static Bitmap Text(string value,int lengthMm){var b=new Bitmap((int)Math.Round(lengthMm*200/25.4),30,PixelFormat.Format24bppRgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;using(var font=new Font("Arial",23,FontStyle.Bold,GraphicsUnit.Pixel))using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){var size=g.MeasureString(value,font);if(size.Width>b.Width-70){b.Dispose();throw new Exception("El texto no cabe: reduce el texto o amplía la longitud.");}g.DrawString(value,font,Brushes.Black,new RectangleF(20,0,b.Width-67,30),sf);}}return b;}
  public static void SelfTest(){using(var b=new Bitmap(2,30)){using(var g=Graphics.FromImage(b))g.Clear(Color.White);b.SetPixel(0,0,Color.Black);b.SetPixel(1,29,Color.Black);var p=Pixels(b);if(p[3]!=64||p[4]!=2)throw new Exception("Orden de bits LetraTag.");}
   byte[] body=new byte[1500];for(int i=0;i<body.Length;i++)body[i]=(byte)(i%251);foreach(int max in new[]{20,180,500}){var packets=Pack(body,max);var result=new List<byte>();for(int i=1;i<packets.Count;i++){if(packets[i].Length>max)throw new Exception("Límite de paquete.");int position=i-1;if(packets[i][0]!=(position>=27?position+1:position))throw new Exception("Índice de paquete.");int n=packets[i].Length-1-(i==packets.Count-1?2:0);result.AddRange(packets[i].Skip(1).Take(n));}if(!result.SequenceEqual(body))throw new Exception("Reconstrucción de datos.");if(packets[0][8]!=(packets[0].Take(8).Sum(v=>(int)v)&255))throw new Exception("Checksum.");}
  }
 }
 public static class LTAsync {
  public static async Task<T> Wait<T>(IAsyncOperation<T> operation,CancellationToken token,int milliseconds){var started=DateTime.UtcNow;try{while(operation.Status==AsyncStatus.Started){token.ThrowIfCancellationRequested();if((DateTime.UtcNow-started).TotalMilliseconds>milliseconds){operation.Cancel();throw new TimeoutException("La operación Bluetooth no respondió a tiempo.");}await Task.Delay(75,token);}token.ThrowIfCancellationRequested();return operation.GetResults();}finally{if(operation.Status==AsyncStatus.Started)operation.Cancel();else operation.Close();}}
 }
 public class LTDevice {public ulong Address;public string Name;public override string ToString(){return Name+" · "+Address.ToString("X12");}}
 public class LetraTagPanel:UserControl {
  readonly Func<Bitmap> getDesign;ComboBox devices=new ComboBox();TextBox log=new TextBox();Button search=new Button(),print=new Button(),stop=new Button();BluetoothLEAdvertisementWatcher watcher;CancellationTokenSource cts;bool closing;List<LTDevice> found=new List<LTDevice>();
  public event Action SelectionChanged;
  public string SelectedName {get{return HasSelection?devices.SelectedItem.ToString():"Sin seleccionar · Busca tu LetraTag";}}
  public bool HasSelection {get{return devices.SelectedItem is LTDevice;}}
  public Task PrintCurrent(){return Print();}
  public LetraTagPanel(Func<Bitmap> buildDesign){getDesign=buildDesign;Dock=DockStyle.Bottom;Height=225;BackColor=Color.FromArgb(238,245,250);Font=new Font("Segoe UI",9);
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(6),WrapContents=true};
   var caption=new System.Windows.Forms.Label{Text="DYMO LetraTag 200B · Se imprime el diseño actual del lienzo de arriba.",AutoSize=true};top.Controls.Add(caption);top.SetFlowBreak(caption,true);
   search.Text="Buscar DYMO (12 s)";search.AutoSize=true;search.Click+=async(s,e)=>await Search();top.Controls.Add(search);
   devices.SelectedIndexChanged+=(s,e)=>{if(SelectionChanged!=null)SelectionChanged();};devices.Width=220;devices.DropDownStyle=ComboBoxStyle.DropDownList;top.Controls.Add(devices);
   print.Text="Imprimir diseño actual";print.AutoSize=true;print.Click+=async(s,e)=>await Print();top.Controls.Add(print);
   stop.Text="Detener";stop.AutoSize=true;stop.Enabled=false;stop.Click+=(s,e)=>{if(cts!=null)cts.Cancel();if(watcher!=null)watcher.Stop();};top.Controls.Add(stop);
   var save=new Button{Text="Guardar diagnóstico…",AutoSize=true};save.Click+=(s,e)=>{using(var d=new SaveFileDialog{Filter="Diagnóstico|*.txt",FileName="diagnostico_letratag.txt"})if(d.ShowDialog()==DialogResult.OK)File.WriteAllText(d.FileName,log.Text,Encoding.UTF8);};top.Controls.Add(save);
   log.Dock=DockStyle.Fill;log.Multiline=true;log.ScrollBars=ScrollBars.Vertical;log.ReadOnly=true;log.BackColor=Color.White;Controls.Add(log);Controls.Add(top);
   Log("Un único editor. Modifica el texto en el lienzo y pulsa Imprimir diseño actual. La búsqueda no imprime.");
  }
  protected override void Dispose(bool disposing){if(disposing){closing=true;if(cts!=null)cts.Cancel();if(watcher!=null)watcher.Stop();}base.Dispose(disposing);}
  void Log(string value){if(closing||IsDisposed)return;log.AppendText(DateTime.Now.ToString("HH:mm:ss")+" "+value+Environment.NewLine);}
  bool Begin(){if(cts!=null)return false;cts=new CancellationTokenSource();search.Enabled=print.Enabled=false;stop.Enabled=true;return true;}
  void End(){if(cts!=null){cts.Dispose();cts=null;}if(closing)return;search.Enabled=print.Enabled=true;stop.Enabled=false;}
  async Task Search(){if(!Begin())return;var token=cts.Token;found.Clear();devices.Items.Clear();Log("Buscando anuncios Bluetooth LetraTag durante 12 segundos…");
   try{watcher=new BluetoothLEAdvertisementWatcher{ScanningMode=BluetoothLEScanningMode.Active};watcher.Received+=(s,e)=>{string name=e.Advertisement.LocalName??"";bool match=name.StartsWith("Letratag",StringComparison.OrdinalIgnoreCase)||name.StartsWith("DYMO LT-200B",StringComparison.OrdinalIgnoreCase)||e.Advertisement.ServiceUuids.Any(u=>u.ToString().StartsWith("be3dd650",StringComparison.OrdinalIgnoreCase));if(!match||closing)return;try{BeginInvoke(new Action(()=>{if(closing||cts==null)return;if(found.Any(item=>item.Address==e.BluetoothAddress))return;var d=new LTDevice{Address=e.BluetoothAddress,Name=String.IsNullOrEmpty(name)?"LetraTag BLE":name};found.Add(d);devices.Items.Add(d);if(devices.SelectedIndex<0)devices.SelectedIndex=0;Log("Detectada "+d);}));}catch(InvalidOperationException){}};watcher.Start();await Task.Delay(12000,token);Log("Búsqueda terminada: "+found.Count+" unidad(es). Si no aparece, acércala y comprueba que no esté conectada al móvil.");}
   catch(OperationCanceledException){Log("Búsqueda detenida.");}catch(Exception ex){Log("No se pudo buscar: "+ex.Message);}finally{if(watcher!=null){watcher.Stop();watcher=null;}End();}
  }
  async Task Write(GattCharacteristic tx,byte[] data,CancellationToken token){using(var writer=new DataWriter()){writer.WriteBytes(data);var status=await LTAsync.Wait(tx.WriteValueAsync(writer.DetachBuffer(),GattWriteOption.WriteWithoutResponse),token,8000);if(status!=GattCommunicationStatus.Success)throw new Exception("Error al escribir por Bluetooth: "+status);}}
  async Task Print(){if(cts!=null){Log("Espera a que termine la operación actual.");return;}var selected=devices.SelectedItem as LTDevice;if(selected==null){MessageBox.Show("Busca y selecciona tu LetraTag antes de imprimir.");return;}Bitmap image=null;BluetoothLEDevice device=null;GattSession session=null;List<GattDeviceService> opened=new List<GattDeviceService>();
   try{image=getDesign();if(MessageBox.Show("Se enviará UNA etiqueta a "+selected.Name+".\nComprueba que hay cinta LT de 12 mm y que la salida está libre.\n\n¿Imprimir ahora?","Prueba LetraTag",MessageBoxButtons.OKCancel)!=DialogResult.OK){image.Dispose();return;}if(!Begin()){image.Dispose();return;}var token=cts.Token;Log("Conectando a "+selected.Address.ToString("X12")+"…");
    device=await LTAsync.Wait(BluetoothLEDevice.FromBluetoothAddressAsync(selected.Address),token,15000);if(device==null)throw new Exception("No se pudo abrir el dispositivo. Comprueba Bluetooth y permisos de Windows.");
    var services=await LTAsync.Wait(device.GetGattServicesAsync(BluetoothCacheMode.Uncached),token,20000);if(services.Status!=GattCommunicationStatus.Success)throw new Exception("No se pudieron consultar los servicios: "+services.Status);opened.AddRange(services.Services);var service=opened.FirstOrDefault(v=>v.Uuid.ToString().StartsWith("be3dd650",StringComparison.OrdinalIgnoreCase));if(service==null)throw new Exception("El dispositivo no anuncia el servicio LetraTag esperado.");Log("Servicio "+service.Uuid);
    var chars=await LTAsync.Wait(service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached),token,15000);if(chars.Status!=GattCommunicationStatus.Success)throw new Exception("No se pudieron consultar las características.");var tx=chars.Characteristics.FirstOrDefault(c=>c.Uuid.ToString().StartsWith("be3dd651",StringComparison.OrdinalIgnoreCase));var rx=chars.Characteristics.FirstOrDefault(c=>c.Uuid.ToString().StartsWith("be3dd652",StringComparison.OrdinalIgnoreCase));if(tx==null||rx==null)throw new Exception("Faltan canales de envío o respuesta.");
    var reply=new TaskCompletionSource<byte>();rx.ValueChanged+=(s,e)=>{using(var reader=DataReader.FromBuffer(e.CharacteristicValue)){var bytes=new byte[reader.UnconsumedBufferLength];reader.ReadBytes(bytes);for(int i=0;i+2<bytes.Length;i++)if(bytes[i]==27&&bytes[i+1]==82)reply.TrySetResult(bytes[i+2]);}};
    var subscribe=await LTAsync.Wait(rx.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify),token,8000);if(subscribe!=GattCommunicationStatus.Success)throw new Exception("No se pudieron activar las respuestas: "+subscribe);
    session=await LTAsync.Wait(GattSession.FromDeviceIdAsync(device.BluetoothDeviceId),token,8000);int maxWrite=session==null?20:Math.Max(20,Math.Min(503,(int)session.MaxPduSize-3));var packets=LTProtocol.Pack(LTProtocol.Body(image),maxWrite);Log("Mapa "+image.Width+" × 30; máximo de escritura "+maxWrite+" bytes; "+packets.Count+" paquetes.");
    for(int i=0;i<packets.Count;i++){token.ThrowIfCancellationRequested();await Write(tx,packets[i],token);await Task.Delay(35,token);if(i==0||i==packets.Count-1||i%10==0)Log("Envío "+(i+1)+"/"+packets.Count);}
    Log("Datos enviados. Esperando respuesta de la impresora…");var timeout=Task.Delay(90000,token);var completed=await Task.WhenAny(reply.Task,timeout);token.ThrowIfCancellationRequested();if(completed!=reply.Task)throw new TimeoutException("No llegó confirmación. Comprueba si salió la etiqueta antes de repetir.");byte code=await reply.Task;Log("Respuesta ESC R: "+code+" (0x"+code.ToString("X2")+").");if(code==0)Log("La impresora informó de finalización. Comprueba la impresión y corta con el botón físico.");else Log("Respuesta de variante no validada: comprueba físicamente el resultado. No se reintenta.");
   }catch(OperationCanceledException){Log("Operación detenida. Puede haberse impreso una parte: revisa la cinta antes de repetir.");}catch(Exception ex){Log("No se confirma la impresión: "+ex.GetBaseException().Message);if(!closing)MessageBox.Show(ex.GetBaseException().Message,"Prueba LetraTag");}finally{foreach(var service in opened)service.Dispose();if(session!=null)session.Dispose();if(device!=null)device.Dispose();if(image!=null)image.Dispose();End();}
  }
 }
}
