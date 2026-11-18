using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EtiquetaLocal {
 public class SnmpNode {public byte Tag;public byte[] Bytes;public List<SnmpNode> Children=new List<SnmpNode>();}
 public class SnmpValue {public string Oid;public byte Tag;public byte[] Data;public string Text {get{return Encoding.UTF8.GetString(Data).Trim('\0',' ','\r','\n');}}}
 public static class PrinterDiscovery {
  static int sequence=1000;
  static byte[] Join(params byte[][] arrays){return arrays.SelectMany(v=>v).ToArray();}
  static byte[] Tlv(byte tag,byte[] value){var length=new List<byte>();if(value.Length<128)length.Add((byte)value.Length);else{var n=new List<byte>();int left=value.Length;while(left>0){n.Insert(0,(byte)(left&255));left>>=8;}length.Add((byte)(128+n.Count));length.AddRange(n);}return Join(new[]{tag},length.ToArray(),value);}
  static byte[] Integer(int value){var data=new List<byte>();uint n=(uint)value;do{data.Insert(0,(byte)(n&255));n>>=8;}while(n>0);if(data[0]>=128)data.Insert(0,0);return Tlv(2,data.ToArray());}
  static byte[] OidBytes(string oid){var parts=oid.Split('.').Select(UInt32.Parse).ToArray();var data=new List<byte>();foreach(uint v in (new[]{parts[0]*40+parts[1]}).Concat(parts.Skip(2))){uint n=v;var group=new List<byte>{(byte)(n&127)};while((n>>=7)>0)group.Insert(0,(byte)((n&127)|128));data.AddRange(group);}return data.ToArray();}
  public static string DecodeOid(byte[] bytes){var values=new List<uint>();uint value=0;foreach(byte b in bytes){if(value>0x1ffffff)throw new Exception("OID fuera de rango.");value=(value<<7)|(uint)(b&127);if((b&128)==0){values.Add(value);value=0;}}if(bytes.Length==0||(bytes[bytes.Length-1]&128)!=0||values.Count==0)throw new Exception("OID incompleto.");uint first=values[0];var all=new List<uint>{first<40?0u:first<80?1u:2u,first<40?first:first<80?first-40:first-80};all.AddRange(values.Skip(1));return String.Join(".",all);}
  public static SnmpNode Parse(byte[] data){int position=0,nodes=0;var node=Read(data,ref position,data.Length,0,ref nodes);if(position!=data.Length)throw new Exception("Datos BER sobrantes.");return node;}
  static SnmpNode Read(byte[] data,ref int position,int end,int depth,ref int nodes){if(depth>10||++nodes>128||position+2>end)throw new Exception("Respuesta BER no válida.");byte tag=data[position++];int length=data[position++];if((length&128)!=0){int count=length&127;if(count<1||count>3||position+count>end)throw new Exception("Longitud BER no válida.");length=0;for(int i=0;i<count;i++)length=(length<<8)|data[position++];}if(length<0||length>end-position)throw new Exception("Respuesta BER truncada.");var node=new SnmpNode{Tag=tag,Bytes=data.Skip(position).Take(length).ToArray()};int stop=position+length;if((tag&32)!=0){while(position<stop)node.Children.Add(Read(data,ref position,stop,depth+1,ref nodes));}else position=stop;return node;}
  static int Number(SnmpNode node){if(node.Tag!=2||node.Bytes.Length>4)throw new Exception("Entero SNMP no válido.");int n=0;foreach(var b in node.Bytes)n=(n<<8)|b;return n;}
  public static byte[] Request(string oid,int id,bool next){var binding=Tlv(48,Join(Tlv(6,OidBytes(oid)),Tlv(5,new byte[0])));var pdu=Tlv(next?(byte)161:(byte)160,Join(Integer(id),Integer(0),Integer(0),Tlv(48,binding)));return Tlv(48,Join(Integer(0),Tlv(4,Encoding.ASCII.GetBytes("public")),pdu));}
  public static SnmpValue Response(byte[] bytes,int id){var root=Parse(bytes);if(root.Tag!=48||root.Children.Count!=3||Number(root.Children[0])!=0)return null;var pdu=root.Children[2];if(pdu.Tag!=162||pdu.Children.Count!=4||Number(pdu.Children[0])!=id||Number(pdu.Children[1])!=0)return null;var bindings=pdu.Children[3];if(bindings.Tag!=48||bindings.Children.Count!=1)return null;var binding=bindings.Children[0];if(binding.Children.Count!=2||binding.Children[0].Tag!=6)return null;var value=binding.Children[1];return new SnmpValue{Oid=DecodeOid(binding.Children[0].Bytes),Tag=value.Tag,Data=value.Bytes};}
  static async Task<SnmpValue> Query(string host,string oid,bool next,CancellationToken token){using(var client=new UdpClient()){try{client.Connect(host,161);int id=Interlocked.Increment(ref sequence);byte[] request=Request(oid,id,next);await client.SendAsync(request,request.Length);var receive=client.ReceiveAsync();var timeout=Task.Delay(650,token);if(await Task.WhenAny(receive,timeout)!=receive)return null;token.ThrowIfCancellationRequested();var response=await receive;return Response(response.Buffer,id);}catch(OperationCanceledException){throw;}catch{return null;}}}
  static async Task<string> Identity(string host,CancellationToken token){
   const string column="1.3.6.1.2.1.25.3.2.1.2";string cursor=column;
   for(int i=0;i<6;i++){var value=await Query(host,cursor,true,token);if(value==null)break;if(!value.Oid.StartsWith(column+".",StringComparison.Ordinal)||value.Oid==cursor)break;cursor=value.Oid;if(value.Tag==6&&DecodeOid(value.Data)=="1.3.6.1.2.1.25.3.1.5"){string index=cursor.Substring(column.Length);var descr=await Query(host,"1.3.6.1.2.1.25.3.2.1.3"+index,false,token);return descr!=null&&descr.Tag==4&&!String.IsNullOrWhiteSpace(descr.Text)?descr.Text:"Impresora identificada por SNMP";}}
   var sys=await Query(host,"1.3.6.1.2.1.1.1.0",false,token);if(sys!=null&&sys.Tag==4&&Regex.IsMatch(sys.Text,@"\b(printer|imprimante|impresora|LaserJet|DeskJet|OfficeJet|JetDirect|B[- ]?FV4[DT][A-Z0-9-]*|ZDesigner|Zebra\s+(ZT|ZD|GK|GX|ZQ)[A-Z0-9-]*)\b",RegexOptions.IgnoreCase))return sys.Text;
   return null;
  }
  public static string DisplayText(SnmpValue value){
   if(value==null||value.Tag!=4||value.Data==null)return "";string text;
   try{text=new UTF8Encoding(false,true).GetString(value.Data);}catch(DecoderFallbackException){text=Encoding.GetEncoding(1252).GetString(value.Data);}
   return new string(text.Trim('\0',' ','\r','\n').Take(512).Select(c=>Char.IsControl(c)?' ':c).ToArray());
  }
  static async Task<string> TextValue(string host,string oid,bool next,CancellationToken token){
   var value=await Query(host,oid,next,token);token.ThrowIfCancellationRequested();
   if(value==null||(next?!value.Oid.StartsWith(oid+".",StringComparison.Ordinal):value.Oid!=oid))return "";
   return DisplayText(value);
  }
  public static void SetDetails(Found printer,string systemName,string printerName,string location){
   printer.DeviceName=String.IsNullOrWhiteSpace(printerName)?systemName:printerName;
   printer.Location=location;
   printer.MetadataSource="Nombre: "+(String.IsNullOrWhiteSpace(printerName)?"SNMP sysName":"SNMP prtGeneralPrinterName")+". Ubicación: SNMP sysLocation. Los campos vacíos no se deducen del modelo ni de la IP.";
  }
  public static async Task<Found> Probe(string host,CancellationToken token){
   int[] ports={9100,8000,631,515};var identity=Identity(host,token);var hits=await Task.WhenAll(ports.Select(port=>Network.Port(host,port,token)));
   string name;try{name=await identity;}catch(OperationCanceledException){throw;}catch{name=null;}token.ThrowIfCancellationRequested();
   int[] open=ports.Where((port,index)=>hits[index]).ToArray();if(name==null&&open.Length==0)return null;
   var printer=new Found{Name=name??"Posible impresora (sin identificar)",Address=host,Kind=name==null?"Sin verificar":"Impresora",Ports=open,Verified=name!=null,Detail=(name==null?"Solo puerto abierto; identidad no confirmada. ":"Identificada por SNMP. ")+(open.Length>0?"Puertos: "+String.Join(", ",open):"Sin puerto de impresión detectado")};
   if(printer.Verified){var info=await Task.WhenAll(TextValue(host,"1.3.6.1.2.1.1.5.0",false,token),TextValue(host,"1.3.6.1.2.1.43.5.1.1.16",true,token),TextValue(host,"1.3.6.1.2.1.1.6.0",false,token));SetDetails(printer,info[0],info[1],info[2]);}
   return printer;
  }
  public static void SelfTest(){
   var named=new Found{Name="Dos impresoras del mismo modelo",Verified=true};SetDetails(named,"KMC45BF1","Contabilidad","Dpto. Contabilidad");if(named.Name!="Dos impresoras del mismo modelo"||named.DeviceName!="Contabilidad"||named.Location!="Dpto. Contabilidad")throw new Exception("Nombre, modelo y ubicación no separados");
   SetDetails(named,"KMC45BF1","","LABORATORIO");if(named.DeviceName!="KMC45BF1"||named.Location!="LABORATORIO")throw new Exception("Nombre alternativo sysName");SetDetails(named,"","","");if(!named.Verified||named.Location!="")throw new Exception("Ubicación vacía cambia la identidad de la impresora");
   if(DisplayText(new SnmpValue{Tag=4,Data=new byte[]{0xC1,0x72,0x65,0x61}})!="Área"||DisplayText(new SnmpValue{Tag=4,Data=Encoding.UTF8.GetBytes("Área técnica")})!="Área técnica")throw new Exception("Texto de ubicación con acentos");
   const string oid="1.3.6.1.2.1.25.3.2.1.2.1";if(DecodeOid(OidBytes(oid))!=oid)throw new Exception("Codificación OID.");var request=Parse(Request(oid,123,false));if(request.Children[2].Tag!=160)throw new Exception("Consulta SNMP no GET.");
   var binding=Tlv(48,Join(Tlv(6,OidBytes(oid)),Tlv(6,OidBytes("1.3.6.1.2.1.25.3.1.5"))));var bytes=Tlv(48,Join(Integer(0),Tlv(4,Encoding.ASCII.GetBytes("public")),Tlv(162,Join(Integer(123),Integer(0),Integer(0),Tlv(48,binding)))));var response=Response(bytes,123);if(response==null||response.Oid!=oid||DecodeOid(response.Data)!="1.3.6.1.2.1.25.3.1.5")throw new Exception("Identificación SNMP de impresora.");if(Response(bytes,124)!=null)throw new Exception("Respuesta con identificador incorrecto.");bool rejected=false;try{Parse(new byte[]{48,130,255,255});}catch{rejected=true;}if(!rejected)throw new Exception("Respuesta truncada aceptada.");
  }
 }
}
