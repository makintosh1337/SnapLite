using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SnapLite
{
    internal static class Tests
    {
        private static int count;
        private static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);count++;}
        public static Bitmap MakeDemo()
        {
            var b=new Bitmap(900,480);
            using(var g=Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(239,242,251));using(var br=new SolidBrush(Color.FromArgb(95,76,230)))g.FillRectangle(br,0,0,900,95);
                using(var f=new Font("Segoe UI",25,FontStyle.Bold))g.DrawString("SnapLite  /  Проверка редактора",f,Brushes.White,32,26);
                using(var f=new Font("Segoe UI",16))g.DrawString("Точная область. Локальные снимки. Удобные пометки.",f,Brushes.DimGray,32,130);
                using(var br=new SolidBrush(Color.White))g.FillRectangle(br,32,195,836,240);
                for(int i=0;i<6;i++){using(var br=new SolidBrush(Color.FromArgb(75+i*20,100+i*12,210-i*12)))g.FillRectangle(br,64+i*126,230,92,90+i*12);}
                using(var f=new Font("Segoe UI",11))g.DrawString("Демонстрационное изображение · 900 × 480 px",f,Brushes.DimGray,52,402);
            }
            return b;
        }
        public static void Run(string reportPath)
        {
            string dir=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)),"test-artifacts");Directory.CreateDirectory(dir);
            var log=new StringBuilder();
            try
            {
                Check(Images.FromPoints(new Point(30,40),new Point(10,5))==new Rectangle(10,5,20,35),"reverse drag coordinates");
                Check(Images.Clamp(new Rectangle(-4,-3,20,20),new Size(10,10))==new Rectangle(0,0,10,10),"clip negative coordinates");
                Check(Hotkeys.IsValid(Keys.PrintScreen,0)&&Hotkeys.IsValid(Keys.S,6)&&!Hotkeys.IsValid(Keys.S,0)&&!Hotkeys.IsValid(Keys.S,4)&&!Hotkeys.IsValid(Keys.ControlKey,2),"hotkey validation");
                var settings=new Settings {OutputScale=999,DelaySeconds=-3,JpegQuality=0,Format="bad",CaptureAction="bad",SaveFolder=dir};settings.Normalize();
                Check(settings.OutputScale==400&&settings.DelaySeconds==0&&settings.JpegQuality==1&&settings.Format=="PNG"&&settings.CaptureAction=="Editor","settings bounds");
                string xml=Path.Combine(dir,"settings.xml");settings.Save(xml);settings.OutputScale=50;settings.Save(xml);
                using(var s=File.OpenRead(xml)){var loaded=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(s);Check(loaded.OutputScale==50&&loaded.SaveFolder==dir,"settings round trip and atomic replacement");}
                using(var source=new Bitmap(80,60))
                {
                    using(var g=Graphics.FromImage(source))g.Clear(Color.White);
                    using(var resized=Images.Resize(source,50))Check(resized.Size==new Size(40,30),"50 percent output dimensions");
                    using(var resized=Images.Resize(source,125))Check(resized.Size==new Size(100,75),"125 percent output dimensions");
                    settings.Format="PNG";string a=Images.SaveUnique(source,settings),b=Images.SaveUnique(source,settings);Check(a!=b,"unique save names");
                    using(var saved=new Bitmap(a))Check(saved.Size==source.Size,"PNG dimensions");
                    settings.Format="JPG";settings.JpegQuality=92;string jpg=Images.SaveUnique(source,settings);using(var saved=new Bitmap(jpg))Check(saved.RawFormat.Guid==ImageFormat.Jpeg.Guid&&saved.Size==source.Size,"JPG encoding");
                    using(var doc=new Document((Bitmap)source.Clone()))
                    {
                        var mark=new Mark{Tool=DrawTool.Cover,Color=Color.Black,Width=3};mark.Points.Add(new PointF(10,10));mark.Points.Add(new PointF(30,30));doc.Add(mark);
                        using(var result=doc.Render(100))Check(result.GetPixel(20,20).R==0&&result.GetPixel(2,2).R==255,"annotations rendered into image");
                        doc.Undo();Check(doc.Marks.Count==0,"undo");doc.Redo();Check(doc.Marks.Count==1,"redo");doc.Undo();doc.Add(mark);doc.Redo();Check(doc.Marks.Count==1,"new edit clears redo");
                        foreach(DrawTool tool in Enum.GetValues(typeof(DrawTool))){var m=new Mark{Tool=tool,Color=Color.Red,Width=3,Text="Тест"};m.Points.Add(new PointF(2,2));m.Points.Add(new PointF(50,40));doc.Add(m);}
                        using(var result=doc.Render(200)){Check(result.Size==new Size(160,120),"all drawing tools render and export at 200 percent");result.Save(Path.Combine(dir,"annotations.png"));}
                    }
                }
                using(var demo=MakeDemo())demo.Save(Path.Combine(dir,"demo.png"));
                using(var icon=Theme.Icon())using(var file=File.Create(Path.Combine(dir,"SnapLite.ico")))icon.Save(file);
                // Real capture exercises GDI and DPI coordinates; only a neutral 16 px corner is retained in memory.
                using(var capture=Images.Capture(new Rectangle(SystemInformation.VirtualScreen.Location,new Size(16,16)),false))Check(capture.Size==new Size(16,16),"real desktop capture");
                using(var capture=Images.Capture(new Rectangle(SystemInformation.VirtualScreen.Location,new Size(16,16)),true))Check(capture.Size==new Size(16,16),"real desktop capture with cursor");
                log.AppendLine("PASS: "+count+" checks");
                log.AppendLine("Selection geometry, settings persistence, hotkeys, dimensions, PNG/JPG, annotations, undo/redo, real GDI capture.");
            }
            catch(Exception ex){log.AppendLine(ex.ToString());Environment.ExitCode=1;}
            File.WriteAllText(reportPath,log.ToString(),Encoding.UTF8);
        }
    }
}
