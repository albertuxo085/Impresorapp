using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EtiquetaLocal {
 public static class DirectTests {
  static void Assert(bool value,string message){if(!value)throw new Exception(message);}
  public static void Run(bool network=false){
   using(var b=new Bitmap(9,2,PixelFormat.Format24bppRgb)){
    using(var g=Graphics.FromImage(b))g.Clear(Color.White);b.SetPixel(0,0,Color.Black);b.SetPixel(7,0,Color.Black);b.SetPixel(8,1,Color.Black);
    byte[] bits=Direct.Raster(b);Assert(bits.Length==4&&bits[0]==0x81&&bits[1]==0&&bits[2]==0&&bits[3]==0x80,"Black-bit polarity, row padding and bit order");
   }
   int width=800,height=1600,row=100;byte[] raster=new byte[row*height];for(int i=0;i<raster.Length;i++)raster[i]=(byte)(i%251);
   string zpl=Encoding.ASCII.GetString(Direct.Zpl(raster,width,height,3));Assert(zpl.StartsWith("^XA")&&zpl.EndsWith("^PQ3,0,1,N^XZ"),"ZPL envelope and copies");
   var fields=Regex.Matches(zpl,@"\^FO0,(\d+)\^GFA,(\d+),(\d+),(\d+),([0-9A-F]+)\^FS");Assert(fields.Count==3,"Large graphics split into bounded fields");
   var restored=new List<byte>();int nextY=0;foreach(Match m in fields){int y=int.Parse(m.Groups[1].Value),count=int.Parse(m.Groups[2].Value);Assert(y==nextY&&count<=99999&&count==int.Parse(m.Groups[3].Value)&&int.Parse(m.Groups[4].Value)==row,"Field limits and coordinates");string hex=m.Groups[5].Value;Assert(hex.Length==count*2,"Hex byte count");for(int i=0;i<hex.Length;i+=2)restored.Add(Convert.ToByte(hex.Substring(i,2),16));nextY+=count/row;}
   Assert(nextY==height&&restored.Count==raster.Length,"Raster dimensions restored");for(int i=0;i<raster.Length;i++)Assert(raster[i]==restored[i],"Raster round trip");
   if(network){var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();try{
    int port=((IPEndPoint)listener.LocalEndpoint).Port;byte[] sent=Encoding.ASCII.GetBytes(zpl);
    var receive=Task.Run(()=>{using(var client=listener.AcceptTcpClient()){client.ReceiveTimeout=5000;using(var stream=client.GetStream())using(var ms=new MemoryStream()){stream.CopyTo(ms);return ms.ToArray();}}});
    Direct.Send("127.0.0.1",port,sent);Assert(receive.Wait(6000),"TCP receive completion");var actual=receive.Result;Assert(actual.Length==sent.Length,"TCP payload length");for(int i=0;i<sent.Length;i++)Assert(sent[i]==actual[i],"TCP bytes preserved");
   }finally{listener.Stop();}}
   bool rejected=false;try{Direct.Build(new Label(),8,0);}catch{rejected=true;}Assert(rejected,"Reject zero copies");
   File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ejemplo_100x50.zpl"),Direct.Build(new Label{Elements=new List<Element>{new Element{Text="ETIQUETA DE PRUEBA",X=5,Y=5,W=90,H=10,Bold=true,FontSize=16},new Element{Kind="Código 39",Text="AZOR001",X=5,Y=20,W=90,H=15}}},8,1));
  }
 }
}
