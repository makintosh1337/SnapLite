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
            using(var f=new GlassForm {Text=Loc.T("Текст на скриншоте"),ClientSize=new Size(400,202),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false})
            {
                var title=new Label {Text=Loc.T("Добавить подпись"),AutoSize=true,ForeColor=Glass.Text,Font=new Font("Segoe UI",12,FontStyle.Bold),Location=new Point(18,16),BackColor=Color.Transparent};f.Controls.Add(title);
                var field=new GlassPanel {Bounds=new Rectangle(18,48,364,90),Padding=new Padding(12)};
                var box=new TextBox {Multiline=true,AcceptsReturn=true,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Glass.Surface,ForeColor=Glass.Text,Font=Glass.UiFont};field.Controls.Add(box);f.Controls.Add(field);
                var ok=new GlassButton {Text=Loc.T("Добавить"),Width=112,Primary=true,Bounds=new Rectangle(270,153,112,34),DialogResult=DialogResult.OK};f.Controls.Add(ok);
                var cancel=new GlassButton {Text=Loc.T("Отмена"),Bounds=new Rectangle(164,153,98,34),DialogResult=DialogResult.Cancel};f.Controls.Add(cancel);f.AcceptButton=ok;f.CancelButton=cancel;
                f.Shown+=delegate{box.Focus();};
                return f.ShowDialog(owner)==DialogResult.OK?box.Text:null;
            }
        }
    }

    // An inexpensive static surface: there is no animation or idle rendering timer.
    internal sealed class EditorViewport : Panel
    {
        public EditorViewport(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Glass.Mix(Glass.Background,Color.Black,.12f));
            using(var p=new Pen(Color.FromArgb(13,Glass.Text)))
                for(int x=0;x<Width;x+=24)for(int y=0;y<Height;y+=24)e.Graphics.DrawRectangle(p,x,y,1,1);
        }
    }

    internal sealed class InkButton : Control
    {
        private Color ink;
        private bool hover;
        public Color Ink {get{return ink;}set{ink=value;Invalidate();}}
        public InkButton(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.Selectable,true);Size=new Size(38,38);Cursor=Cursors.Hand;TabStop=true;DoubleBuffered=true;BackColor=Color.Transparent;}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            Glass.PaintCard(e.Graphics,ClientRectangle,hover?Glass.Mix(Glass.Surface,Glass.Text,.10f):Glass.Surface,Math.Min(Glass.Radius,10));
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            int x=(Width-16)/2,y=(Height-16)/2;
            using(var b=new SolidBrush(ink))e.Graphics.FillEllipse(b,x,y,16,16);
            using(var p=new Pen(Color.FromArgb(130,Glass.Text),Focused?2:1))e.Graphics.DrawEllipse(p,x-2,y-2,20,20);
            base.OnPaint(e);
        }
    }

    public sealed class EditorForm : GlassForm
    {
        private readonly Document document;
        private Settings settings;
        private readonly Action<string> notify;
        private readonly DrawingCanvas canvas;
        private readonly Panel viewport;
        private readonly GlassNumber outputScale;
        private readonly Label status;
        private readonly GlassCombo zoom;
        private readonly GlassPanel toolsHost;
        private readonly FlowLayoutPanel tools;
        private readonly FlowLayoutPanel actions,scaleGroup;
        private bool verticalTools;
        private bool fit=true, arrangingTools;
        private readonly ToolTip tip=new ToolTip {AutoPopDelay=7000,InitialDelay=450,ReshowDelay=100};

        public EditorForm(Bitmap image,Settings settings,Action<string> notify)
        {
            Loc.Configure(settings.Language);
            document=new Document(image);this.settings=settings;this.notify=notify;
            Text=Loc.T("SnapLite — редактор");Icon=Theme.Icon();
            AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;MinimumSize=new Size(780,480);
            var area=Screen.FromPoint(Cursor.Position).WorkingArea;
            Size=new Size(Math.Min(area.Width-40,1120),Math.Min(area.Height-40,790));KeyPreview=true;
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=new Padding(14,0,14,8),BackColor=Color.Transparent};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,64));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,25));Controls.Add(layout);
            var header=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.Transparent};layout.Controls.Add(header,0,0);
            var title=new Label {Text=Loc.T("Редактор"),Font=new Font("Segoe UI Semibold",17,FontStyle.Bold),ForeColor=Glass.Text,BackColor=Color.Transparent,AutoSize=true,Location=new Point(0,12)};header.Controls.Add(title);
            var subtitle=new Label {Text="SNAPLITE  /  "+image.Width+" × "+image.Height,Font=new Font("Segoe UI",9),ForeColor=Glass.Muted,Tag="muted",TextAlign=ContentAlignment.MiddleRight,Dock=DockStyle.Right,Width=270,BackColor=Color.Transparent};header.Controls.Add(subtitle);

            var workspace=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.Transparent};layout.Controls.Add(workspace,0,1);
            canvas=new DrawingCanvas(document){Ink=Color.FromArgb(settings.PenColor),PenWidth=settings.PenWidth};
            viewport=new EditorViewport {Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(18)};viewport.Controls.Add(canvas);canvas.Location=new Point(18,18);workspace.Controls.Add(viewport);
            verticalTools=settings.ToolbarPosition=="Left"||settings.ToolbarPosition=="Right";
            toolsHost=new GlassPanel {Dock=settings.ToolbarPosition=="Bottom"?DockStyle.Bottom:(settings.ToolbarPosition=="Left"?DockStyle.Left:(settings.ToolbarPosition=="Right"?DockStyle.Right:DockStyle.Top)),Padding=new Padding(8),Margin=Padding.Empty,Height=60,Width=152};workspace.Controls.Add(toolsHost);viewport.BringToFront();
            tools=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=verticalTools?FlowDirection.TopDown:FlowDirection.LeftToRight,WrapContents=!verticalTools,AutoScroll=verticalTools,BackColor=Color.Transparent,Padding=Padding.Empty,Margin=Padding.Empty};toolsHost.Controls.Add(tools);
            string[] names={Loc.T("Перо"),Loc.T("Стрелка"),Loc.T("Рамка"),Loc.T("Маркер"),Loc.T("Текст"),Loc.T("Заливка")};string[] glyphs={"✎","↗","□","▰","T","■"};
            foreach(string toolName in settings.ToolOrder.Split(','))
            {
                DrawTool tool=(DrawTool)Enum.Parse(typeof(DrawTool),toolName);int i=(int)tool;var b=new GlassButton {Text=settings.ShowToolLabels?names[i]:"",Glyph=glyphs[i],Tag=tool,Selected=tool==DrawTool.Pen,Size=new Size(verticalTools?106:(settings.ShowToolLabels?100:38),38),Margin=new Padding(2,2,4,2),Font=new Font("Segoe UI",9)};
                Loc.SetToolTip(tip,b,i==5?Loc.T("Заливка: закройте фрагмент выбранным цветом"):names[i]);
                b.Click+=delegate(object s,EventArgs e){foreach(Control item in tools.Controls){var button=item as GlassButton;if(button!=null&&button.Tag is DrawTool)button.Selected=button==s;}canvas.Tool=tool;canvas.Focus();};tools.Controls.Add(b);
            }
            var color=new InkButton {Ink=canvas.Ink,Margin=new Padding(6,2,4,2)};Loc.SetToolTip(tip,color,Loc.T("Цвет пера, стрелок, текста и заливки"));color.Click+=delegate{using(var d=new ColorDialog {Color=canvas.Ink,FullOpen=true})if(d.ShowDialog(this)==DialogResult.OK){canvas.Ink=d.Color;color.Ink=d.Color;}};tools.Controls.Add(color);
            var width=new GlassCombo {Tag="PenWidth",DropDownStyle=ComboBoxStyle.DropDownList,Width=verticalTools?106:76,Margin=new Padding(2,6,6,2)};foreach(int w in new[]{1,2,3,5,8,12,16,20})width.Items.Add(w+" px");width.SelectedItem=settings.PenWidth+" px";if(width.SelectedIndex<0)width.SelectedIndex=2;width.SelectedIndexChanged+=delegate{if(!Loc.Applying&&!String.IsNullOrEmpty(width.Text))canvas.PenWidth=int.Parse(width.Text.Split(' ')[0]);};Loc.SetToolTip(tip,width,Loc.T("Толщина пера в пикселях"));tools.Controls.Add(width);
            var undo=new GlassButton {Glyph="↶",Size=new Size(verticalTools?50:36,38),Margin=new Padding(2,2,2,2)};undo.Click+=delegate{document.Undo();canvas.Invalidate();};Loc.SetToolTip(tip,undo,Loc.T("Отменить (Ctrl+Z)"));tools.Controls.Add(undo);
            var redo=new GlassButton {Glyph="↷",Size=new Size(verticalTools?50:36,38),Margin=new Padding(2,2,6,2)};redo.Click+=delegate{document.Redo();canvas.Invalidate();};Loc.SetToolTip(tip,redo,Loc.T("Вернуть (Ctrl+Y)"));tools.Controls.Add(redo);
            zoom=new GlassCombo {Width=106,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(2,6,2,2)};zoom.Items.AddRange(new object[]{Loc.T("По окну"),"25%","50%","75%","100%","150%","200%","400%"});zoom.SelectedIndex=0;zoom.SelectedIndexChanged+=delegate{if(Loc.Applying)return;fit=zoom.SelectedIndex==0;UpdateZoom();};Loc.SetToolTip(tip,zoom,Loc.T("Масштаб просмотра; размер сохраняемого файла задаётся ниже"));tools.Controls.Add(zoom);
            if(verticalTools){color.Width=106;undo.Width=106;redo.Width=106;}
            toolsHost.Resize+=delegate{ArrangeTools();};viewport.Resize+=delegate{if(fit)UpdateZoom();};

            var footer=new Panel {Dock=DockStyle.Fill,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(0,12,0,0)};layout.Controls.Add(footer,0,2);
            scaleGroup=new FlowLayoutPanel {Dock=settings.ActionPosition=="Left"?DockStyle.Right:DockStyle.Left,Width=180,WrapContents=false,BackColor=Color.Transparent,Padding=new Padding(0,3,0,0),Margin=Padding.Empty};footer.Controls.Add(scaleGroup);
            scaleGroup.Controls.Add(new Label {Text=Loc.T("Вывод"),AutoSize=true,ForeColor=Glass.Muted,Tag="muted",BackColor=Color.Transparent,Margin=new Padding(0,7,8,0)});
            outputScale=new GlassNumber {Minimum=10,Maximum=400,Increment=10,Value=settings.OutputScale,Width=69,Margin=new Padding(0,3,3,0)};scaleGroup.Controls.Add(outputScale);scaleGroup.Controls.Add(new Label{Text="%",AutoSize=true,ForeColor=Glass.Muted,Tag="muted",BackColor=Color.Transparent,Margin=new Padding(0,7,0,0)});Loc.SetToolTip(tip,outputScale,Loc.T("Размер изображения при копировании и сохранении (10–400%)"));
            actions=new FlowLayoutPanel {Dock=settings.ActionPosition=="Left"?DockStyle.Left:DockStyle.Right,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty};footer.Controls.Add(actions);
            foreach(string token in settings.ButtonOrder.Split(','))
            {
                GlassButton b;
                if(token=="Copy"){b=new GlassButton {Text=settings.ShowToolLabels?Loc.T("Копировать"):"",Glyph="▣",Primary=true,Size=new Size(settings.ShowToolLabels?142:44,40)};b.Click+=delegate{CopyImage();};Loc.SetToolTip(tip,b,Loc.T("Копировать (Ctrl+C)"));}
                else if(token=="Save"){b=new GlassButton {Text=settings.ShowToolLabels?Loc.T("Сохранить как"):"",Glyph="↓",Size=new Size(settings.ShowToolLabels?158:44,40)};b.Click+=delegate{SaveImage(true);};Loc.SetToolTip(tip,b,Loc.T("Сохранить как… (Ctrl+S)"));}
                else if(token=="QuickSave"){b=new GlassButton {Text=settings.ShowToolLabels?Loc.T("В папку"):"",Glyph="↧",Size=new Size(settings.ShowToolLabels?118:44,40)};b.Click+=delegate{SaveImage(false);};Loc.SetToolTip(tip,b,Loc.T("Быстро сохранить в папку из настроек (Ctrl+Shift+S)"));}
                else continue;
                b.Tag=token;b.Margin=new Padding(4,0,0,0);actions.Controls.Add(b);
            }
            status=new Label {Dock=DockStyle.Fill,ForeColor=Glass.Muted,Tag="muted",Font=new Font("Segoe UI",8.5f),BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty,AutoEllipsis=true};layout.Controls.Add(status,0,3);
            outputScale.ValueChanged+=delegate{UpdateStatus();};Shown+=delegate{ArrangeTools();UpdateZoom();UpdateStatus();canvas.Focus();};
        }

        public void ApplyAppearance(Settings value)
        {
            if(value==null||IsDisposed)return;
            settings=value;Loc.Configure(value.Language);Glass.Configure(value);
            Point scroll=viewport.AutoScrollPosition;
            Control workspace=toolsHost.Parent,footer=actions.Parent;
            SuspendLayout();workspace.SuspendLayout();footer.SuspendLayout();tools.SuspendLayout();actions.SuspendLayout();arrangingTools=true;
            try
            {
                verticalTools=value.ToolbarPosition=="Left"||value.ToolbarPosition=="Right";
                toolsHost.Dock=DockStyle.None;
                tools.AutoScroll=false;tools.AutoScrollPosition=Point.Empty;
                tools.FlowDirection=verticalTools?FlowDirection.TopDown:FlowDirection.LeftToRight;
                tools.WrapContents=!verticalTools;
                string[] names={Loc.T("Перо"),Loc.T("Стрелка"),Loc.T("Рамка"),Loc.T("Маркер"),Loc.T("Текст"),Loc.T("Заливка")};
                foreach(Control item in tools.Controls)
                {
                    var button=item as GlassButton;
                    if(button!=null&&button.Tag is DrawTool)
                    {
                        button.Text=value.ShowToolLabels?names[(int)(DrawTool)button.Tag]:"";
                        button.Width=verticalTools?106:(value.ShowToolLabels?100:38);
                        button.Selected=(DrawTool)button.Tag==canvas.Tool;
                    }
                    else if(item is InkButton){item.Width=verticalTools?106:38;((InkButton)item).Ink=canvas.Ink;}
                    else if(button!=null)item.Width=verticalTools?106:36;
                    else if(Convert.ToString(item.Tag)=="PenWidth"){item.Width=verticalTools?106:76;((ComboBox)item).SelectedItem=canvas.PenWidth+" px";}
                }
                int index=0;
                foreach(string name in value.ToolOrder.Split(','))
                {
                    DrawTool desired=(DrawTool)Enum.Parse(typeof(DrawTool),name);
                    Control found=null;foreach(Control item in tools.Controls)if(item.Tag is DrawTool&&(DrawTool)item.Tag==desired){found=item;break;}
                    if(found!=null)tools.Controls.SetChildIndex(found,index++);
                }
                toolsHost.Width=152;toolsHost.Height=58;
                toolsHost.Dock=value.ToolbarPosition=="Bottom"?DockStyle.Bottom:(value.ToolbarPosition=="Left"?DockStyle.Left:(value.ToolbarPosition=="Right"?DockStyle.Right:DockStyle.Top));
                tools.AutoScroll=verticalTools;viewport.BringToFront();
                foreach(Control item in actions.Controls)
                {
                    var button=(GlassButton)item;string token=(string)button.Tag;
                    string text=token=="Copy"?Loc.T("Копировать"):(token=="Save"?Loc.T("Сохранить как"):Loc.T("В папку"));
                    button.Text=value.ShowToolLabels?text:"";
                    button.Width=value.ShowToolLabels?(token=="Copy"?142:(token=="Save"?158:118)):44;
                }
                index=0;
                foreach(string token in value.ButtonOrder.Split(','))
                {
                    Control found=null;foreach(Control item in actions.Controls)if((string)item.Tag==token){found=item;break;}
                    if(found!=null)actions.Controls.SetChildIndex(found,index++);
                }
                actions.Dock=value.ActionPosition=="Left"?DockStyle.Left:DockStyle.Right;
                scaleGroup.Dock=value.ActionPosition=="Left"?DockStyle.Right:DockStyle.Left;
                Loc.Apply(this);Glass.Apply(this);
            }
            finally
            {
                actions.ResumeLayout(true);tools.ResumeLayout(true);footer.ResumeLayout(true);workspace.ResumeLayout(true);ResumeLayout(true);arrangingTools=false;
            }
            ArrangeTools();UpdateZoom();
            if(!fit)viewport.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);
            UpdateStatus();Invalidate(true);
        }

        private void ArrangeTools()
        {
            if(arrangingTools||verticalTools||tools==null)return;arrangingTools=true;
            try
            {
                int available=Math.Max(100,toolsHost.ClientSize.Width-toolsHost.Padding.Horizontal),used=0,rows=1;
                foreach(Control item in tools.Controls){int width=item.Width+item.Margin.Horizontal;if(used>0&&used+width>available){rows++;used=0;}used+=width;}
                toolsHost.Height=16+rows*42;
            }
            finally{arrangingTools=false;}
        }
        private void UpdateZoom()
        {
            if(viewport==null)return;
            float z=fit?Math.Min(1,Math.Min(Math.Max(1,viewport.ClientSize.Width-42)/(float)document.Image.Width,Math.Max(1,viewport.ClientSize.Height-42)/(float)document.Image.Height)):float.Parse(zoom.Text.TrimEnd('%'),CultureInfo.InvariantCulture)/100;
            canvas.SetZoom(z);if(fit)viewport.AutoScrollPosition=Point.Empty;UpdateStatus();
        }
        private void UpdateStatus()
        {
            if(status==null)return;int scale=(int)outputScale.Value;
            status.Text=document.Image.Width+" × "+document.Image.Height+Loc.T(" px   ·   Вывод ")+Math.Max(1,(int)Math.Round(document.Image.Width*scale/100.0))+" × "+Math.Max(1,(int)Math.Round(document.Image.Height*scale/100.0))+Loc.T(" px   ·   Просмотр ")+Math.Round(canvas.Zoom*100)+Loc.T("%   ·   Ctrl+Z — отмена");
        }
        private void CopyImage()
        {
            try {using(var image=document.Render((int)outputScale.Value))Images.Copy(image);notify(Loc.T("Скриншот скопирован в буфер обмена."));if(settings.CloseAfterAction)Close();}
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
                        using(var d=new SaveFileDialog {Title=Loc.T("Сохранить скриншот"),Filter="PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg",FilterIndex=settings.Format=="JPG"?2:1,FileName="SnapLite_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),InitialDirectory=Directory.Exists(settings.SaveFolder)?settings.SaveFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),AddExtension=true,OverwritePrompt=true})
                        {if(d.ShowDialog(this)!=DialogResult.OK)return;path=d.FileName;using(var s=new FileStream(path,FileMode.Create,FileAccess.Write))Images.Encode(image,s,d.FilterIndex==2?"JPG":"PNG",settings.JpegQuality);}
                    }
                    else path=Images.SaveUnique(image,settings);
                    notify(Loc.T("Сохранено: ")+path);
                }
                if(settings.CloseAfterAction)Close();
            }
            catch(Exception ex){Error(ex);}
        }
        private void Error(Exception ex){MessageBox.Show(this,ex.Message,Loc.T("SnapLite — действие не выполнено"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}
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
