using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace EtiquetaLocal {
 [DataContract] public class NetworkPreferences {
  [DataMember] public string First;
  [DataMember] public string Last;
 }
 public static class AppPreferences {
  static string SettingsPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtiquetaLocal","preferences.json"); } }
  public static bool Valid(string first,string last){try{Network.Range(first,last);return true;}catch{return false;}}
  public static NetworkPreferences Load(){return Load(SettingsPath);}
  public static NetworkPreferences Load(string path){try{using(var stream=File.OpenRead(path)){var value=(NetworkPreferences)new DataContractJsonSerializer(typeof(NetworkPreferences)).ReadObject(stream);return value!=null&&Valid(value.First,value.Last)?value:null;}}catch{return null;}}
  public static void Save(string first,string last){Save(SettingsPath,first,last);}
  public static void Save(string path,string first,string last){
   if(!Valid(first,last))throw new ArgumentException("Rango de IP no válido.");
   Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{using(var stream=File.Create(temp))new DataContractJsonSerializer(typeof(NetworkPreferences)).WriteObject(stream,new NetworkPreferences{First=first,Last=last});
    if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
   }finally{if(File.Exists(temp))File.Delete(temp);}
  }
  public static void SelfTest(){
   string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preferences-test-"+Guid.NewGuid().ToString("N")+".json");
   try{Save(path,"192.0.2.10","192.0.2.99");var loaded=Load(path);if(loaded==null||loaded.First!="192.0.2.10"||loaded.Last!="192.0.2.99")throw new Exception("No se recupera el rango guardado");
    Save(path,"192.0.2.20","192.0.2.80");if(Load(path).First!="192.0.2.20")throw new Exception("No se actualizan las preferencias");
    bool rejected=false;try{Save(path,"incorrecta","192.0.2.1");}catch(ArgumentException){rejected=true;}if(!rejected||Load(path).First!="192.0.2.20")throw new Exception("Se ha perdido un rango válido");
    File.WriteAllText(path,"JSON roto");if(Load(path)!=null)throw new Exception("No se gestiona una preferencia dañada");
   }finally{if(File.Exists(path))File.Delete(path);}
  }
 }
}
