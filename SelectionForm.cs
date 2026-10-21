using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed class SelectionForm : Form
    {
        private readonly Bitmap screen;
        private readonly Rectangle desktop;
        private readonly bool magnifier;
        private readonly Font labelFont=new Font("Segoe UI",10);
        private readonly Font titleFont=new Font("Segoe UI",13,FontStyle.Bold);
        private Point mouse,origin;
        private Rectangle initial;
        private bool dragging;
        private int hit=-2;
        private readonly GlassPanel actions;
        private readonly Settings appearance;
        private readonly ToolTip tip=new ToolTip();
        public Rectangle Selection {get;private set;}
        public string RequestedAction {get;private set;}
        public SelectionForm(Bitmap screen,Rectangle desktop,bool magnifier,string defaultAction):this(screen,desktop,magnifier,defaultAction,new Settings()){}
        public SelectionForm(Bitmap screen,Rectangle desktop,bool magnifier,string defaultAction,Settings settings)
        {
            this.screen=screen;this.desktop=desktop;this.magnifier=magnifier;appearance=settings;
            RequestedAction=defaultAction;
            Text="SnapLite — выделите область"; FormBorderStyle=FormBorderStyle.None;
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.Manual; Bounds=desktop;
            TopMost=true;ShowInTaskbar=false;DoubleBuffered=true;KeyPreview=true;Cursor=Cursors.Cross;
            mouse=PointToClient(System.Windows.Forms.Cursor.Position);
            actions=new GlassPanel {Padding=new Padding(6),Visible=false,Cursor=Cursors.Default};
            var row=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty};actions.Controls.Add(row);
            int actionWidth=0;
            Action<string,string,string,int> add=delegate(string action,string text,string glyph,int width)
            {
                int buttonWidth=settings.ShowToolLabels?width:38;
                var button=new GlassButton {Text=settings.ShowToolLabels?text:"",Glyph=glyph,Primary=action==defaultAction,Size=new Size(buttonWidth,36),Margin=new Padding(2),Font=new Font("Segoe UI",9)};
                button.Click+=delegate{RequestedAction=action;Complete();};tip.SetToolTip(button,text+(action==defaultAction?" (Enter)":(action=="Copy"?" (Ctrl+C)":(action=="Save"?" (Ctrl+S)":""))));row.Controls.Add(button);actionWidth+=buttonWidth+4;
            };
            add("Editor","Правка","✎",104);
            foreach(string token in settings.ButtonOrder.Split(','))
            {
                if(token=="Copy")add("Copy","Копировать","▣",136);
                else if(token=="QuickSave")add("Save","В папку","↓",112);
            }
            var cancel=new GlassButton {Glyph="×",Size=new Size(36,36),Margin=new Padding(2)};cancel.Click+=delegate{Close();};tip.SetToolTip(cancel,"Отмена (Esc)");row.Controls.Add(cancel);actionWidth+=40;
            actions.Size=new Size(actionWidth+12,52);Controls.Add(actions);
            Shown+=delegate{Bounds=desktop;Activate();Focus();};
        }
        private Point ClampPoint(Point p) {return new Point(Math.Max(0,Math.Min(ClientSize.Width,p.X)),Math.Max(0,Math.Min(ClientSize.Height,p.Y)));}
        private Point[] Handles()
        {
            var r=Selection;int mx=r.Left+r.Width/2,my=r.Top+r.Height/2;
            return new[]{new Point(r.Left,r.Top),new Point(mx,r.Top),new Point(r.Right,r.Top),new Point(r.Right,my),new Point(r.Right,r.Bottom),new Point(mx,r.Bottom),new Point(r.Left,r.Bottom),new Point(r.Left,my)};
        }
        private int Hit(Point p)
        {
            if(Selection.Width<2 || Selection.Height<2)return -2;
            var hs=Handles();for(int i=0;i<hs.Length;i++)if(new Rectangle(hs[i].X-7,hs[i].Y-7,14,14).Contains(p))return i;
            return Selection.Contains(p)?-1:-2;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button==MouseButtons.Right){Close();return;}
            if(e.Button!=MouseButtons.Left)return;
            origin=ClampPoint(e.Location);initial=Selection;hit=Hit(origin);dragging=true;Capture=true;actions.Visible=false;
            if(hit==-2)Selection=new Rectangle(origin,Size.Empty);Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);mouse=ClampPoint(e.Location);
            if(dragging)
            {
                if(hit==-2)
                {
                    Point end=mouse;
                    if((ModifierKeys&Keys.Shift)!=0)
                    {
                        int len=Math.Min(Math.Abs(end.X-origin.X),Math.Abs(end.Y-origin.Y));
                        end=new Point(origin.X+(end.X>=origin.X?len:-len),origin.Y+(end.Y>=origin.Y?len:-len));
                    }
                    Selection=Images.FromPoints(origin,end);
                }
                else if(hit==-1)
                {
                    int x=Math.Max(0,Math.Min(ClientSize.Width-initial.Width,initial.X+mouse.X-origin.X));
                    int y=Math.Max(0,Math.Min(ClientSize.Height-initial.Height,initial.Y+mouse.Y-origin.Y));
                    Selection=new Rectangle(x,y,initial.Width,initial.Height);
                }
                else
                {
                    int l=initial.Left,t=initial.Top,r=initial.Right,b=initial.Bottom;
                    if(hit==0 || hit==6 || hit==7)l=mouse.X;
                    if(hit==2 || hit==3 || hit==4)r=mouse.X;
                    if(hit==0 || hit==1 || hit==2)t=mouse.Y;
                    if(hit==4 || hit==5 || hit==6)b=mouse.Y;
                    Selection=Images.FromPoints(new Point(l,t),new Point(r,b));
                }
            }
            else
            {
                int h=Hit(mouse); Cursor=h==-1?Cursors.SizeAll:(h==1||h==5?Cursors.SizeNS:(h==3||h==7?Cursors.SizeWE:(h==0||h==4?Cursors.SizeNWSE:(h==2||h==6?Cursors.SizeNESW:Cursors.Cross))));
            }
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(!dragging)return;dragging=false;Capture=false;PositionActions();Invalidate();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e) {base.OnMouseDoubleClick(e);if(Selection.Contains(e.Location))Complete();}
        private void PositionActions()
        {
            if(Selection.Width<2||Selection.Height<2){actions.Visible=false;return;}
            var monitor=Screen.FromPoint(new Point(Selection.Right-1+desktop.X,Selection.Bottom-1+desktop.Y)).Bounds;
            monitor.Offset(-desktop.X,-desktop.Y);
            int targetX=appearance.ActionPosition=="Left"?Selection.Left:Selection.Right-actions.Width;
            int x=Math.Max(monitor.Left+8,Math.Min(monitor.Right-actions.Width-8,targetX));
            bool above=appearance.SelectionToolbarPosition=="Above";
            int y=above?Selection.Top-actions.Height-12:Selection.Bottom+12;
            if(y<monitor.Top+8)y=Selection.Bottom+12;
            if(y+actions.Height>monitor.Bottom-8)y=Selection.Top-actions.Height-12;
            y=Math.Max(monitor.Top+8,Math.Min(monitor.Bottom-actions.Height-8,y));
            actions.Location=new Point(x,y);actions.Visible=true;
        }
        private void Complete() {if(Selection.Width<2||Selection.Height<2)return;DialogResult=DialogResult.OK;Close();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            Keys key=keyData&Keys.KeyCode;
            if(key==Keys.Escape){Close();return true;}
            if(keyData==(Keys.Control|Keys.C)){RequestedAction="Copy";Complete();return true;}
            if(keyData==(Keys.Control|Keys.S)){RequestedAction="Save";Complete();return true;}
            if(key==Keys.Enter){Complete();return true;}
            if(keyData==(Keys.Control|Keys.A)){Selection=ClientRectangle;PositionActions();Invalidate();return true;}
            if(Selection.Width>0 && (key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down))
            {
                int n=(keyData&Keys.Shift)!=0?10:1;int dx=key==Keys.Left?-n:(key==Keys.Right?n:0),dy=key==Keys.Up?-n:(key==Keys.Down?n:0);
                var r=Selection;
                if((keyData&Keys.Control)!=0){r.Width=Math.Max(2,Math.Min(ClientSize.Width-r.X,r.Width+dx));r.Height=Math.Max(2,Math.Min(ClientSize.Height-r.Y,r.Height+dy));}
                else {r.X=Math.Max(0,Math.Min(ClientSize.Width-r.Width,r.X+dx));r.Y=Math.Max(0,Math.Min(ClientSize.Height-r.Height,r.Y+dy));}
                Selection=r;PositionActions();Invalidate();return true;
            }
            return base.ProcessCmdKey(ref msg,keyData);
        }
        internal void PaintBackdrop(Graphics g)
        {
            g.DrawImageUnscaled(screen,0,0);
            using(var shade=new SolidBrush(Color.FromArgb(126,Glass.Background)))
            {
                if(Selection.Width>0&&Selection.Height>0)
                {var r=Selection;g.FillRectangle(shade,0,0,Width,r.Top);g.FillRectangle(shade,0,r.Bottom,Width,Height-r.Bottom);g.FillRectangle(shade,0,r.Top,r.Left,r.Height);g.FillRectangle(shade,r.Right,r.Top,Width-r.Right,r.Height);}
                else g.FillRectangle(shade,ClientRectangle);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;PaintBackdrop(g);
            if(Selection.Width>0&&Selection.Height>0)
            {
                var r=Selection;g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var p=new Pen(Glass.Accent,1.5f))g.DrawRectangle(p,r.X,r.Y,r.Width-1,r.Height-1);
                foreach(var h in Handles())
                {
                    using(var b=new SolidBrush(Glass.Accent))g.FillEllipse(b,h.X-4,h.Y-4,8,8);
                    using(var p=new Pen(Color.White,1.5f))g.DrawEllipse(p,h.X-4,h.Y-4,8,8);
                }
                string size=r.Width+" × "+r.Height+" px";var sz=g.MeasureString(size,labelFont);
                int y=r.Top>38?r.Top-36:r.Top+9;int x=Math.Min(Math.Max(8,r.Left),Math.Max(8,ClientSize.Width-(int)sz.Width-28));
                Glass.PaintCard(g,new Rectangle(x,y,(int)Math.Ceiling(sz.Width)+22,28),Glass.Surface,9);
                using(var br=new SolidBrush(Glass.Text))g.DrawString(size,labelFont,br,x+11,y+5);
            }
            else
            {
                var monitor=Screen.FromPoint(System.Windows.Forms.Cursor.Position).Bounds;monitor.Offset(-desktop.X,-desktop.Y);
                var box=new Rectangle(monitor.Left+Math.Max(12,(monitor.Width-572)/2),monitor.Top+48,Math.Min(572,monitor.Width-24),84);
                Glass.PaintCard(g,box,Glass.Surface,18);
                using(var br=new SolidBrush(Glass.Text))g.DrawString("Выделите область мышью",titleFont,br,box.Left+22,box.Top+15);
                using(var br=new SolidBrush(Glass.Muted))g.DrawString("Shift — квадрат  ·  Ctrl+A — весь экран  ·  Esc — отмена",labelFont,br,box.Left+22,box.Top+49);
            }
            if(magnifier && (!actions.Visible||dragging))PaintMagnifier(g);
            base.OnPaint(e);
        }
        private void PaintMagnifier(Graphics g)
        {
            var monitor=Screen.FromPoint(new Point(mouse.X+desktop.X,mouse.Y+desktop.Y)).Bounds;monitor.Offset(-desktop.X,-desktop.Y);
            int x=mouse.X+28,y=mouse.Y+28;const int side=119;
            if(x+side>monitor.Right-8)x=mouse.X-side-28;if(y+side+24>monitor.Bottom-8)y=mouse.Y-side-48;
            x=Math.Max(monitor.Left+4,x);y=Math.Max(monitor.Top+4,y);
            Glass.PaintCard(g,new Rectangle(x-6,y-6,side+12,side+38),Glass.Surface,12);
            var old=g.InterpolationMode;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(screen,new Rectangle(x,y,side,side),new Rectangle(mouse.X-8,mouse.Y-8,17,17),GraphicsUnit.Pixel);
            g.InterpolationMode=old;g.PixelOffsetMode=PixelOffsetMode.Default;
            using(var p=new Pen(Glass.Accent,1)){g.DrawLine(p,x+side/2,y,x+side/2,y+side);g.DrawLine(p,x,y+side/2,x+side,y+side/2);}
            using(var br=new SolidBrush(Glass.Text))g.DrawString("X "+(mouse.X+desktop.X)+"  Y "+(mouse.Y+desktop.Y),labelFont,br,x+3,y+side+5);
        }
        protected override void Dispose(bool disposing){if(disposing){labelFont.Dispose();titleFont.Dispose();tip.Dispose();}base.Dispose(disposing);}
    }
}
