using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SnapLite
{
    public static class Native
    {
        [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
        [DllImport("user32.dll")] public static extern bool GetCursorInfo(ref CursorInfo info);
        [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr h, out IconInfo info);
        [DllImport("user32.dll")] public static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr icon, int cx, int cy, int step, IntPtr brush, int flags);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr h);
        [StructLayout(LayoutKind.Sequential)] public struct CursorInfo { public int Size; public int Flags; public IntPtr Cursor; public Point Position; }
        [StructLayout(LayoutKind.Sequential)] public struct IconInfo { [MarshalAs(UnmanagedType.Bool)] public bool IsIcon; public int X; public int Y; public IntPtr Mask; public IntPtr Color; }
    }

    public class Settings
    {
        public int Version = 1;
        public int Hotkey = (int)Keys.PrintScreen;
        public uint Modifiers = 0;
        public bool Magnifier = true;
        public bool IncludeCursor = false;
        public int DelaySeconds = 0;
        public string CaptureAction = "Editor";
        public int OutputScale = 100;
        public string Format = "PNG";
        public int JpegQuality = 92;
        public string SaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SnapLite");
        public bool Notifications = true;
        public bool CloseAfterAction = true;
        public bool AutoStart = false;
        public int PenWidth = 3;
        public int PenColor = Color.FromArgb(255, 72, 100).ToArgb();
        public string ThemePreset = "Midnight";
        public int BackgroundColor = Color.FromArgb(17,21,34).ToArgb();
        public int PanelColor = Color.FromArgb(35,42,63).ToArgb();
        public int TextColor = Color.FromArgb(239,243,255).ToArgb();
        public int AccentColor = Color.FromArgb(143,124,255).ToArgb();
        public int Accent2Color = Color.FromArgb(103,211,244).ToArgb();
        public int MenuColor = Color.FromArgb(28,34,52).ToArgb();
        public int ButtonColor = Color.FromArgb(49,57,81).ToArgb();
        public int BorderColor = Color.FromArgb(150,169,210).ToArgb();
        public int GlassOpacity = 82;
        public int CornerRadius = 16;
        public string ToolbarPosition = "Top";
        public string ActionPosition = "Right";
        public string SelectionToolbarPosition = "Below";
        public string ButtonOrder = "Copy,Save,QuickSave";
        public string ToolOrder = "Pen,Arrow,Rectangle,Marker,Text,Cover";
        public bool ShowToolLabels = true;
        public static string ProfileDirectory;
        public static string DirectoryPath { get { return ProfileDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SnapLite"); } }
        public static string FilePath { get { return Path.Combine(DirectoryPath, "settings.xml"); } }
        public Settings Clone() { return (Settings)MemberwiseClone(); }
        public void Normalize()
        {
            OutputScale = Math.Max(10, Math.Min(400, OutputScale));
            DelaySeconds = Math.Max(0, Math.Min(10, DelaySeconds));
            JpegQuality = Math.Max(1, Math.Min(100, JpegQuality));
            PenWidth = Math.Max(1, Math.Min(20, PenWidth));
            Modifiers &= 7;
            if (!Hotkeys.IsValid((Keys)Hotkey, Modifiers)) { Hotkey=(int)Keys.PrintScreen; Modifiers=0; }
            if (Format != "PNG" && Format != "JPG") Format="PNG";
            if (CaptureAction != "Editor" && CaptureAction != "Copy" && CaptureAction != "Save") CaptureAction="Editor";
            if (String.IsNullOrWhiteSpace(SaveFolder)) SaveFolder=new Settings().SaveFolder;
            Version=2;
            GlassOpacity=Math.Max(30,Math.Min(100,GlassOpacity));
            CornerRadius=Math.Max(0,Math.Min(28,CornerRadius));
            if(ThemePreset!="Midnight"&&ThemePreset!="Aurora"&&ThemePreset!="Pearl"&&ThemePreset!="Rose"&&ThemePreset!="Graphite"&&ThemePreset!="Custom")ThemePreset="Midnight";
            BackgroundColor=Opaque(BackgroundColor);PanelColor=Opaque(PanelColor);TextColor=Opaque(TextColor);AccentColor=Opaque(AccentColor);
            Accent2Color=Opaque(Accent2Color);MenuColor=Opaque(MenuColor);ButtonColor=Opaque(ButtonColor);BorderColor=Opaque(BorderColor);
            if(ToolbarPosition!="Top"&&ToolbarPosition!="Bottom"&&ToolbarPosition!="Left"&&ToolbarPosition!="Right")ToolbarPosition="Top";
            if(ActionPosition!="Left"&&ActionPosition!="Right")ActionPosition="Right";
            if(SelectionToolbarPosition!="Above"&&SelectionToolbarPosition!="Below")SelectionToolbarPosition="Below";
            ButtonOrder=NormalizeOrder(ButtonOrder,"Copy,Save,QuickSave");
            ToolOrder=NormalizeOrder(ToolOrder,"Pen,Arrow,Rectangle,Marker,Text,Cover");
        }
        private static int Opaque(int argb){return Color.FromArgb(255,Color.FromArgb(argb)).ToArgb();}
        private static string NormalizeOrder(string value,string canonical)
        {
            if(String.IsNullOrEmpty(value))return canonical;
            var expected=canonical.Split(',');var actual=value.Split(',');
            if(actual.Length!=expected.Length)return canonical;
            var seen=new HashSet<string>();foreach(var item in actual)if(Array.IndexOf(expected,item)<0||!seen.Add(item))return canonical;
            return value;
        }
        public static Settings Load(out string warning)
        {
            warning=null;
            if (!File.Exists(FilePath)) return new Settings();
            try { using (var s=File.OpenRead(FilePath)) { var v=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(s); v.Normalize(); return v; } }
            catch (Exception e) { warning="Не удалось прочитать настройки. Используются стандартные.\n"+e.Message; return new Settings(); }
        }
        public void Save(string path)
        {
            Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp=path+".tmp";
            try
            {
                using (var s=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)) new XmlSerializer(typeof(Settings)).Serialize(s,this);
                if (File.Exists(path)) File.Replace(tmp,path,null); else File.Move(tmp,path);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
    }

    public static class Hotkeys
    {
        public static bool IsValid(Keys key, uint mods)
        {
            int n=(int)key;
            if (n==0 || key==Keys.ControlKey || key==Keys.ShiftKey || key==Keys.Menu || key==Keys.LWin || key==Keys.RWin || key==Keys.Escape) return false;
            return (mods&3)!=0 || key==Keys.PrintScreen || (key>=Keys.F1 && key<=Keys.F24);
        }
        public static string Label(int key, uint mods)
        {
            return ((mods&2)!=0?"Ctrl + ":"")+((mods&1)!=0?"Alt + ":"")+((mods&4)!=0?"Shift + ":"")+
                ((Keys)key==Keys.PrintScreen?"Print Screen":((Keys)key).ToString());
        }
    }

    public static class Images
    {
        public static Rectangle FromPoints(Point a,Point b) { return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y)); }
        public static Rectangle Clamp(Rectangle r, Size bounds) { return Rectangle.Intersect(new Rectangle(Point.Empty,bounds),r); }
        public static Bitmap Capture(Rectangle bounds, bool cursor)
        {
            Bitmap bitmap=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppRgb);
            try
            {
                using (Graphics g=Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);
                    if (cursor)
                    {
                        Native.CursorInfo c=new Native.CursorInfo(); c.Size=Marshal.SizeOf(c);
                        Native.IconInfo i;
                        if (Native.GetCursorInfo(ref c) && c.Flags==1 && Native.GetIconInfo(c.Cursor,out i))
                        {
                            try { IntPtr dc=g.GetHdc(); try { Native.DrawIconEx(dc,c.Position.X-bounds.X-i.X,c.Position.Y-bounds.Y-i.Y,c.Cursor,0,0,0,IntPtr.Zero,3); } finally { g.ReleaseHdc(dc); } }
                            finally { if (i.Mask!=IntPtr.Zero) Native.DeleteObject(i.Mask); if (i.Color!=IntPtr.Zero) Native.DeleteObject(i.Color); }
                        }
                    }
                }
                return bitmap;
            }
            catch { bitmap.Dispose(); throw; }
        }
        public static Bitmap Resize(Bitmap source,int percent)
        {
            int w=Math.Max(1,(int)Math.Round(source.Width*percent/100.0));
            int h=Math.Max(1,(int)Math.Round(source.Height*percent/100.0));
            if ((long)w*h > 80000000) throw new InvalidOperationException("Изображение слишком большое. Уменьшите масштаб или область (лимит 80 млн пикселей).");
            var result=new Bitmap(w,h,PixelFormat.Format32bppRgb);
            try { using (var g=Graphics.FromImage(result)) { g.Clear(Color.White); g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality; using (var attr=new ImageAttributes()) { attr.SetWrapMode(WrapMode.TileFlipXY); g.DrawImage(source,new Rectangle(0,0,w,h),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attr); } } return result; }
            catch { result.Dispose(); throw; }
        }
        public static void Encode(Bitmap image,Stream stream,string format,int quality)
        {
            if (format=="JPG")
            {
                var codec=Array.Find(ImageCodecInfo.GetImageEncoders(),delegate(ImageCodecInfo c){return c.MimeType=="image/jpeg";});
                using (var p=new EncoderParameters(1)) { p.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,(long)quality); image.Save(stream,codec,p); }
            }
            else image.Save(stream,ImageFormat.Png);
        }
        public static string SaveUnique(Bitmap image,Settings s)
        {
            Directory.CreateDirectory(s.SaveFolder);
            string stem="SnapLite_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff");
            for (int i=0;i<1000;i++)
            {
                string path=Path.Combine(s.SaveFolder,stem+(i==0?"":"_"+i)+(s.Format=="JPG"?".jpg":".png"));
                FileStream stream;
                try { stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None); }
                catch (IOException) { if (File.Exists(path)) continue; throw; }
                try { using(stream) Encode(image,stream,s.Format,s.JpegQuality); return path; }
                catch { if (File.Exists(path)) File.Delete(path); throw; }
            }
            throw new IOException("Не удалось выбрать свободное имя файла.");
        }
        public static void Copy(Bitmap bitmap)
        {
            // Bitmap plus PNG preserves compatibility with classic and modern paste targets.
            using (var png=new MemoryStream())
            {
                bitmap.Save(png,ImageFormat.Png); png.Position=0;
                var data=new DataObject(); data.SetData(DataFormats.Bitmap,true,bitmap); data.SetData("PNG",false,png);
                Clipboard.SetDataObject(data,true,5,80);
            }
        }
    }

    public enum DrawTool { Pen, Arrow, Rectangle, Marker, Text, Cover }
    public class Mark
    {
        public DrawTool Tool;
        public Color Color;
        public int Width;
        public List<PointF> Points=new List<PointF>();
        public string Text="";
        public void Paint(Graphics g)
        {
            if (Points.Count==0) return;
            PointF a=Points[0], b=Points[Points.Count-1];
            using (Pen pen=new Pen(Tool==DrawTool.Marker?Color.FromArgb(80,Color):Color,Tool==DrawTool.Marker?Width*6:Width))
            {
                pen.StartCap=LineCap.Round; pen.EndCap=LineCap.Round; pen.LineJoin=LineJoin.Round;
                if (Tool==DrawTool.Pen || Tool==DrawTool.Marker)
                {
                    if (Points.Count>1) g.DrawLines(pen,Points.ToArray());
                    else using (var brush=new SolidBrush(pen.Color)) g.FillEllipse(brush,a.X-pen.Width/2,a.Y-pen.Width/2,pen.Width,pen.Width);
                }
                else if (Tool==DrawTool.Arrow)
                { using(var cap=new AdjustableArrowCap(4,5,true)) { pen.CustomEndCap=cap; if (a!=b) g.DrawLine(pen,a,b); } }
                else if (Tool==DrawTool.Rectangle || Tool==DrawTool.Cover)
                {
                    var r=RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
                    if (Tool==DrawTool.Cover) using (var brush=new SolidBrush(Color)) g.FillRectangle(brush,r);
                    else g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);
                }
                else if (Tool==DrawTool.Text)
                { using(var f=new Font("Segoe UI",Math.Max(14,Width*6),FontStyle.Bold,GraphicsUnit.Pixel)) using(var br=new SolidBrush(Color)) g.DrawString(Text,f,br,a); }
            }
        }
    }
    public class Document : IDisposable
    {
        public Bitmap Image { get; private set; }
        public readonly List<Mark> Marks=new List<Mark>();
        private readonly Stack<Mark> redo=new Stack<Mark>();
        public Document(Bitmap image) { Image=image; }
        public void Add(Mark m) { Marks.Add(m); redo.Clear(); }
        public void Undo() { if(Marks.Count>0) { redo.Push(Marks[Marks.Count-1]); Marks.RemoveAt(Marks.Count-1); } }
        public void Redo() { if(redo.Count>0) Marks.Add(redo.Pop()); }
        public void Paint(Graphics g) { g.DrawImageUnscaled(Image,0,0); g.SmoothingMode=SmoothingMode.AntiAlias; foreach(var m in Marks) m.Paint(g); }
        public Bitmap Render(int scale)
        {
            using(var raw=new Bitmap(Image.Width,Image.Height,PixelFormat.Format32bppRgb))
            { using(var g=Graphics.FromImage(raw)) Paint(g); return Images.Resize(raw,scale); }
        }
        public void Dispose() { if(Image!=null) {Image.Dispose();Image=null;} Marks.Clear();redo.Clear(); }
    }
    public static class Theme
    {
        public static Color Background {get{return Glass.Background;}}
        public static Color Ink {get{return Glass.Text;}}
        public static Color Accent {get{return Glass.Accent;}}
        public static Color Muted {get{return Glass.Muted;}}
        public static Button Button(string text,int width,bool accent)
        {
            return new GlassButton {Text=text,Width=width,Height=36,Primary=accent,Cursor=Cursors.Hand,Margin=new Padding(4)};
        }
        public static Icon Icon()
        {
            using(var b=new Bitmap(32,32))
            {
                using(var g=Graphics.FromImage(b))
                {
                    g.SmoothingMode=SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
                    using(var br=new SolidBrush(Accent)) g.FillEllipse(br,1,1,30,30);
                    using(var p=new Pen(Color.White,2.5f)) {g.DrawRectangle(p,8,10,16,12);g.DrawLine(p,12,7,20,7);g.DrawEllipse(p,13,13,6,6);}
                }
                IntPtr h=b.GetHicon();try {using(var borrowed=System.Drawing.Icon.FromHandle(h)) return (Icon)borrowed.Clone();}finally {Native.DestroyIcon(h);}
            }
        }
    }
}
