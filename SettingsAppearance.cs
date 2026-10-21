using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed partial class SettingsForm
    {
        private void SelectPage(int index)
        {
            for(int i=0;i<pages.Count;i++){pages[i].Visible=i==index;navigation[i].Selected=i==index;navigation[i].Invalidate();}pages[index].BringToFront();pages[index].PerformLayout();
        }
        private GlassPanel Card(FlowLayoutPanel page,int height){var c=new GlassPanel{Width=376,Height=height,Margin=new Padding(0,0,0,9),Padding=Padding.Empty};page.Controls.Add(c);return c;}
        private static void Put(Control parent,Control child,int x,int y,int w,int h){child.Bounds=new Rectangle(x,y,w,h);parent.Controls.Add(child);}
        private static void Caption(Control card,string text,int x,int y){card.Controls.Add(new Label{Text=text,AutoSize=true,Font=new Font("Segoe UI",9,FontStyle.Bold),Location=new Point(x,y),BackColor=Color.Transparent});}
        private static void Note(Control card,string text,int x,int y,int w){card.Controls.Add(new Label{Text=text,Tag="muted",AutoSize=false,Size=new Size(w,100),Font=new Font("Segoe UI",8.25f),Location=new Point(x,y),BackColor=Color.Transparent});}
        private CheckBox Toggle(FlowLayoutPanel pane,string title,string hint,bool value){var c=Card(pane,60);var t=new GlassToggle{Text=title,Checked=value,Location=new Point(16,7),Size=new Size(337,28)};c.Controls.Add(t);Note(c,hint,16,37,336);return t;}
        private static NumericUpDown Number(int min,int max,int value){return new GlassNumber{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value))};}
        private static ComboBox Combo(string[] items,int index){var c=new GlassCombo();c.Items.AddRange(items);c.SelectedIndex=Math.Max(0,index);return c;}
        private void AddColor(Control parent,string title,int index,Func<int> get,Action<int> set)
        {
            var swatch=new ColorWell(title,get){Bounds=new Rectangle(16+(index%2)*177,39+(index/2)*34,165,29)};
            swatch.Click+=delegate{int? value=ColorPrompt.Ask(this,title,get());if(!value.HasValue)return;set(value.Value);draft.ThemePreset="Custom";updating=true;presets.SelectedIndex=5;updating=false;RefreshAppearance();};parent.Controls.Add(swatch);
        }
        private void AddPreview(FlowLayoutPanel pane){pane.Controls.Add(new AppearancePreview(delegate{return draft;}){Height=91,Width=376,Margin=new Padding(0,0,0,9)});}
        private void RefreshAppearance(){Glass.Configure(draft);Glass.Apply(this);Invalidate(true);}
        private void RefreshPreview(){foreach(var p in pages)foreach(Control c in p.Controls)if(c is AppearancePreview)c.Invalidate();}
        private static int PresetIndex(string name){int index=Array.IndexOf(new[]{"Midnight","Aurora","Pearl","Rose","Graphite","Custom"},name);return index<0?0:index;}
        private void SetPreset(int index)
        {
            int[] c;
            if(index==1)c=new[]{0x10282B,0x1E4145,0xE8FCF5,0x69E5C3,0x8EB9FF,0x153337,0x2C5559,0x8BC0B5};
            else if(index==2)c=new[]{0xE9EDF6,0xFAFBFF,0x202B40,0x7760DF,0x4BBECC,0xF6F8FE,0xE1E5F2,0x9CAAC8};
            else if(index==3)c=new[]{0x2A182A,0x462A44,0xFFF0F8,0xF4A2D3,0xBEA1FF,0x341F34,0x5C3854,0xC8A4C4};
            else if(index==4)c=new[]{0x1B1D22,0x30333C,0xF0F2F8,0xB9C3DE,0x91ADBD,0x24272F,0x414652,0x9AA3B7};
            else c=new[]{0x111522,0x232A3F,0xEFF3FF,0x8F7CFF,0x67D3F4,0x1C2234,0x313951,0x96A9D2};
            draft.ThemePreset=new[]{"Midnight","Aurora","Pearl","Rose","Graphite"}[Math.Max(0,Math.Min(4,index))];draft.BackgroundColor=Rgb(c[0]);draft.PanelColor=Rgb(c[1]);draft.TextColor=Rgb(c[2]);draft.AccentColor=Rgb(c[3]);draft.Accent2Color=Rgb(c[4]);draft.MenuColor=Rgb(c[5]);draft.ButtonColor=Rgb(c[6]);draft.BorderColor=Rgb(c[7]);
        }
        private static int Rgb(int rgb){return unchecked((int)0xff000000)|rgb;}
        private sealed class OrderItem
        {
            public string Id,Label;public OrderItem(string id,string label){Id=id;Label=label;}public override string ToString(){return Loc.Display(Label);}
        }
        private ListBox OrderCard(FlowLayoutPanel page,string title,string order,string[] ids,string[] labels,bool tools)
        {
            var card=Card(page,tools?216:144);Caption(card,title,16,12);
            var list=new ListBox{Bounds=new Rectangle(16,39,280,tools?156:82),BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=25,IntegralHeight=false,BackColor=Glass.Surface,ForeColor=Glass.Text};
            foreach(var token in order.Split(',')){int i=Array.IndexOf(ids,token);if(i>=0)list.Items.Add(new OrderItem(token,labels[i]));}list.SelectedIndex=0;card.Controls.Add(list);
            list.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;using(var bg=new SolidBrush(Glass.Surface))e.Graphics.FillRectangle(bg,e.Bounds);if((e.State&DrawItemState.Selected)!=0)using(var b=new SolidBrush(Color.FromArgb(65,Glass.Accent)))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,list.Items[e.Index].ToString(),list.Font,new Rectangle(e.Bounds.X+9,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height),Glass.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis|Glass.TextFlags);};
            var up=new GlassButton{Text="↑",Bounds=new Rectangle(308,39,45,33)};var down=new GlassButton{Text="↓",Bounds=new Rectangle(308,79,45,33)};card.Controls.Add(up);card.Controls.Add(down);up.Click+=delegate{MoveOrder(list,-1);};down.Click+=delegate{MoveOrder(list,1);};return list;
        }
        private void MoveOrder(ListBox list,int offset)
        {
            int index=list.SelectedIndex,target=index+offset;if(index<0||target<0||target>=list.Items.Count)return;var item=list.Items[index];list.Items.RemoveAt(index);list.Items.Insert(target,item);list.SelectedIndex=target;draft.ButtonOrder=Order(actionsOrder);draft.ToolOrder=Order(toolsOrder);RefreshPreview();
        }
        private static string Order(ListBox list){var ids=new List<string>();foreach(OrderItem item in list.Items)ids.Add(item.Id);return String.Join(",",ids.ToArray());}
        private static void ResetOrder(ListBox list,string[] ids,string[] text){list.Items.Clear();for(int i=0;i<ids.Length;i++)list.Items.Add(new OrderItem(ids[i],text[i]));list.SelectedIndex=0;}
    }
    internal sealed class ColorWell : Button
    {
        private readonly string title;private readonly Func<int> get;
        public ColorWell(string title,Func<int> get){this.title=title;this.get=get;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;Text=title;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);SetStyle(ControlStyles.Opaque,false);BackColor=Color.Transparent;FlatAppearance.MouseOverBackColor=Color.Transparent;FlatAppearance.MouseDownBackColor=Color.Transparent;}
        protected override void OnPaintBackground(PaintEventArgs e){Glass.PaintParent(this,e.Graphics);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;Glass.PaintParent(this,g);g.SmoothingMode=SmoothingMode.AntiAlias;var color=Color.FromArgb(get());using(var b=new SolidBrush(Color.FromArgb(25,Glass.Border)))using(var path=Glass.Round(new RectangleF(0,0,Width-1,Height-1),7))g.FillPath(b,path);
            using(var b=new SolidBrush(color))g.FillEllipse(b,8,7,14,14);using(var p=new Pen(Color.FromArgb(100,Glass.Border)))g.DrawEllipse(p,8,7,14,14);TextRenderer.DrawText(g,title,Font,new Rectangle(29,0,Width-33,Height),Glass.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|Glass.TextFlags);
        }
    }
    internal static class ColorPrompt
    {
        public static int? Ask(IWin32Window owner,string name,int initial)
        {
            using(var f=new GlassForm{Text=name,ClientSize=new Size(345,174),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MaximizeBox=false,MinimizeBox=false})
            {
                f.Controls.Add(new Label{Text=Loc.T("HEX-цвет  #RRGGBB"),Location=new Point(18,16),AutoSize=true,BackColor=Color.Transparent});
                var value=new GlassField{Text="#"+(initial&0xffffff).ToString("X6"),Bounds=new Rectangle(18,43,184,35)};f.Controls.Add(value);var palette=new GlassButton{Text=Loc.T("Палитра…"),Bounds=new Rectangle(212,43,114,35)};f.Controls.Add(palette);
                palette.Click+=delegate{using(var d=new ColorDialog{Color=Color.FromArgb(initial),FullOpen=true})if(d.ShowDialog(f)==DialogResult.OK)value.Text="#"+(d.Color.ToArgb()&0xffffff).ToString("X6");};
                var cancel=new GlassButton{Text=Loc.T("Отмена"),Bounds=new Rectangle(100,117,94,36),DialogResult=DialogResult.Cancel};f.Controls.Add(cancel);var ok=new GlassButton{Text=Loc.T("Применить"),Primary=true,Bounds=new Rectangle(204,117,122,36)};f.Controls.Add(ok);int result=initial;
                ok.Click+=delegate{int rgb;string text=value.Text.Trim().TrimStart('#');if(text.Length!=6||!int.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb)){MessageBox.Show(f,Loc.T("Введите цвет в формате #RRGGBB."),Loc.T("Цвет"),MessageBoxButtons.OK,MessageBoxIcon.Information);return;}result=unchecked((int)0xff000000)|rgb;f.DialogResult=DialogResult.OK;f.Close();};f.CancelButton=cancel;f.AcceptButton=ok;Glass.Apply(f);return f.ShowDialog(owner)==DialogResult.OK?(int?)result:null;
            }
        }
    }
    internal sealed class AppearancePreview : Control
    {
        private readonly Func<Settings> get;
        public AppearancePreview(Func<Settings> get){this.get=get;DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
        protected override void OnPaintBackground(PaintEventArgs e){Glass.PaintParent(this,e.Graphics);}
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);if(e.Button!=MouseButtons.Right)return;var menu=new ContextMenuStrip();menu.Items.Add(Loc.T("Выбрать область"));menu.Items.Add(Loc.T("Открыть папку снимков"));menu.Items.Add(new ToolStripSeparator());menu.Items.Add(Loc.T("Настройки…"));Glass.StyleMenu(menu);menu.Closed+=delegate{menu.Dispose();};menu.Show(this,e.Location);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var s=get();var g=e.Graphics;Glass.PaintParent(this,g);g.SmoothingMode=SmoothingMode.AntiAlias;Glass.PaintCard(g,ClientRectangle,Glass.Surface,Glass.Radius);
            using(var f=new Font("Segoe UI",9,FontStyle.Bold))using(var b=new SolidBrush(Glass.Text))g.DrawString(Loc.T("Предпросмотр"),f,b,15,12);
            using(var b=new SolidBrush(Glass.Muted))g.DrawString(s.ToolbarPosition=="Top"?Loc.T("сверху"):(s.ToolbarPosition=="Bottom"?Loc.T("снизу"):(s.ToolbarPosition=="Left"?Loc.T("слева"):Loc.T("справа"))),Font,b,Width-70,12);
            int x=15,y=40,w=Width-30;using(var b=new SolidBrush(Color.FromArgb(90,Glass.Menu)))using(var p=Glass.Round(new RectangleF(x,y,w,35),Math.Min(12,Glass.Radius)))g.FillPath(b,p);
            string[] items=s.ButtonOrder.Split(',');int start=s.ActionPosition=="Left"?x+6:Math.Max(x+6,Width-272);
            for(int i=0;i<items.Length;i++)
            {
                var box=new Rectangle(start+i*84,y+5,77,25);using(var b=new SolidBrush(i==0?Glass.Accent:Glass.Secondary))using(var p=Glass.Round(box,Math.Min(9,Glass.Radius)))g.FillPath(b,p);
                string text=items[i]=="Copy"?Loc.T("Копия"):(items[i]=="Save"?Loc.T("Сохранить"):Loc.T("В папку"));TextRenderer.DrawText(g,text,Font,box,i==0?(Glass.IsDark?Color.FromArgb(20,22,35):Color.White):Glass.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|Glass.TextFlags);
            }
        }
    }

    // Manual content offset keeps scrolling event-driven and avoids the bright native scrollbar.
    internal sealed class CompactScrollPage : FlowLayoutPanel
    {
        private int offset,maximum,dragY,dragOffset;
        private bool layingOut,dragging;
        public CompactScrollPage()
        {
            AutoScroll=false;FlowDirection=FlowDirection.TopDown;WrapContents=false;
            Padding=new Padding(0,4,14,0);BackColor=Color.Transparent;DoubleBuffered=true;
            SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);
        }
        protected override void OnPaintBackground(PaintEventArgs e){Glass.PaintParent(this,e.Graphics);}
        protected override void OnLayout(LayoutEventArgs levent)
        {
            if(layingOut)return;layingOut=true;
            try
            {
                base.OnLayout(levent);int height=Padding.Vertical;
                foreach(Control c in Controls)height+=c.Height+c.Margin.Vertical;
                maximum=Math.Max(0,height-ClientSize.Height);offset=Math.Max(0,Math.Min(maximum,offset));
                int y=Padding.Top-offset,width=Math.Max(310,ClientSize.Width-Padding.Horizontal-3);
                foreach(Control c in Controls){c.SetBounds(Padding.Left+c.Margin.Left,y+c.Margin.Top,width-c.Margin.Horizontal,c.Height);y+=c.Height+c.Margin.Vertical;}
            }
            finally{layingOut=false;}
            Invalidate(new Rectangle(Math.Max(0,Width-10),0,10,Height));
        }
        private Rectangle Thumb()
        {
            if(maximum==0)return Rectangle.Empty;int track=Math.Max(1,Height-10),height=Math.Max(26,(int)(track*(Height/(float)(Height+maximum))));
            return new Rectangle(Width-6,5+(int)((track-height)*(offset/(float)maximum)),4,height);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var r=Thumb();if(r.IsEmpty)return;using(var path=Glass.Round(r,2))using(var b=new SolidBrush(Color.FromArgb(dragging?190:90,Glass.Border)))e.Graphics.FillPath(b,path);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if(maximum==0){base.OnMouseWheel(e);return;}offset=Math.Max(0,Math.Min(maximum,offset-e.Delta/120*52));PerformLayout();var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||e.X<Width-13||maximum==0)return;
            var r=Thumb();if(!r.Contains(e.Location)){offset=Math.Max(0,Math.Min(maximum,(int)((e.Y-r.Height/2f-5)/Math.Max(1,Height-10-r.Height)*maximum)));PerformLayout();}
            dragY=e.Y;dragOffset=offset;dragging=true;Capture=true;Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!dragging)return;int travel=Math.Max(1,Height-10-Thumb().Height);offset=Math.Max(0,Math.Min(maximum,dragOffset+(e.Y-dragY)*maximum/travel));PerformLayout();
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Invalidate();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
        {
            if(maximum>0&&(keyData==Keys.PageDown||keyData==Keys.PageUp)){offset=Math.Max(0,Math.Min(maximum,offset+(keyData==Keys.PageDown?1:-1)*(Height-30)));PerformLayout();return true;}return base.ProcessCmdKey(ref msg,keyData);
        }
        protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);Watch(e.Control);}
        private void Watch(Control c)
        {
            c.Enter+=delegate{if(!Visible||layingOut)return;Point p=PointToClient(c.PointToScreen(Point.Empty));if(p.Y<0)offset=Math.Max(0,offset+p.Y-4);else if(p.Y+c.Height>Height)offset=Math.Min(maximum,offset+p.Y+c.Height-Height+4);PerformLayout();};
            c.ControlAdded+=delegate(object sender,ControlEventArgs e){Watch(e.Control);};foreach(Control child in c.Controls)Watch(child);
        }
    }
}
