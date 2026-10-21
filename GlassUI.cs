using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapLite
{
    // All glass is painted only when Windows invalidates a control. There are no
    // animation loops, background bitmaps, composition hooks or render timers.
    public static class Glass
    {
        public static Color Background = Color.FromArgb(15, 18, 31);
        public static Color Surface = Color.FromArgb(31, 36, 56);
        public static Color Text = Color.FromArgb(241, 244, 255);
        public static Color Muted = Color.FromArgb(154, 165, 192);
        public static Color Accent = Color.FromArgb(155, 130, 255);
        public static Color Accent2 = Color.FromArgb(89, 198, 242);
        public static Color Secondary = Color.FromArgb(49, 57, 81);
        public static Color Border = Color.FromArgb(150, 169, 210);
        public static Color Menu = Color.FromArgb(28, 34, 52);
        public static int Radius = 16;
        public static int GlassOpacity = 78;
        public static bool IsDark = true;
        public static string ThemePreset = "Midnight";
        public static readonly Font UiFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font StrongFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font SymbolFont = new Font("Segoe UI Symbol", 11f, FontStyle.Regular);
        public const TextFormatFlags TextFlags = TextFormatFlags.PreserveGraphicsTranslateTransform |
            TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static Color Mix(Color a, Color b, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * amount), (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount), (int)(a.B + (b.B - a.B) * amount));
        }

        private static Color Opaque(int argb)
        {
            Color c = Color.FromArgb(argb);
            return Color.FromArgb(c.R, c.G, c.B);
        }

        public static void Configure(Settings s)
        {
            if (s == null) return;
            ThemePreset = s.ThemePreset ?? "Custom";
            Background = Opaque(s.BackgroundColor);
            Surface = Opaque(s.PanelColor);
            Text = Opaque(s.TextColor);
            Accent = Opaque(s.AccentColor);
            Accent2 = Opaque(s.Accent2Color);
            Secondary = Opaque(s.ButtonColor);
            Border = Opaque(s.BorderColor);
            Menu = Opaque(s.MenuColor);
            GlassOpacity = Math.Max(0, Math.Min(100, s.GlassOpacity));
            Radius = Math.Max(0, Math.Min(28, s.CornerRadius));
            IsDark = Background.R * .2126 + Background.G * .7152 + Background.B * .0722 < 140;
            Muted = Mix(Text, Background, IsDark ? .40f : .36f);
        }

        public static GraphicsPath Round(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0) return path;
            radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f));
            if (radius < .5f) { path.AddRectangle(rect); path.CloseFigure(); return path; }
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void Bloom(Graphics g, RectangleF bounds, Color color, int alpha)
        {
            if (bounds.Width < 1 || bounds.Height < 1) return;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(alpha, color);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, color) };
                    brush.CenterPoint = new PointF(bounds.X + bounds.Width * .5f, bounds.Y + bounds.Height * .5f);
                    g.FillEllipse(brush, bounds);
                }
            }
        }

        public static void PaintWindow(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush brush = new LinearGradientBrush(rect,
                Mix(Background, Accent, IsDark ? .055f : .035f), Background, 75f))
                g.FillRectangle(brush, rect);
            Bloom(g, new RectangleF(rect.X - rect.Width * .16f, rect.Y - rect.Height * .55f,
                rect.Width * 1.05f, rect.Height * 1.05f), Accent, IsDark ? 46 : 24);
            Bloom(g, new RectangleF(rect.Right - rect.Width * .62f, rect.Y + rect.Height * .10f,
                rect.Width * .95f, rect.Height * .95f), Accent2, IsDark ? 22 : 19);
            using (Pen pen = new Pen(Color.FromArgb(IsDark ? 20 : 50, Color.White)))
                g.DrawLine(pen, rect.X + 18, rect.Y, rect.Right - 18, rect.Y);
        }

        // Reconstruct only the material under a child. Invoking a parent's full
        // Paint event can also redraw its headings into the child and GDI text
        // does not preserve a translated Graphics context unless asked to do so.
        // Drawing surfaces directly also avoids native CheckBox background
        // painting, which can produce black rectangles with transparent colors.
        public static void PaintParent(Control child, Graphics g)
        {
            GraphicsState initial = g.Save();
            try
            {
                g.SetClip(child.ClientRectangle, CombineMode.Intersect);
                using (SolidBrush fallback = new SolidBrush(Background)) g.FillRectangle(fallback, child.ClientRectangle);
                List<Control> parents = new List<Control>();
                List<Point> offsets = new List<Point>();
                int x = -child.Left, y = -child.Top;
                Control parent = child.Parent;
                while (parent != null)
                {
                    parents.Add(parent); offsets.Add(new Point(x, y));
                    x -= parent.Left; y -= parent.Top; parent = parent.Parent;
                }
                for (int i = parents.Count - 1; i >= 0; i--)
                {
                    Control surface = parents[i];
                    GraphicsState state = g.Save();
                    g.TranslateTransform(offsets[i].X, offsets[i].Y);
                    if (surface is GlassForm) PaintWindow(g, surface.ClientRectangle);
                    else if (surface is SelectionForm) ((SelectionForm)surface).PaintBackdrop(g);
                    else if (surface is GlassPanel)
                        PaintCard(g, surface.ClientRectangle, Surface, ((GlassPanel)surface).Radius);
                    else if (surface.BackColor.A > 0)
                        using (SolidBrush brush = new SolidBrush(surface.BackColor)) g.FillRectangle(brush, surface.ClientRectangle);
                    g.Restore(state);
                }
            }
            finally { g.Restore(initial); }
        }

        public static void PaintCard(Graphics g, Rectangle bounds, Color color, int radius)
        {
            if (bounds.Width < 2 || bounds.Height < 2) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(bounds.X + .5f, bounds.Y + .5f, bounds.Width - 1f, bounds.Height - 1f);
            int alpha = Math.Max(0, Math.Min(255, color.A * GlassOpacity / 100));
            Color top = Color.FromArgb(alpha, Mix(color, Color.White, IsDark ? .065f : .28f));
            Color bottom = Color.FromArgb(alpha, Mix(color, Background, .18f));
            using (GraphicsPath path = Round(rect, radius))
            using (LinearGradientBrush fill = new LinearGradientBrush(bounds, top, bottom, 94f))
            {
                g.FillPath(fill, path);
                using (LinearGradientBrush edge = new LinearGradientBrush(bounds,
                    Color.FromArgb(IsDark ? 105 : 175, Mix(Border, Color.White, .50f)),
                    Color.FromArgb(IsDark ? 34 : 80, Border), 83f))
                using (Pen pen = new Pen(edge, 1)) g.DrawPath(pen, path);
            }
        }

        public static void Apply(Form form)
        {
            form.Font = UiFont;
            form.BackColor = Background;
            form.ForeColor = Text;
            ApplyChildren(form);
            if (form.IsHandleCreated) ApplyNative(form);
            form.Invalidate(true);
        }

        private static void ApplyChildren(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is GlassCombo || child is GlassNumber || child is TextBox || child is ListBox)
                {
                    child.BackColor = Mix(Surface, Background, .15f);
                    child.ForeColor = Text;
                }
                else if (child is GlassPanel || child is GlassField || child is GlassButton || child is GlassToggle)
                    child.ForeColor = Text;
                else if (child is Label)
                    child.ForeColor = String.Equals(Convert.ToString(child.Tag), "muted", StringComparison.OrdinalIgnoreCase) ? Muted : Text;
                ApplyChildren(child);
                child.Invalidate();
            }
        }

        public static void Repaint(Form form) { Apply(form); }

        public static void ApplyNative(Form form)
        {
            if (!form.IsHandleCreated || Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            try
            {
                int dark = IsDark ? 1 : 0;
                int result = DwmSetWindowAttribute(form.Handle, 20, ref dark, 4);
                if (result != 0) DwmSetWindowAttribute(form.Handle, 19, ref dark, 4);
                int corners = Radius > 0 ? 2 : 1;
                DwmSetWindowAttribute(form.Handle, 33, ref corners, 4);
                // Unsupported attributes fail harmlessly on older Windows releases.
                int caption = ColorTranslator.ToWin32(Background);
                int ink = ColorTranslator.ToWin32(Text);
                DwmSetWindowAttribute(form.Handle, 35, ref caption, 4);
                DwmSetWindowAttribute(form.Handle, 36, ref ink, 4);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        public static void StyleMenu(ContextMenuStrip menu)
        {
            menu.Font = UiFont;
            menu.BackColor = Menu;
            menu.ForeColor = Text;
            menu.Padding = new Padding(6);
            menu.ShowImageMargin = false;
            menu.Renderer = new GlassMenuRenderer();
            foreach (ToolStripItem item in menu.Items)
            {
                item.Padding = item is ToolStripSeparator ? new Padding(0, 3, 0, 3) : new Padding(9, 6, 9, 6);
                item.ForeColor = Text;
            }
        }

        internal static void DrawGlyph(Graphics g, string glyph, Rectangle rect, Color color)
        {
            TextRenderer.DrawText(g, glyph, SymbolFont, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFlags);
        }

        private class GlassMenuRenderer : ToolStripProfessionalRenderer
        {
            public GlassMenuRenderer() { RoundedEdges = false; }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (SolidBrush brush = new SolidBrush(Menu)) e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (Pen pen = new Pen(Mix(Menu, Border, .36f)))
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected && !e.Item.Pressed) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
                using (GraphicsPath path = Round(rect, Math.Min(9, Radius)))
                using (SolidBrush brush = new SolidBrush(Mix(Menu, Accent, IsDark ? .24f : .16f)))
                    e.Graphics.FillPath(brush, path);
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Text : Muted;
                base.OnRenderItemText(e);
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using (Pen pen = new Pen(Mix(Menu, Border, .20f)))
                    e.Graphics.DrawLine(pen, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
            }
        }
    }

    public class GlassForm : Form
    {
        public GlassForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Glass.UiFont;
            BackColor = Glass.Background;
            ForeColor = Glass.Text;
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Glass.ApplyNative(this); }
        protected override void OnPaintBackground(PaintEventArgs e) { Glass.PaintWindow(e.Graphics, ClientRectangle); }
    }

    public class GlassPanel : Panel
    {
        private int radius = -1;
        public int Radius { get { return radius < 0 ? Glass.Radius : radius; } set { radius = value; Invalidate(); } }
        public GlassPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            BackColor = Color.Transparent;
            ForeColor = Glass.Text;
            Padding = new Padding(16);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Glass.PaintParent(this, e.Graphics);
            Glass.PaintCard(e.Graphics, ClientRectangle, Glass.Surface, Radius);
            base.OnPaint(e);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { Glass.PaintParent(this, e.Graphics); }
    }

    public class GlassButton : Button
    {
        private bool hover;
        private bool pressed;
        private bool primary;
        private bool selected;
        private string glyph = "";
        public bool Primary { get { return primary; } set { primary = value; Invalidate(); } }
        public bool Selected { get { return selected; } set { selected = value; Invalidate(); } }
        public string Glyph { get { return glyph; } set { glyph = value ?? ""; Invalidate(); } }
        public GlassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            BackColor = Color.Transparent;
            ForeColor = Glass.Text;
            Font = Glass.UiFont;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            Size = new Size(110, 36);
            Margin = new Padding(3);
            Padding = new Padding(8, 0, 8, 0);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { pressed = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Glass.PaintParent(this, e.Graphics);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 2 || Height < 2) return;
            Graphics g = e.Graphics;
            Glass.PaintParent(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(.5f, .5f, Width - 1f, Height - 1f);
            int radius = Math.Min(Glass.Radius, 12);
            Color secondary = BackColor.A > 0 ? BackColor : Glass.Secondary;
            Color fill = Primary ? Glass.Accent : Selected ? Glass.Mix(secondary, Glass.Accent, .28f) : secondary;
            if (hover && Enabled) fill = Glass.Mix(fill, Primary ? Color.White : Glass.Accent, Primary ? .10f : .14f);
            if (pressed && Enabled) fill = Glass.Mix(fill, Glass.Background, .17f);
            if (!Enabled) fill = Glass.Mix(fill, Glass.Background, .56f);
            int alpha = Primary || Selected ? 255 : Math.Min(255, 50 + Glass.GlassOpacity * 2);
            using (GraphicsPath path = Glass.Round(rect, radius))
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(alpha, Glass.Mix(fill, Color.White, Glass.IsDark ? .07f : .16f)), Color.FromArgb(alpha, fill), 90f))
            {
                g.FillPath(brush, path);
                Color edge = Primary ? Glass.Mix(Glass.Accent, Color.White, .35f)
                    : Selected ? Glass.Mix(Glass.Accent, Glass.Text, .24f) : Glass.Mix(Glass.Secondary, Glass.Border, hover ? .65f : .38f);
                using (Pen pen = new Pen(Color.FromArgb(Enabled ? 180 : 80, edge))) g.DrawPath(pen, path);
                if (Focused && ShowFocusCues)
                    using (Pen pen = new Pen(Color.FromArgb(200, Glass.Accent), 2))
                    using (GraphicsPath focus = Glass.Round(new RectangleF(2.5f, 2.5f, Width - 5, Height - 5), Math.Max(0, radius - 2)))
                        g.DrawPath(pen, focus);
            }
            Color ink = Primary ? (Glass.Accent.GetBrightness() > .7f ? Color.FromArgb(22, 25, 40) : Color.White) : ForeColor;
            if (!Enabled) ink = Glass.Muted;
            bool leftAlign = TextAlign == ContentAlignment.MiddleLeft || TextAlign == ContentAlignment.TopLeft || TextAlign == ContentAlignment.BottomLeft;
            bool rightAlign = TextAlign == ContentAlignment.MiddleRight || TextAlign == ContentAlignment.TopRight || TextAlign == ContentAlignment.BottomRight;
            int left = leftAlign ? Math.Min(8, Padding.Left) : Padding.Left;
            int right = leftAlign ? Math.Min(5, Padding.Right) : Padding.Right;
            Rectangle textRect = new Rectangle(left, pressed ? 1 : 0, Math.Max(0, Width - left - right), Height);
            if (!String.IsNullOrEmpty(Glyph))
            {
                int glyphWidth = String.IsNullOrEmpty(Text) ? Width : 20;
                Rectangle glyphRect = String.IsNullOrEmpty(Text) ? new Rectangle(0, textRect.Y, Width, Height)
                    : new Rectangle(left, textRect.Y, glyphWidth, Height);
                Glass.DrawGlyph(g, Glyph, glyphRect, ink);
                textRect.X += glyphWidth + 2;
                textRect.Width = Math.Max(0, textRect.Width - glyphWidth - 2);
            }
            TextRenderer.DrawText(g, Text, Font, textRect, ink,
                (leftAlign ? TextFormatFlags.Left : rightAlign ? TextFormatFlags.Right : TextFormatFlags.HorizontalCenter) |
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | Glass.TextFlags);
        }
    }

    public class GlassToggle : CheckBox
    {
        private bool hover;
        public GlassToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            AutoSize = false;
            BackColor = Color.Transparent;
            ForeColor = Glass.Text;
            Font = Glass.UiFont;
            Height = 32;
            Width = 260;
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { Glass.PaintParent(this, e.Graphics); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Glass.PaintParent(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF pill = new RectangleF(Math.Max(0, Width - 41), (Height - 22) / 2f, 40, 22);
            Color fill = Checked ? Glass.Accent : Glass.Mix(Glass.Surface, Glass.Text, .12f);
            if (hover && Enabled) fill = Glass.Mix(fill, Glass.Text, .08f);
            if (!Enabled) fill = Glass.Mix(fill, Glass.Background, .55f);
            using (GraphicsPath path = Glass.Round(pill, 11))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen edge = new Pen(Glass.Mix(fill, Glass.Text, .13f)))
            {
                g.FillPath(brush, path);
                g.DrawPath(edge, path);
            }
            float thumbX = Checked ? pill.Right - 19 : pill.X + 3;
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(30, Color.Black)))
                g.FillEllipse(shadow, thumbX, pill.Y + 4, 16, 16);
            using (SolidBrush thumb = new SolidBrush(Enabled ? Color.White : Glass.Muted))
                g.FillEllipse(thumb, thumbX, pill.Y + 3, 16, 16);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Math.Max(0, Width - 55), Height),
                Enabled ? ForeColor : Glass.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | Glass.TextFlags);
            if (Focused && ShowFocusCues)
                using (Pen pen = new Pen(Glass.Accent))
                using (GraphicsPath focus = Glass.Round(new RectangleF(pill.X - 2, pill.Y - 2, pill.Width + 4, pill.Height + 4), 13))
                    g.DrawPath(pen, focus);
        }
    }

    public class GlassCombo : ComboBox
    {
        public GlassCombo()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = Glass.Mix(Glass.Surface, Glass.Background, .15f);
            ForeColor = Glass.Text;
            Font = Glass.UiFont;
            ItemHeight = 25;
            IntegralHeight = false;
            DropDownHeight = 220;
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            bool selected = (e.State & DrawItemState.Selected) != 0;
            bool closed = (e.State & DrawItemState.ComboBoxEdit) != 0;
            Color fill = selected && !closed ? Glass.Mix(Glass.Surface, Glass.Accent, .28f) : BackColor;
            using (SolidBrush brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, e.Bounds);
            string label = e.Index >= 0 && e.Index < Items.Count ? GetItemText(Items[e.Index]) : Text;
            Rectangle bounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 14), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, label, Font, bounds, Enabled ? ForeColor : Glass.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | Glass.TextFlags);
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 15 && IsHandleCreated && Width > 24 && Height > 2)
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    Rectangle arrow = new Rectangle(Width - 24, 1, 23, Height - 2);
                    using (SolidBrush brush = new SolidBrush(BackColor)) g.FillRectangle(brush, arrow);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float x = Width - 12, y = Height / 2f;
                    using (Pen pen = new Pen(Enabled ? Glass.Muted : Glass.Mix(Glass.Muted, Glass.Background, .4f), 1.5f))
                    {
                        pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                        g.DrawLines(pen, new PointF[] { new PointF(x - 4, y - 2), new PointF(x, y + 2), new PointF(x + 4, y - 2) });
                    }
                    using (Pen pen = new Pen(Glass.Mix(Glass.Surface, Glass.Text, Focused ? .36f : .16f)))
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }

    public class GlassNumber : NumericUpDown
    {
        private SpinnerSkin spinner;
        public GlassNumber()
        {
            Font = Glass.UiFont;
            BackColor = Glass.Mix(Glass.Surface, Glass.Background, .15f);
            ForeColor = Glass.Text;
            BorderStyle = BorderStyle.FixedSingle;
            TextAlign = HorizontalAlignment.Center;
            Width = 80;
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            foreach (Control child in Controls)
            {
                child.BackColor = BackColor; child.ForeColor = ForeColor;
                if (!(child is TextBox))
                {
                    if (spinner != null) spinner.Detach();
                    spinner = new SpinnerSkin(this, child);
                }
            }
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (spinner != null) { spinner.Detach(); spinner = null; }
            base.OnHandleDestroyed(e);
        }
        private sealed class SpinnerSkin : NativeWindow
        {
            private readonly GlassNumber owner;
            private readonly Control control;
            public SpinnerSkin(GlassNumber owner, Control control)
            {
                this.owner = owner; this.control = control;
                control.HandleCreated += Created;
                control.HandleDestroyed += Destroyed;
                if (control.IsHandleCreated) AssignHandle(control.Handle);
                else { IntPtr unused = control.Handle; }
            }
            private void Created(object sender, EventArgs e)
            {
                if (Handle != IntPtr.Zero) ReleaseHandle();
                AssignHandle(control.Handle);
            }
            private void Destroyed(object sender, EventArgs e) { if (Handle != IntPtr.Zero) ReleaseHandle(); }
            public void Detach()
            {
                control.HandleCreated -= Created;
                control.HandleDestroyed -= Destroyed;
                if (Handle != IntPtr.Zero) ReleaseHandle();
            }
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == 15 && Handle != IntPtr.Zero)
                    using (Graphics g = Graphics.FromHwnd(Handle)) Paint(g);
                else if ((m.Msg == 0x317 || m.Msg == 0x318) && m.WParam != IntPtr.Zero)
                    using (Graphics g = Graphics.FromHdc(m.WParam)) Paint(g);
            }
            private void Paint(Graphics g)
            {
                Rectangle rect = control.ClientRectangle;
                if (rect.Width < 2 || rect.Height < 4) return;
                using (SolidBrush brush = new SolidBrush(owner.BackColor)) g.FillRectangle(brush, rect);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen divider = new Pen(Glass.Mix(owner.BackColor, Glass.Border, .30f)))
                {
                    g.DrawLine(divider, 0, 0, 0, rect.Height);
                    g.DrawLine(divider, 1, rect.Height / 2f, rect.Width, rect.Height / 2f);
                }
                float x = rect.Width / 2f;
                float half = rect.Height / 2f;
                float d = Math.Min(3.2f, rect.Width / 4f);
                using (Pen arrow = new Pen(owner.Enabled ? Glass.Text : Glass.Muted, 1.3f))
                {
                    arrow.StartCap = LineCap.Round; arrow.EndCap = LineCap.Round;
                    float y = half / 2f;
                    g.DrawLines(arrow, new PointF[] { new PointF(x - d, y + 1.5f), new PointF(x, y - 1.5f), new PointF(x + d, y + 1.5f) });
                    y += half;
                    g.DrawLines(arrow, new PointF[] { new PointF(x - d, y - 1.5f), new PointF(x, y + 1.5f), new PointF(x + d, y - 1.5f) });
                }
            }
        }
    }

    public class GlassField : Panel
    {
        public TextBox Input { get; private set; }
        public override string Text
        {
            get { return Input == null ? base.Text : Input.Text; }
            set { if (Input == null) base.Text = value; else Input.Text = value ?? ""; }
        }
        public GlassField()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Opaque, false);
            BackColor = Color.Transparent;
            ForeColor = Glass.Text;
            Height = 34;
            Padding = new Padding(11, 7, 11, 7);
            Input = new TextBox();
            Input.BorderStyle = BorderStyle.None;
            Input.Font = Glass.UiFont;
            Input.ForeColor = Glass.Text;
            Input.BackColor = Glass.Mix(Glass.Surface, Glass.Background, .15f);
            Input.TextChanged += delegate { OnTextChanged(EventArgs.Empty); };
            Input.GotFocus += delegate { Invalidate(); };
            Input.LostFocus += delegate { Invalidate(); };
            Controls.Add(Input);
            TabStop = false;
            LayoutInput();
        }
        private void LayoutInput()
        {
            if (Input != null) Input.SetBounds(Padding.Left, Math.Max(Padding.Top, (Height - Input.PreferredHeight) / 2),
                Math.Max(1, Width - Padding.Horizontal), Input.PreferredHeight);
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutInput(); }
        protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); LayoutInput(); }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); LayoutInput(); }
        protected override bool ScaleChildren { get { return false; } }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (Input != null) { Input.Font = Font; LayoutInput(); }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { Glass.PaintParent(this, e.Graphics); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Glass.PaintParent(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Glass.Round(new RectangleF(.5f, .5f, Width - 1, Height - 1), Math.Min(Glass.Radius, 9)))
            using (SolidBrush fill = new SolidBrush(Input.BackColor))
            using (Pen edge = new Pen(Input.Focused ? Glass.Accent : Glass.Mix(Glass.Surface, Glass.Text, .16f)))
            {
                g.FillPath(fill, path);
                g.DrawPath(edge, path);
            }
            base.OnPaint(e);
        }
    }
}
