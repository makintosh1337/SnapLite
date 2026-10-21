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
        private static Settings ReadSettings(string path)
        {
            using(var stream=File.OpenRead(path))return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
        }
        private static void CheckAppearance(string dir)
        {
            var defaults=new Settings();defaults.Normalize();
            Check(defaults.ThemePreset=="Midnight"&&defaults.GlassOpacity==82&&defaults.CornerRadius==16,"appearance defaults");
            Check(defaults.ToolbarPosition=="Top"&&defaults.ActionPosition=="Right"&&defaults.SelectionToolbarPosition=="Below"&&defaults.ButtonOrder=="Copy,Save,QuickSave"&&defaults.ToolOrder=="Pen,Arrow,Rectangle,Marker,Text,Cover","layout defaults");

            var bounds=new Settings {GlassOpacity=-1,CornerRadius=-1};bounds.Normalize();
            Check(bounds.GlassOpacity==30&&bounds.CornerRadius==0,"appearance minimum bounds");
            bounds.GlassOpacity=101;bounds.CornerRadius=100;bounds.Normalize();
            Check(bounds.GlassOpacity==100&&bounds.CornerRadius==28,"appearance maximum bounds");
            bounds.ToolbarPosition="Diagonal";bounds.ActionPosition=null;bounds.SelectionToolbarPosition="";bounds.Normalize();
            Check(bounds.ToolbarPosition=="Top"&&bounds.ActionPosition=="Right"&&bounds.SelectionToolbarPosition=="Below","invalid positions recover to visible defaults");
            bounds.BackgroundColor=Color.FromArgb(0,18,31,47).ToArgb();bounds.PanelColor=Color.FromArgb(1,35,50,67).ToArgb();bounds.TextColor=Color.FromArgb(90,239,242,250).ToArgb();bounds.AccentColor=Color.FromArgb(220,65,216,175).ToArgb();bounds.Normalize();
            Check(bounds.BackgroundColor==Color.FromArgb(18,31,47).ToArgb()&&bounds.PanelColor==Color.FromArgb(35,50,67).ToArgb()&&bounds.TextColor==Color.FromArgb(239,242,250).ToArgb()&&bounds.AccentColor==Color.FromArgb(65,216,175).ToArgb(),"palette alpha cannot make controls unreadable");
            bounds.MenuColor=Color.FromArgb(0,20,30,40).ToArgb();bounds.ButtonColor=Color.FromArgb(1,40,50,60).ToArgb();bounds.BorderColor=Color.FromArgb(90,110,120,130).ToArgb();bounds.Accent2Color=Color.FromArgb(220,70,80,190).ToArgb();bounds.Normalize();
            Check(bounds.MenuColor==Color.FromArgb(20,30,40).ToArgb()&&bounds.ButtonColor==Color.FromArgb(40,50,60).ToArgb()&&bounds.BorderColor==Color.FromArgb(110,120,130).ToArgb()&&bounds.Accent2Color==Color.FromArgb(70,80,190).ToArgb(),"menu and button palette alpha is normalized separately from glass opacity");
            bool safeOrder=true;
            foreach(string order in new string[]{null,"","Copy,Copy,Save","Copy,Save","Copy,Save,Other","Copy,Save,QuickSave,Save"})
            {
                bounds.ButtonOrder=order;bounds.Normalize();safeOrder&=bounds.ButtonOrder=="Copy,Save,QuickSave";
            }
            Check(safeOrder,"invalid button orders cannot hide or duplicate actions");
            bool safeTools=true;
            foreach(string order in new string[]{null,"","Pen,Arrow,Rectangle,Marker,Text,Text","Pen,Arrow,Rectangle,Marker,Text","Pen,Arrow,Rectangle,Marker,Text,Other","Pen,Arrow,Rectangle,Marker,Text,Cover,Pen"})
            {
                bounds.ToolOrder=order;bounds.Normalize();safeTools&=bounds.ToolOrder=="Pen,Arrow,Rectangle,Marker,Text,Cover";
            }
            Check(safeTools,"invalid tool orders cannot hide or duplicate drawing tools");

            // A pre-appearance profile must retain its capture preferences and gain safe visual defaults.
            string legacy=Path.Combine(dir,"legacy-settings.xml");
            File.WriteAllText(legacy,"<?xml version=\"1.0\"?><Settings><Hotkey>83</Hotkey><Modifiers>6</Modifiers><Magnifier>false</Magnifier><IncludeCursor>true</IncludeCursor><DelaySeconds>3</DelaySeconds><CaptureAction>Save</CaptureAction><OutputScale>150</OutputScale><Format>JPG</Format><JpegQuality>87</JpegQuality><CloseAfterAction>false</CloseAfterAction></Settings>",Encoding.UTF8);
            var migrated=ReadSettings(legacy);migrated.Normalize();
            Check(migrated.ThemePreset=="Midnight"&&migrated.GlassOpacity==82&&migrated.CornerRadius==16&&migrated.ButtonOrder=="Copy,Save,QuickSave"&&migrated.ToolOrder=="Pen,Arrow,Rectangle,Marker,Text,Cover","old profile gains appearance defaults");
            Check(migrated.Hotkey==(int)Keys.S&&migrated.Modifiers==6&&!migrated.Magnifier&&migrated.IncludeCursor&&migrated.DelaySeconds==3&&migrated.CaptureAction=="Save"&&migrated.OutputScale==150&&migrated.Format=="JPG"&&migrated.JpegQuality==87&&!migrated.CloseAfterAction,"appearance migration preserves capture preferences");

            var draft=defaults.Clone();draft.AccentColor=Color.Orange.ToArgb();draft.ButtonOrder="QuickSave,Copy,Save";draft.GlassOpacity=45;
            Check(defaults.AccentColor!=draft.AccentColor&&defaults.ButtonOrder=="Copy,Save,QuickSave"&&defaults.GlassOpacity==82,"cancelled appearance draft leaves saved settings unchanged");

            var custom=migrated.Clone();custom.SaveFolder=dir;
            custom.BackgroundColor=Color.FromArgb(255,18,31,47).ToArgb();custom.PanelColor=Color.FromArgb(255,35,50,67).ToArgb();
            custom.TextColor=Color.FromArgb(255,239,242,250).ToArgb();custom.AccentColor=Color.FromArgb(255,65,216,175).ToArgb();
            custom.MenuColor=Color.FromArgb(255,20,30,40).ToArgb();custom.ButtonColor=Color.FromArgb(255,40,50,60).ToArgb();custom.BorderColor=Color.FromArgb(255,110,120,130).ToArgb();custom.Accent2Color=Color.FromArgb(255,70,80,190).ToArgb();
            custom.GlassOpacity=61;custom.CornerRadius=23;custom.ToolbarPosition="Left";custom.ActionPosition="Left";custom.SelectionToolbarPosition="Above";custom.ButtonOrder="QuickSave,Copy,Save";custom.ToolOrder="Cover,Text,Marker,Rectangle,Arrow,Pen";custom.ShowToolLabels=false;
            string path=Path.Combine(dir,"appearance-settings.xml");custom.Save(path);var restored=ReadSettings(path);restored.Normalize();
            Check(restored.BackgroundColor==custom.BackgroundColor&&restored.PanelColor==custom.PanelColor&&restored.TextColor==custom.TextColor&&restored.AccentColor==custom.AccentColor&&restored.GlassOpacity==61&&restored.CornerRadius==23,"custom colors and glass appearance survive restart");
            Check(restored.MenuColor==custom.MenuColor&&restored.ButtonColor==custom.ButtonColor&&restored.BorderColor==custom.BorderColor&&restored.Accent2Color==custom.Accent2Color,"custom menu, button and border colors survive restart");
            Check(restored.ToolbarPosition=="Left"&&restored.ActionPosition=="Left"&&restored.SelectionToolbarPosition=="Above"&&restored.ButtonOrder=="QuickSave,Copy,Save"&&restored.ToolOrder=="Cover,Text,Marker,Rectangle,Arrow,Pen"&&!restored.ShowToolLabels,"custom button arrangement survives restart");
            Check(restored.Hotkey==custom.Hotkey&&restored.Modifiers==custom.Modifiers&&restored.IncludeCursor==custom.IncludeCursor&&restored.Magnifier==custom.Magnifier&&restored.DelaySeconds==custom.DelaySeconds&&restored.CaptureAction==custom.CaptureAction&&restored.OutputScale==custom.OutputScale&&restored.Format==custom.Format&&restored.JpegQuality==custom.JpegQuality&&restored.CloseAfterAction==custom.CloseAfterAction,"saving appearance preserves capture preferences");
        }
        private static void CheckLanguageAndSize(string dir)
        {
            var defaults=new Settings();Check(defaults.Language=="ru","existing users keep Russian by default");
            defaults.Language="EN";defaults.Normalize();Check(defaults.Language=="en","English language code is normalized");
            defaults.Language="invalid";defaults.Normalize();Check(defaults.Language=="ru","unknown language safely falls back to Russian");
            var saved=new Settings{Language="en",Hotkey=(int)Keys.F9,Modifiers=2,OutputScale=175,ThemePreset="Custom",SaveFolder=dir};
            string path=Path.Combine(dir,"language-settings.xml");saved.Save(path);var restored=ReadSettings(path);restored.Normalize();
            Check(restored.Language=="en"&&restored.Hotkey==(int)Keys.F9&&restored.Modifiers==2&&restored.OutputScale==175&&restored.ThemePreset=="Custom","language survives restart without changing capture or appearance");
            var draft=saved.Clone();draft.Language="ru";Check(saved.Language=="en","language draft is independent");
            Check(ReadSettings(Path.Combine(dir,"legacy-settings.xml")).Language=="ru","legacy profiles gain Russian language default");
            Loc.Configure("en");
            try
            {
                Check(Loc.T("Настройки")=="Settings"&&Loc.T("Размер области")=="Area size"&&Loc.T("Перо")=="Pen","English translation covers settings, dimensions and editing");
                using(var form=new Form())using(var tips=new ToolTip())using(var menu=new ContextMenuStrip())
                {
                    var label=new Label{Text="Настройки"};var input=new TextBox{Text="Текст"};
                    var combo=new ComboBox();combo.Items.AddRange(new object[]{"По окну","Слева"});combo.SelectedIndex=1;
                    form.Controls.Add(label);form.Controls.Add(input);form.Controls.Add(combo);Loc.SetToolTip(tips,label,"Копировать (Ctrl+C)");
                    menu.Items.Add("Настройки…");Loc.Apply(form);Loc.ApplyMenu(menu);
                    Check(label.Text=="Settings"&&input.Text=="Текст","translation preserves editable user text");
                    Check(combo.SelectedIndex==1&&(string)combo.Items[0]=="Fit to window"&&(string)combo.Items[1]=="Left","language change preserves combo selection");
                    Check(tips.GetToolTip(label)=="Copy (Ctrl+C)"&&menu.Items[0].Text=="Settings…","tooltips and tray menus use the selected language");
                    Loc.Configure("ru");Loc.Apply(form);Loc.ApplyMenu(menu);
                    Check(label.Text=="Настройки"&&combo.SelectedIndex==1&&(string)combo.Items[0]=="По окну"&&tips.GetToolTip(label)=="Копировать (Ctrl+C)"&&menu.Items[0].Text=="Настройки…","live language switch can return to Russian");
                }
            }
            finally{Loc.Configure("ru");}
            Check(Images.FitSelectionSize(new Rectangle(25,30,10,10),new Size(969,568),new Size(1920,1080))==new Rectangle(25,30,969,568),"exact area dimensions preserve the origin when they fit");
            Check(Images.FitSelectionSize(new Rectangle(1800,900,100,100),new Size(1024,768),new Size(1920,1080))==new Rectangle(896,312,1024,768),"near screen edges area moves instead of losing requested dimensions");
            Check(Images.FitSelectionSize(new Rectangle(50,60,100,100),new Size(5000,5000),new Size(1920,1080))==new Rectangle(0,0,1920,1080),"area dimensions cannot exceed captured desktop");
            Check(Images.FitSelectionSize(new Rectangle(2000,-30,10,10),new Size(-1,0),new Size(1920,1080))==new Rectangle(1918,0,2,2),"invalid dimensions and origins remain inside the capture");
            using(var bitmap=new Bitmap(80,60))
            {
                using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.FillRectangle(Brushes.Red,50,40,30,20);}
                var area=Images.FitSelectionSize(new Rectangle(75,55,5,5),new Size(30,20),bitmap.Size);
                using(var crop=bitmap.Clone(area,PixelFormat.Format32bppRgb))Check(crop.Size==new Size(30,20)&&crop.GetPixel(0,0).R==255&&crop.GetPixel(0,0).G==0,"custom area crops original pixels at the fitted coordinates");
            }
        }
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
                CheckAppearance(dir);
                CheckLanguageAndSize(dir);
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
                log.AppendLine("Selection geometry, settings persistence and appearance migration, hotkeys, dimensions, PNG/JPG, annotations, undo/redo, real GDI capture.");
            }
            catch(Exception ex){log.AppendLine(ex.ToString());Environment.ExitCode=1;}
            File.WriteAllText(reportPath,log.ToString(),Encoding.UTF8);
        }
    }
}
