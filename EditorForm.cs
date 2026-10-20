using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed class DrawingCanvas : Control
    {
        public Document Document;
        public DrawTool Tool;
        public Color Ink { get; set; }
        public int PenWidth { get; set; }
        public float Zoom=1;
        private Mark current;
        public DrawingCanvas(Document doc)
        {
            Document=doc;Ink=Color.FromArgb(255,72,100);PenWidth=3;DoubleBuffered=true;BackColor=Color.White;Cursor=Cursors.Cross;TabStop=true;
            SetStyle(ControlStyles.ResizeRedraw,true);
        }
        public void SetZoom(float zoom)
        {
            Zoom=Math.Max(.1f,Math.Min(4,zoom));Size=new Size((int)Math.Ceiling(Document.Image.Width*Zoom),(int)Math.Ceiling(Document.Image.Height*Zoom));Invalidate();
        }
        private PointF ImagePoint(Point p){return new PointF(Math.Max(0,Math.Min(Document.Image.Width-1,p.X/Zoom)),Math.Max(0,Math.Min(Document.Image.Height-1,p.Y/Zoom)));}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.ScaleTransform(Zoom,Zoom);g.InterpolationMode=Zoom>=1?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;
            Document.Paint(g);if(current!=null)current.Paint(g);base.OnPaint(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();
            if(Tool==DrawTool.Text)
            {
                string text=TextPrompt.Ask(FindForm());if(!String.IsNullOrWhiteSpace(text)){var mark=new Mark{Tool=Tool,Color=Ink,Width=PenWidth,Text=text};mark.Points.Add(ImagePoint(e.Location));Document.Add(mark);Invalidate();}return;
            }
            current=new Mark{Tool=Tool,Color=Ink,Width=PenWidth};current.Points.Add(ImagePoint(e.Location));Capture=true;Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(current==null)return;
            var p=ImagePoint(e.Location);
            if(Tool==DrawTool.Pen||Tool==DrawTool.Marker)current.Points.Add(p);
            else {if(current.Points.Count>1)current.Points.RemoveAt(1);current.Points.Add(p);}Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(current==null)return;Document.Add(current);current=null;Capture=false;Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);if(!Capture && current!=null){Document.Add(current);current=null;Invalidate();}
        }
    }
    public static class TextPrompt
    {
        public static string Ask(IWin32Window owner)
        {
            using(var f=new Form {Text="Текст на скриншоте",ClientSize=new Size(430,190),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false,Font=new Font("Segoe UI",10)})
            {
                var box=new TextBox {Multiline=true,Bounds=new Rectangle(16,16,398,110)};f.Controls.Add(box);
                var ok=Theme.Button("Добавить",112,true);ok.Location=new Point(302,140);ok.DialogResult=DialogResult.OK;f.Controls.Add(ok);
                var cancel=Theme.Button("Отмена",100,false);cancel.Location=new Point(190,140);cancel.DialogResult=DialogResult.Cancel;f.Controls.Add(cancel);f.AcceptButton=ok;f.CancelButton=cancel;
                return f.ShowDialog(owner)==DialogResult.OK?box.Text:null;
            }
        }
    }
    public sealed class EditorForm : Form
    {
        private readonly Document document;
        private readonly Settings settings;
        private readonly Action<string> notify;
        private readonly DrawingCanvas canvas;
        private readonly Panel viewport;
        private readonly NumericUpDown outputScale;
        private readonly ToolStripStatusLabel status;
        private readonly ToolStripComboBox zoom;
        private bool fit=true;
        private readonly ToolTip tip=new ToolTip();
        public EditorForm(Bitmap image,Settings settings,Action<string> notify)
        {
            document=new Document(image);this.settings=settings;this.notify=notify;
            Text="SnapLite — редактор";Icon=Theme.Icon();Font=new Font("Segoe UI",10);BackColor=Theme.Background;
            AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;MinimumSize=new Size(830,480);
            var area=Screen.FromPoint(Cursor.Position).WorkingArea;
            Size=new Size(Math.Min(area.Width-40,1120),Math.Min(area.Height-40,790));KeyPreview=true;
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,57));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,26));Controls.Add(layout);
            var tools=new ToolStrip {Dock=DockStyle.Fill,GripStyle=ToolStripGripStyle.Hidden,BackColor=Color.White,Padding=new Padding(8,4,8,4),RenderMode=ToolStripRenderMode.System};layout.Controls.Add(tools,0,0);
            canvas=new DrawingCanvas(document){Ink=Color.FromArgb(settings.PenColor),PenWidth=settings.PenWidth};
            string[] names={"Перо","Стрелка","Рамка","Маркер","Текст","Заливка"};
            for(int i=0;i<names.Length;i++)
            {
                DrawTool tool=(DrawTool)i;var b=new ToolStripButton(names[i]){Tag=tool,Checked=i==0,DisplayStyle=ToolStripItemDisplayStyle.Text,ToolTipText=i==5?"Сплошная заливка выбранным цветом":names[i]};
                b.Click+=delegate(object s,EventArgs e){foreach(ToolStripItem it in tools.Items)if(it is ToolStripButton && it.Tag is DrawTool)((ToolStripButton)it).Checked=it==s;canvas.Tool=tool;};tools.Items.Add(b);
            }
            tools.Items.Add(new ToolStripSeparator());
            var color=new ToolStripButton("Цвет") {BackColor=canvas.Ink,ToolTipText="Выбрать цвет"};color.Click+=delegate{using(var d=new ColorDialog {Color=canvas.Ink,FullOpen=true})if(d.ShowDialog(this)==DialogResult.OK){canvas.Ink=d.Color;color.BackColor=d.Color;}};tools.Items.Add(color);
            var width=new ToolStripComboBox {DropDownStyle=ComboBoxStyle.DropDownList,AutoSize=false,Width=48,ToolTipText="Толщина пера в пикселях"};foreach(int w in new[]{1,2,3,5,8,12,16,20})width.Items.Add(w.ToString());width.SelectedItem=settings.PenWidth.ToString();if(width.SelectedIndex<0)width.SelectedIndex=2;width.SelectedIndexChanged+=delegate{canvas.PenWidth=int.Parse(width.Text);};tools.Items.Add(width);
            tools.Items.Add(new ToolStripSeparator());var undo=new ToolStripButton("↶") {ToolTipText="Отменить (Ctrl+Z)"};undo.Click+=delegate{document.Undo();canvas.Invalidate();};tools.Items.Add(undo);
            var redo=new ToolStripButton("↷") {ToolTipText="Вернуть (Ctrl+Y)"};redo.Click+=delegate{document.Redo();canvas.Invalidate();};tools.Items.Add(redo);
            tools.Items.Add(new ToolStripSeparator());
            zoom=new ToolStripComboBox {AutoSize=false,Width=100,DropDownStyle=ComboBoxStyle.DropDownList,ToolTipText="Масштаб просмотра, не влияет на файл"};zoom.Items.AddRange(new object[]{"По окну","25%","50%","75%","100%","150%","200%","400%"});zoom.SelectedIndex=0;zoom.SelectedIndexChanged+=delegate{fit=zoom.SelectedIndex==0;UpdateZoom();};tools.Items.Add(zoom);
            viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(222,227,237),Padding=new Padding(16)};viewport.Controls.Add(canvas);canvas.Location=new Point(16,16);layout.Controls.Add(viewport,0,1);viewport.Resize+=delegate{if(fit)UpdateZoom();};
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(8,7,8,4),BackColor=Color.White,WrapContents=false};layout.Controls.Add(footer,0,2);
            footer.Controls.Add(new Label{Text="В файл:",AutoSize=true,Margin=new Padding(4,10,0,0)});
            outputScale=new NumericUpDown{Minimum=10,Maximum=400,Increment=10,Value=settings.OutputScale,Width=65,Margin=new Padding(4,7,0,0)};footer.Controls.Add(outputScale);footer.Controls.Add(new Label{Text="%",AutoSize=true,Margin=new Padding(1,10,14,0)});tip.SetToolTip(outputScale,"Масштаб изображения при копировании и сохранении");
            var copy=Theme.Button("Копировать",122,true);copy.Click+=delegate{CopyImage();};tip.SetToolTip(copy,"Ctrl+C");footer.Controls.Add(copy);
            var save=Theme.Button("Сохранить как…",154,false);save.Click+=delegate{SaveImage(true);};tip.SetToolTip(save,"Ctrl+S");footer.Controls.Add(save);
            var quick=Theme.Button("В папку",104,false);quick.Click+=delegate{SaveImage(false);};tip.SetToolTip(quick,"Быстрое сохранение в папку из настроек (Ctrl+Shift+S)");footer.Controls.Add(quick);
            var strip=new StatusStrip{Dock=DockStyle.Fill,BackColor=Theme.Background,SizingGrip=false};status=new ToolStripStatusLabel();strip.Items.Add(status);layout.Controls.Add(strip,0,3);
            outputScale.ValueChanged+=delegate{UpdateStatus();};Shown+=delegate{UpdateZoom();UpdateStatus();canvas.Focus();};
        }
        private void UpdateZoom()
        {
            if(viewport==null)return;
            float z=fit?Math.Min(1,Math.Min(Math.Max(1,viewport.ClientSize.Width-38)/(float)document.Image.Width,Math.Max(1,viewport.ClientSize.Height-38)/(float)document.Image.Height)):float.Parse(zoom.Text.TrimEnd('%'),CultureInfo.InvariantCulture)/100;
            canvas.SetZoom(z);if(fit)viewport.AutoScrollPosition=Point.Empty;UpdateStatus();
        }
        private void UpdateStatus()
        {
            if(status==null)return;int scale=(int)outputScale.Value;
            status.Text="  "+document.Image.Width+" × "+document.Image.Height+" px   •   Вывод: "+Math.Max(1,(int)Math.Round(document.Image.Width*scale/100.0))+" × "+Math.Max(1,(int)Math.Round(document.Image.Height*scale/100.0))+" px   •   Просмотр: "+Math.Round(canvas.Zoom*100)+"%   •   Ctrl+Z — отмена";
        }
        private void CopyImage()
        {
            try {using(var image=document.Render((int)outputScale.Value))Images.Copy(image);notify("Скриншот скопирован в буфер обмена.");if(settings.CloseAfterAction)Close();}
            catch(Exception ex){Error(ex);}
        }
        private void SaveImage(bool dialog)
        {
            try
            {
                using(var image=document.Render((int)outputScale.Value))
                {
                    string path;
                    if(dialog)
                    {
                        using(var d=new SaveFileDialog {Title="Сохранить скриншот",Filter="PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg",FilterIndex=settings.Format=="JPG"?2:1,FileName="SnapLite_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),InitialDirectory=Directory.Exists(settings.SaveFolder)?settings.SaveFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),AddExtension=true,OverwritePrompt=true})
                        {if(d.ShowDialog(this)!=DialogResult.OK)return;path=d.FileName;using(var s=new FileStream(path,FileMode.Create,FileAccess.Write))Images.Encode(image,s,d.FilterIndex==2?"JPG":"PNG",settings.JpegQuality);}
                    }
                    else path=Images.SaveUnique(image,settings);
                    notify("Сохранено: "+path);
                }
                if(settings.CloseAfterAction)Close();
            }
            catch(Exception ex){Error(ex);}
        }
        private void Error(Exception ex){MessageBox.Show(this,ex.Message,"SnapLite — действие не выполнено",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            if(keyData==(Keys.Control|Keys.Z)){document.Undo();canvas.Invalidate();return true;}
            if(keyData==(Keys.Control|Keys.Y)){document.Redo();canvas.Invalidate();return true;}
            if(keyData==(Keys.Control|Keys.C)){CopyImage();return true;}
            if(keyData==(Keys.Control|Keys.S)){SaveImage(true);return true;}
            if(keyData==(Keys.Control|Keys.Shift|Keys.S)){SaveImage(false);return true;}
            if(keyData==Keys.Escape){Close();return true;}
            return base.ProcessCmdKey(ref msg,keyData);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){settings.PenColor=canvas.Ink.ToArgb();settings.PenWidth=canvas.PenWidth;document.Dispose();tip.Dispose();}base.Dispose(disposing);
        }
    }
}
