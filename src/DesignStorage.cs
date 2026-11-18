using System;
using System.IO;
using System.Text;
namespace EtiquetaLocal {
 public static class DesignStorage {
  public static string EnsureDefaultFolder(){return EnsureFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));}
  public static string EnsureFolder(string documents){if(String.IsNullOrWhiteSpace(documents))throw new IOException("Windows no ha indicado la carpeta Documentos.");string path=Path.Combine(documents,"Diseños Etiquetas");Directory.CreateDirectory(path);return path;}
  public static void Write(string path,string content){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,content,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static void SelfTest(){string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"design-test-"+Guid.NewGuid().ToString("N"));try{string dir=EnsureFolder(root);if(!Directory.Exists(dir)||EnsureFolder(root)!=dir)throw new Exception("Carpeta de diseños");string f=Path.Combine(dir,"Mi cartel.etiqueta");Write(f,"uno");Write(f,"dos");if(File.ReadAllText(f)!="dos")throw new Exception("Guardado atómico");}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
 }
}
