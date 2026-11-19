using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace EtiquetaLocal {
 // Plantilla del catálogo: categoría, nombre y cómo se construye la etiqueta.
 public class CatalogItem {
  public string Category,Name;public Func<Label> Build;
  public string Size {get{var l=Build();return l.Width.ToString("0.#")+" × "+l.Height.ToString("0.#")+" mm";}}
 }

 // Catálogo de etiquetas de ejemplo pensadas para Azor Ambiental. Todas se pueden editar después.
 public static class AzorCatalog {
  public static readonly string[] Categories={"Residuos peligrosos","Reciclaje y no peligrosos","Seguridad y avisos","Almacén y ubicaciones","Muestras y laboratorio","Inventario e informática","Mantenimiento y calibración","Contenedores y logística","Oficina y archivo"};
  const string Emoji="Segoe UI Emoji";

  static Element T(string text,float x,float y,float w,float h,float pt,bool bold=false,string align="Centro"){return new Element{Kind="Texto",Text=text,X=x,Y=y,W=w,H=h,FontSize=pt,Bold=bold,TextAlign=align,FontName="Arial"};}
  static Element L(string text,float x,float y,float w,float h,float pt,bool bold=false){return T(text,x,y,w,h,pt,bold,"Izquierda");}
  static Element S(string symbol,float x,float y,float w,float h,float pt){return new Element{Kind="Texto",Text=symbol,X=x,Y=y,W=w,H=h,FontSize=pt,FontName=Emoji,TextAlign="Centro"};}
  static Element F(float x,float y,float w,float h,float stroke){return new Element{Kind="Marco",Text="",X=x,Y=y,W=w,H=h,Stroke=stroke};}
  static Element Line(float x,float y,float w,float stroke){return new Element{Kind="Línea",Text="",X=x,Y=y,W=w,H=Math.Max(.5f,stroke*2),Stroke=stroke};}
  static Element Q(string text,float x,float y,float size){return new Element{Kind="QR",Text=text,X=x,Y=y,W=size,H=size};}
  static Element B(string text,float x,float y,float w,float h){return new Element{Kind="Código 39",Text=text,X=x,Y=y,W=w,H=h};}
  static Label Make(float w,float h,params Element[] elements){var l=new Label{Width=w,Height=h,Media=""};l.Elements.AddRange(elements);return l;}

  // Residuo peligroso (100 × 100 mm): datos del residuo y pictogramas.
  static Label Hazard(string name,string ler,params string[] pictos){
   var e=new List<Element>{F(1.5f,1.5f,97,97,1.2f),T("RESIDUO PELIGROSO",3,4,94,11,22,true),Line(3,15,94,.6f),
    L("Residuo:",4,18.5f,24,6.5f,10,true),L(name,27,18.5f,70,6.5f,10,true),
    L("Código LER:",4,25.5f,24,6.5f,10,true),L(ler,27,25.5f,70,6.5f,11),
    L("Titular:",4,32.5f,24,6.5f,10,true),L("Azor Ambiental",27,32.5f,70,6.5f,11),
    L("Dirección:",4,39.5f,24,6.5f,10,true),L("[Dirección del centro]",27,39.5f,70,6.5f,10),
    L("Fecha de envasado:",4,46.5f,40,6.5f,10,true),L("___ / ___ / ______",44,46.5f,53,6.5f,11),
    Line(3,54,94,.6f)};
   float cell=94f/Math.Max(1,pictos.Length);
   for(int i=0;i<pictos.Length;i++){var parts=pictos[i].Split('|');float x=3+i*cell,cx=x+cell/2;
    e.Add(F(cx-12,58,24,24,.5f));e.Add(S(parts[0],cx-12,59.5f,24,22,40));e.Add(T(parts.Length>1?parts[1]:"",x,83,cell,8,8,true));}
   e.Add(T("Manténgase cerrado · No mezclar con otros residuos",3,92.5f,94,5,8));
   return Make(100,100,e.ToArray());
  }
  // Residuo no peligroso / reciclaje (100 × 50 mm).
  static Label Recycle(string title,string ler,string symbol="♻"){
   return Make(100,50,F(1,1,98,48,1),S(symbol,3,7,30,32,56),L(title,35,3,62,16.5f,17,true),
    L("Residuo no peligroso · LER "+ler,35,20,62,6,10),L("Azor Ambiental",35,27,62,6,10,true),L("Fecha: ___ / ___ / ______",35,36,62,7,11));
  }
  // Señal de seguridad (100 × 60 mm).
  static Label Sign(string symbol,string title,string detail){
   return Make(100,60,F(1,1,98,58,1.6f),F(3.5f,3.5f,93,53,.4f),S(symbol,6,11,34,38,62),L(title,42,9,54,20,17,true),L(detail,42,31,54,20,11));
  }
  // Aviso con datos a rellenar (100 × 50 mm).
  static Label Notice(string symbol,string title,string detail,string field1,string field2){
   return Make(100,50,F(1,1,98,48,1.6f),S(symbol,3,8,30,32,52),L(title,34,4,63,16,18,true),L(detail,34,20,63,6,10),L(field1,34,30,63,6,10),L(field2,34,38,63,6,10));
  }
  // Muestra (60 × 40 mm) con QR.
  static Label Sample(string title,string code){
   return Make(60,40,F(1,1,58,38,.6f),T(title,2,2,56,6,11,true),Line(2,8.2f,56,.4f),
    L("Ref.: "+code,2.5f,10.5f,34,5,8,true),L("Punto: __________",2.5f,15.5f,34,5,8),L("Fecha: __/__/____",2.5f,20.5f,34,5,8),L("Hora: __:__",2.5f,25.5f,34,5,8),L("Técnico: ________",2.5f,30.5f,34,5,8),
    Q("AZOR-"+code,37.5f,12,20));
  }

  static readonly List<CatalogItem> items=new List<CatalogItem>();
  static void Add(int category,string name,Func<Label> build){items.Add(new CatalogItem{Category=Categories[category],Name=name,Build=build});}
  public static IList<CatalogItem> All {get{if(items.Count==0)Fill();return items;}}

  static void Fill(){
   // Residuos peligrosos. Códigos LER orientativos: revísalos con el responsable de medio ambiente.
   Add(0,"Residuo peligroso (genérico)",()=>Hazard("[Descripción del residuo]","[xx xx xx*]","⚠|Peligro"));
   Add(0,"Aceite usado",()=>Hazard("Aceite mineral usado","13 02 05*","⚠|Nocivo","🐟|Peligroso para el medio ambiente"));
   Add(0,"Envases contaminados",()=>Hazard("Envases contaminados","15 01 10*","⚠|Nocivo","🐟|Peligroso para el medio ambiente"));
   Add(0,"Absorbentes y trapos contaminados",()=>Hazard("Absorbentes y trapos contaminados","15 02 02*","🔥|Inflamable","⚠|Nocivo"));
   Add(0,"Baterías de plomo",()=>Hazard("Baterías de plomo","16 06 01*","☠|Tóxico","⚠|Corrosivo"));
   Add(0,"Fluorescentes y lámparas",()=>Hazard("Tubos fluorescentes y lámparas","20 01 21*","☠|Tóxico","🐟|Peligroso para el medio ambiente"));
   Add(0,"Aerosoles",()=>Hazard("Aerosoles y envases a presión","16 05 04*","🔥|Inflamable","⚠|Gas a presión"));
   Add(0,"Disolventes",()=>Hazard("Disolventes no halogenados","14 06 03*","🔥|Inflamable","☠|Tóxico"));
   Add(0,"Tóner y cartuchos",()=>Hazard("Tóner y cartuchos de impresión","08 03 17*","⚠|Nocivo"));
   Add(0,"RAEE peligrosos",()=>Hazard("RAEE con componentes peligrosos","20 01 35*","⚠|Peligro","🐟|Peligroso para el medio ambiente"));
   // Reciclaje y residuos no peligrosos.
   Add(1,"Papel y cartón",()=>Recycle("PAPEL Y CARTÓN","15 01 01"));
   Add(1,"Plástico",()=>Recycle("PLÁSTICO","15 01 02"));
   Add(1,"Metales y chatarra",()=>Recycle("METALES Y CHATARRA","17 04 05"));
   Add(1,"Madera",()=>Recycle("MADERA","15 01 03"));
   Add(1,"Vidrio",()=>Recycle("VIDRIO","15 01 07"));
   Add(1,"RAEE (aparatos eléctricos)",()=>Recycle("RAEE · APARATOS ELÉCTRICOS","20 01 36","🔌"));
   Add(1,"Pilas usadas",()=>Recycle("PILAS USADAS","20 01 34","🔋"));
   Add(1,"Residuo no peligroso (genérico)",()=>Recycle("[NOMBRE DEL RESIDUO]","[xx xx xx]"));
   // Seguridad y avisos.
   Add(2,"Riesgo eléctrico",()=>Sign("⚡","PELIGRO\nRiesgo eléctrico","No manipular. Solo personal autorizado."));
   Add(2,"Materias inflamables",()=>Sign("🔥","PELIGRO\nInflamable","Prohibido fumar y encender fuego."));
   Add(2,"Prohibido fumar",()=>Sign("🚭","PROHIBIDO FUMAR","Y encender fuego en esta zona."));
   Add(2,"Uso obligatorio de EPI",()=>Sign("⛑","USO OBLIGATORIO\nDE EPI","Casco, chaleco, guantes y calzado de seguridad."));
   Add(2,"Prohibido el paso",()=>Sign("🚫","PROHIBIDO\nEL PASO","Personal no autorizado."));
   Add(2,"Cargas suspendidas",()=>Sign("⚠","ATENCIÓN\nCargas suspendidas","No permanecer bajo la carga."));
   Add(2,"Zona de carga y descarga",()=>Sign("🚚","ZONA DE CARGA\nY DESCARGA","Precaución: circulación de vehículos."));
   Add(2,"Superficie caliente",()=>Sign("♨","ATENCIÓN\nSuperficie caliente","No tocar sin protección."));
   // Almacén y ubicaciones.
   Add(3,"Ubicación de estantería",()=>Make(100,50,F(1,1,98,48,.8f),T("UBICACIÓN",3,3,94,6,10,true),T("A-01-01",3,9,94,15,36,true),B("A-01-01",8,26,84,13),T("Estantería A · Nivel 1 · Hueco 1",3,41.5f,94,6,9)));
   Add(3,"Zona de almacenamiento",()=>Make(100,50,F(1,1,98,48,1.2f),T("ZONA",3,3,94,8,14,true),T("01",3,11,94,20,48,true),T("Almacén temporal de residuos",3,36,94,7,12)));
   Add(3,"Material en cuarentena",()=>Notice("⚠","CUARENTENA","No usar hasta su revisión.","Fecha: ___ / ___ / ______","Responsable: ______________"));
   Add(3,"Material bloqueado / no conforme",()=>Notice("⛔","MATERIAL\nBLOQUEADO","No conforme · No utilizar.","Motivo: __________________","Fecha: ___ / ___ / ______"));
   Add(3,"Recepción de material",()=>Make(100,50,F(1,1,98,48,.8f),T("RECEPCIÓN DE MATERIAL",3,3,94,8,14,true),Line(3,11.5f,94,.5f),L("Proveedor: ______________________",5,14,90,6,10),L("Albarán: ________________________",5,21,90,6,10),L("Fecha: ___ / ___ / ______",5,28,90,6,10),L("Recibido por: ___________________",5,35,90,6,10)));
   // Muestras y laboratorio.
   Add(4,"Muestra de agua",()=>Sample("MUESTRA DE AGUA","AGUA-0001"));
   Add(4,"Muestra de suelo",()=>Sample("MUESTRA DE SUELO","SUELO-0001"));
   Add(4,"Muestra de residuo",()=>Sample("MUESTRA DE RESIDUO","RES-0001"));
   Add(4,"Muestra de lixiviado",()=>Sample("MUESTRA DE LIXIVIADO","LIX-0001"));
   Add(4,"Cadena de custodia",()=>Make(100,50,F(1,1,98,48,.8f),L("CADENA DE CUSTODIA",4,3,62,8,14,true),L("Nº muestra: AZ-0001",4,13,62,6,10,true),L("Tomada por: ______________",4,20,62,6,10),L("Entregada a: _____________",4,27,62,6,10),L("Fecha y hora: __/__/____ __:__",4,34,62,6,10),Q("AZOR-CUSTODIA-AZ-0001",68,9,29)));
   // Inventario e informática.
   Add(5,"Propiedad de Azor Ambiental",()=>Make(60,30,F(1,1,58,28,.5f),T("PROPIEDAD DE AZOR AMBIENTAL",2,2,56,5,8,true),L("Nº inventario",2.5f,8,34,4,7),L("AZ-0001",2.5f,11.5f,34,7,14,true),B("AZ-0001",2.5f,20,34,7),Q("AZOR-INV-AZ-0001",38,8,19)));
   Add(5,"Equipo informático",()=>Make(60,40,F(1,1,58,38,.5f),T("EQUIPO INFORMÁTICO",2,2,56,6,10,true),Line(2,8.2f,56,.4f),L("Nº: PC-0001",2.5f,10.5f,34,5,9,true),L("Usuario: ________",2.5f,16,34,5,8),L("Nº serie: _______",2.5f,21.5f,34,5,8),L("Alta: __/__/____",2.5f,27,34,5,8),T("Soporte IT",2.5f,32.5f,34,5,7),Q("AZOR-IT-PC-0001",37.5f,12,20)));
   Add(5,"Cable de red",()=>Make(50,25,F(1,1,48,23,.4f),T("CABLE DE RED",2,2,46,5,8,true),T("PC-001 → SW1-P12",2,8,46,7,12,true),L("Origen: _________",2.5f,15.5f,45,4,7),L("Destino: ________",2.5f,19.5f,45,4,7)));
   Add(5,"Herramienta / activo",()=>Make(50,25,F(1,1,48,23,.4f),T("AZOR AMBIENTAL",2,1.5f,46,5,8,true),T("HT-0001",2,6.5f,46,6,12,true),B("HT-0001",4,13.5f,42,8)));
   // Mantenimiento y calibración.
   Add(6,"Calibrado",()=>Make(50,25,F(1,1,48,23,.5f),T("CALIBRADO",2,1.5f,46,6,12,true),L("Fecha: __/__/____",3,8.5f,44,4.5f,8),L("Próxima: __/__/____",3,13,44,4.5f,8),L("Por: ____________",3,17.5f,44,4.5f,8)));
   Add(6,"Revisado",()=>Make(50,25,F(1,1,48,23,.5f),T("✓ REVISADO",2,1.5f,46,6,12,true),L("Fecha: __/__/____",3,8.5f,44,4.5f,8),L("Firma: __________",3,13,44,4.5f,8),L("Equipo: _________",3,17.5f,44,4.5f,8)));
   Add(6,"Fuera de servicio",()=>Notice("⛔","FUERA DE\nSERVICIO","No utilizar. Avisar a mantenimiento.","Fecha: ___ / ___ / ______","Firma: __________________"));
   Add(6,"Mantenimiento preventivo",()=>Make(60,40,F(1,1,58,38,.6f),T("MANTENIMIENTO\nPREVENTIVO",2,2,56,10,10,true),Line(2,12.5f,56,.4f),L("Equipo: ______________",3,14.5f,54,5,8),L("Última: __/__/____",3,20,54,5,8),L("Próxima: __/__/____",3,25.5f,54,5,8),L("Técnico: _____________",3,31,54,5,8)));
   // Contenedores y logística.
   Add(7,"Contenedor",()=>Make(100,50,F(1,1,98,48,1),T("CONTENEDOR",3,3,94,7,12,true),T("Nº 0001",3,10,94,14,32,true),B("CONT-0001",10,25.5f,80,12),T("Azor Ambiental · Gestión de residuos",3,40,94,6,9)));
   Add(7,"Big bag",()=>Make(100,100,F(1.5f,1.5f,97,97,1.2f),T("BIG BAG",3,4,94,12,26,true),Line(3,17,94,.6f),L("Nº: BB-0001",5,21,90,8,14,true),L("Contenido: ______________________",5,32,90,7,11),L("Origen: _________________________",5,41,90,7,11),L("Peso: _______ kg",5,50,90,7,11),L("Fecha: ___ / ___ / ______",5,59,90,7,11),B("BB-0001",10,71,80,14),T("Azor Ambiental",3,90,94,6,10,true)));
   Add(7,"Punto de recogida",()=>Sign("♻","PUNTO DE\nRECOGIDA","Pilas · Tóner · Fluorescentes · RAEE pequeños"));
   // Oficina y archivo.
   Add(8,"Caja de archivo",()=>Make(100,50,F(1,1,98,48,.8f),T("ARCHIVO",3,3,94,9,18,true),Line(3,13,94,.5f),L("Departamento: ______________",5,16,90,6,10),L("Contenido: _________________",5,23,90,6,10),L("Año: ______",5,30,40,6,10),L("Caja nº: ______",50,30,45,6,10),T("Azor Ambiental",3,40,94,6,9,true)));
   Add(8,"Carpeta",()=>Make(100,30,F(1,1,98,28,.8f),T("[DEPARTAMENTO]",3,3,94,10,20,true),T("[Contenido de la carpeta] · [Año]",3,15,94,7,11)));
   Add(8,"Taquilla",()=>Make(50,25,F(1,1,48,23,.5f),T("TAQUILLA",2,1.5f,46,5,9,true),T("Nº 01",2,7,46,8,16,true),T("[Nombre]",2,16,46,6,10)));
  }

  public static void SelfTest(){foreach(var item in All){var l=item.Build();Model.Validate(Model.Load(Model.Save(l)));string problem=Editing.MarginProblem(l);if(problem!=null)throw new Exception("Plantilla «"+item.Name+"»: "+problem);}if(All.Count<40)throw new Exception("Catálogo incompleto");}

  // Miniatura de la plantilla (con borde de etiqueta y fondo blanco).
  public static Bitmap Thumbnail(Label label,int width,int height){
   var b=new Bitmap(width,height);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
    float k=Math.Min((width-10)/label.Width,(height-10)/label.Height);float w=label.Width*k,h=label.Height*k,x=(width-w)/2,y=(height-h)/2;
    using(var shadow=new SolidBrush(Color.FromArgb(40,0,0,0)))g.FillRectangle(shadow,x+2,y+2,w,h);g.FillRectangle(Brushes.White,x,y,w,h);
    var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(k,k);try{Render.Draw(g,label);}catch{}g.Restore(state);
    using(var p=new Pen(Color.FromArgb(170,185,188)))g.DrawRectangle(p,x,y,w,h);}
   return b;
  }
 }

 // Ventana del catálogo: categorías a la izquierda, miniaturas a la derecha.
 public class CatalogForm:Form {
  public CatalogItem Chosen;
  readonly FlowLayoutPanel grid=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(244,247,248),Padding=new Padding(12)};
  readonly System.Windows.Forms.Label title=new System.Windows.Forms.Label{Dock=DockStyle.Top,Height=40,Font=new Font("Segoe UI",16),ForeColor=Color.FromArgb(30,45,48)};
  readonly TextBox search=new TextBox{Width=240,Font=new Font("Segoe UI",10),AccessibleName="Buscar plantilla"};
  readonly Button use=new Button{Text="Usar esta plantilla",Width=180,Height=38,FlatStyle=FlatStyle.Flat,ForeColor=Color.White,Font=new Font("Segoe UI",10,FontStyle.Bold),Enabled=false};
  readonly Dictionary<CatalogItem,Bitmap> thumbs=new Dictionary<CatalogItem,Bitmap>();
  readonly List<Button> navButtons=new List<Button>();
  string category;Button selectedTile;
  public CatalogForm(){
   Text="Catálogo de etiquetas · Azor Ambiental";Icon=Branding.AppIcon;ClientSize=new Size(1080,700);MinimumSize=new Size(900,560);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.5f);BackColor=Color.White;KeyPreview=true;
   KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)Close();};
   var nav=new Panel{Dock=DockStyle.Left,Width=230,BackColor=Look.Teal,Padding=new Padding(10,14,10,10)};
   var navFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Look.Teal};
   navFlow.Controls.Add(NavButton("Todas las plantillas",null));foreach(var c in AzorCatalog.Categories)navFlow.Controls.Add(NavButton(c,c));nav.Controls.Add(navFlow);
   var head=new Panel{Dock=DockStyle.Top,Height=86,Padding=new Padding(18,10,18,4),BackColor=Color.White};
   var sub=new System.Windows.Forms.Label{Dock=DockStyle.Bottom,Height=24,ForeColor=Color.FromArgb(90,110,115),Text="Elige una plantilla y cámbiala a tu gusto: doble clic en cualquier texto para escribir."};
   var searchBox=new FlowLayoutPanel{Dock=DockStyle.Right,Width=300,FlowDirection=FlowDirection.RightToLeft,BackColor=Color.White,Padding=new Padding(0,8,0,0)};
   searchBox.Controls.Add(search);searchBox.Controls.Add(new System.Windows.Forms.Label{Text="Buscar",AutoSize=true,Margin=new Padding(0,5,6,0),ForeColor=Color.FromArgb(90,110,115)});
   head.Controls.Add(title);head.Controls.Add(searchBox);head.Controls.Add(sub);
   search.TextChanged+=(s,e)=>Fill();
   var bottom=new Panel{Dock=DockStyle.Bottom,Height=60,BackColor=Color.White,Padding=new Padding(18,10,18,10)};
   var note=new System.Windows.Forms.Label{Dock=DockStyle.Fill,ForeColor=Color.FromArgb(90,110,115),TextAlign=ContentAlignment.MiddleLeft,Text="Plantillas orientativas. Revisa los códigos LER y los pictogramas con el responsable de medio ambiente antes de usarlas."};
   var cancel=new Button{Text="Cancelar",Width=110,Height=38,Dock=DockStyle.Right,FlatStyle=FlatStyle.Flat,DialogResult=DialogResult.Cancel};cancel.FlatAppearance.BorderColor=Look.TealLine;
   use.Dock=DockStyle.Right;use.BackColor=Color.FromArgb(160,180,184);use.FlatAppearance.BorderSize=0;use.EnabledChanged+=(s,e)=>use.BackColor=use.Enabled?Look.Teal:Color.FromArgb(160,180,184);use.Click+=(s,e)=>{if(Chosen!=null)DialogResult=DialogResult.OK;};
   bottom.Controls.Add(note);bottom.Controls.Add(new Panel{Width=10,Dock=DockStyle.Right,BackColor=Color.White});bottom.Controls.Add(cancel);bottom.Controls.Add(new Panel{Width=10,Dock=DockStyle.Right,BackColor=Color.White});bottom.Controls.Add(use);
   var line=new Panel{Dock=DockStyle.Bottom,Height=1,BackColor=Look.TealLine};
   Controls.Add(grid);Controls.Add(head);Controls.Add(line);Controls.Add(bottom);Controls.Add(nav);
   AcceptButton=use;CancelButton=cancel;
   FormClosed+=(s,e)=>{foreach(var b in thumbs.Values)b.Dispose();};
   Select(null);
  }
  Button NavButton(string text,string value){
   var b=new Button{Text="  "+text,TextAlign=ContentAlignment.MiddleLeft,Width=206,Height=40,FlatStyle=FlatStyle.Flat,ForeColor=Color.White,BackColor=Look.TealDark,Font=new Font("Segoe UI",10),Margin=new Padding(0,0,0,6),Cursor=Cursors.Hand,Tag=value,AccessibleName="Categoría "+text};
   b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Look.TealHover;b.Click+=(s,e)=>Select(value);navButtons.Add(b);return b;
  }
  void Select(string value){category=value;foreach(var b in navButtons){bool on=Equals(b.Tag,value);b.BackColor=on?Color.White:Look.TealDark;b.ForeColor=on?Look.Teal:Color.White;b.FlatAppearance.MouseOverBackColor=on?Color.White:Look.TealHover;}title.Text=value??"Todas las plantillas";Fill();}
  void Fill(){
   grid.SuspendLayout();grid.Controls.Clear();selectedTile=null;Chosen=null;use.Enabled=false;
   string q=search.Text.Trim();
   var list=AzorCatalog.All.Where(i=>(category==null||i.Category==category)&&(q.Length==0||i.Name.IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0||i.Category.IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0)).ToList();
   foreach(var item in list)grid.Controls.Add(Tile(item));
   if(list.Count==0)grid.Controls.Add(new System.Windows.Forms.Label{Text="No hay plantillas que coincidan con la búsqueda.",AutoSize=true,ForeColor=Color.FromArgb(90,110,115),Margin=new Padding(8)});
   grid.ResumeLayout();
  }
  Control Tile(CatalogItem item){
   Bitmap thumb;if(!thumbs.TryGetValue(item,out thumb)){thumb=AzorCatalog.Thumbnail(item.Build(),176,128);thumbs[item]=thumb;}
   string size=item.Size;
   var b=new Button{Width=190,Height=190,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,10,10),Cursor=Cursors.Hand,Text="",AccessibleName=item.Name+", "+size+", "+item.Category,AccessibleRole=AccessibleRole.PushButton};
   b.FlatAppearance.BorderColor=Look.TealLine;b.FlatAppearance.MouseOverBackColor=Look.TealSoft;
   b.Paint+=(s,e)=>{var g=e.Graphics;g.DrawImage(thumb,(b.Width-thumb.Width)/2,8);
    using(var f=new Font("Segoe UI",9.5f,FontStyle.Bold))TextRenderer.DrawText(g,item.Name,f,new Rectangle(6,140,b.Width-12,22),Color.FromArgb(30,45,48),TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
    TextRenderer.DrawText(g,size,b.Font,new Rectangle(6,162,b.Width-12,20),Color.FromArgb(90,110,115),TextFormatFlags.HorizontalCenter);
    if(b==selectedTile)using(var p=new Pen(Look.Teal,3))g.DrawRectangle(p,1,1,b.Width-3,b.Height-3);};
   b.Click+=(s,e)=>{var old=selectedTile;selectedTile=b;Chosen=item;use.Enabled=true;if(old!=null)old.Invalidate();b.Invalidate();};
   b.DoubleClick+=(s,e)=>{Chosen=item;DialogResult=DialogResult.OK;};
   b.MouseDoubleClick+=(s,e)=>{Chosen=item;DialogResult=DialogResult.OK;};
   Look.Tips.SetToolTip(b,item.Name+" · "+size);return b;
  }
 }

 public partial class MainForm {
  // Abre el catálogo y carga la plantilla elegida como un diseño nuevo.
  void ShowCatalog(){
   CatalogItem chosen;using(var d=new CatalogForm()){if(d.ShowDialog(this)!=DialogResult.OK||d.Chosen==null)return;chosen=d.Chosen;}
   if(!Confirm())return;
   welcomeDone=true;label=chosen.Build();file="";dirty=false;undo.Clear();canvas.Selected=null;RefreshAll();Mark();
   status.Text="Plantilla «"+chosen.Name+"» cargada ("+label.Width+" × "+label.Height+" mm). Doble clic en un texto para cambiarlo.";
  }
 }
}
