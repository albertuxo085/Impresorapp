using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
namespace EtiquetaLocal {
 public static class PaperPrinting {
  public static event Action SelectionChanged;
  static string selectedName;
  public static string CurrentName {get{if(!String.IsNullOrWhiteSpace(selectedName))return selectedName;using(var p=new PrintDocument())return p.PrinterSettings.IsValid?p.PrinterSettings.PrinterName+" (predeterminada de Windows)":"Sin impresora instalada";}}
  public static void Select(IWin32Window owner,Label label){try{using(var doc=new PrintDocument())Choose(owner,doc,label,true);}catch(Exception ex){MessageBox.Show(owner,ex.Message,"Elegir impresora de papel");}}
  static bool Choose(IWin32Window owner,PrintDocument doc,Label label,bool selectOnly=false){
   var installed=PrinterSettings.InstalledPrinters.Cast<string>().ToArray();if(installed.Length==0)throw new Exception("No hay impresoras de papel instaladas. Instala el controlador oficial de una impresora A4/A3 y vuelve a intentarlo.");
   using(var form=new Form{Text="Imprimir cartel · Elegir impresora",ClientSize=new Size(580,235),StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",10),Icon=Branding.AppIcon,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(242,247,244)}){
    var printers=new ComboBox{Left=20,Top=50,Width=535,DropDownStyle=ComboBoxStyle.DropDownList};printers.Items.AddRange(installed);printers.SelectedItem=installed.Contains(selectedName)?selectedName:installed.Contains(doc.PrinterSettings.PrinterName)?doc.PrinterSettings.PrinterName:installed[0];
    var copies=new NumericUpDown{Left=88,Top=100,Width=90,Minimum=1,Maximum=999,Value=1};
    form.Controls.Add(new System.Windows.Forms.Label{Text="Impresora instalada para papel · "+label.Width+" × "+label.Height+" mm",Left=20,Top=20,Width=535,Height=25});form.Controls.Add(printers);form.Controls.Add(new System.Windows.Forms.Label{Text="Copias",Left=20,Top=104,Width=65});form.Controls.Add(copies);
    form.Controls.Add(new System.Windows.Forms.Label{Text="Primero verás la vista previa. Desde ella podrás imprimir. El diseño se ajusta a los márgenes físicos conservando las proporciones.",Left=20,Top=139,Width=535,Height=45});
    var preview=Ui.Action("print",selectOnly?"Usar esta impresora":"Ver vista previa",()=>{doc.PrinterSettings.PrinterName=printers.SelectedItem as string;doc.PrinterSettings.Copies=(short)copies.Value;selectedName=doc.PrinterSettings.PrinterName;if(SelectionChanged!=null)SelectionChanged();form.DialogResult=DialogResult.OK;},185,true);preview.Location=new Point(250,190);form.Controls.Add(preview);var cancel=new Button{Text="Cancelar",Left=445,Top=192,Width=110,Height=32,DialogResult=DialogResult.Cancel};form.Controls.Add(cancel);form.CancelButton=cancel;form.AcceptButton=preview;return form.ShowDialog(owner)==DialogResult.OK;
   }
  }
  public static void Show(IWin32Window owner,Label original){
   Model.Validate(original);if(original.Elements.Count==0||Model.Outside(original))throw new Exception("Añade el contenido y comprueba que queda dentro del papel.");
   var label=Model.Load(Model.Save(original));using(var doc=new PrintDocument()){
    doc.DocumentName="Cartel · EtiquetaLocal";if(!Choose(owner,doc,label))return;
    bool landscape=label.Width>label.Height;float paperW=Math.Min(label.Width,label.Height),paperH=Math.Max(label.Width,label.Height);
    var paper=doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p=>Math.Abs(p.Width*.254f-paperW)<2&&Math.Abs(p.Height*.254f-paperH)<2);
    if(paper==null)throw new Exception("La impresora seleccionada no ofrece este tamaño de papel. Elige una impresora compatible o cambia el formato del cartel.");
    doc.DefaultPageSettings.PaperSize=paper;doc.DefaultPageSettings.Landscape=landscape;doc.DefaultPageSettings.Margins=new Margins(20,20,20,20);
    doc.PrintPage+=(s,e)=>{var g=e.Graphics;g.PageUnit=GraphicsUnit.Millimeter;var printable=e.PageSettings.PrintableArea;float x=printable.X*.254f+3,y=printable.Y*.254f+3,w=printable.Width*.254f-6,h=printable.Height*.254f-6;float scale=Math.Min(1,Math.Min(w/label.Width,h/label.Height));if(scale<=0)throw new Exception("Área imprimible no válida.");g.TranslateTransform(x-e.PageSettings.HardMarginX*.254f,y-e.PageSettings.HardMarginY*.254f);g.ScaleTransform(scale,scale);Render.Draw(g,label);e.HasMorePages=false;};
    using(var preview=new PrintPreviewDialog{Document=doc,Width=1000,Height=800,Icon=Branding.AppIcon,Text="Vista previa del cartel · Se ajusta a los márgenes de la impresora"})preview.ShowDialog(owner);
   }
  }
 }
}
