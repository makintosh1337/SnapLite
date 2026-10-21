using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed class AreaSizeForm : GlassForm
    {
        private readonly NumericUpDown width,height;
        private readonly ComboBox presets;
        private bool updating;
        public Size ChosenSize {get{return new Size((int)width.Value,(int)height.Value);}}
        private sealed class Preset
        {
            public Size Size;
            public Preset(int w,int h){Size=new Size(w,h);}
            public override string ToString(){return Size.IsEmpty?Loc.T("Свой размер"):Size.Width+" × "+Size.Height+" px";}
        }
        public AreaSizeForm(Size current,Size limit)
        {
            Text=Loc.T("Размер области");ClientSize=new Size(380,258);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterParent;
            Controls.Add(new Label{Text=Loc.T("Точный размер выделения"),Font=new Font("Segoe UI",13,FontStyle.Bold),AutoSize=true,Location=new Point(18,14),BackColor=Color.Transparent});
            Controls.Add(new Label{Text=Loc.T("Готовый размер"),AutoSize=true,Location=new Point(18,49),BackColor=Color.Transparent,Tag="muted"});
            presets=new GlassCombo{Bounds=new Rectangle(18,71,344,32)};presets.Items.Add(new Preset(0,0));
            foreach(var item in new[]{new Size(640,480),new Size(800,600),new Size(1024,768),new Size(1280,720),new Size(1920,1080),new Size(1080,1080),new Size(1080,1920),new Size(2560,1440),new Size(3840,2160)})
                if(item.Width<=limit.Width&&item.Height<=limit.Height)presets.Items.Add(new Preset(item.Width,item.Height));
            presets.SelectedIndex=0;Controls.Add(presets);
            Controls.Add(new Label{Text=Loc.T("Ширина, px"),AutoSize=true,Location=new Point(18,115),BackColor=Color.Transparent,Tag="muted"});
            Controls.Add(new Label{Text=Loc.T("Высота, px"),AutoSize=true,Location=new Point(216,115),BackColor=Color.Transparent,Tag="muted"});
            width=new GlassNumber{Minimum=2,Maximum=Math.Max(2,limit.Width),Value=Math.Max(2,Math.Min(limit.Width,current.Width)),Bounds=new Rectangle(18,139,146,31)};
            height=new GlassNumber{Minimum=2,Maximum=Math.Max(2,limit.Height),Value=Math.Max(2,Math.Min(limit.Height,current.Height)),Bounds=new Rectangle(216,139,146,31)};
            Controls.Add(width);Controls.Add(height);Controls.Add(new Label{Text="×",TextAlign=ContentAlignment.MiddleCenter,Bounds=new Rectangle(176,137,28,31),BackColor=Color.Transparent,Tag="muted"});
            Controls.Add(new Label{Text=Loc.T("Размер задаётся в исходных пикселях экрана."),Bounds=new Rectangle(18,181,344,22),BackColor=Color.Transparent,Tag="muted",Font=new Font("Segoe UI",8.25f)});
            var cancel=new GlassButton{Text=Loc.T("Отмена"),Bounds=new Rectangle(136,215,98,34),DialogResult=DialogResult.Cancel};Controls.Add(cancel);
            var apply=new GlassButton{Text=Loc.T("Применить"),Primary=true,Bounds=new Rectangle(244,215,118,34),DialogResult=DialogResult.OK};Controls.Add(apply);AcceptButton=apply;CancelButton=cancel;
            presets.SelectedIndexChanged+=delegate{if(updating||presets.SelectedIndex<=0)return;var preset=(Preset)presets.SelectedItem;updating=true;width.Value=preset.Size.Width;height.Value=preset.Size.Height;updating=false;};
            EventHandler custom=delegate{if(!updating)presets.SelectedIndex=0;};width.ValueChanged+=custom;height.ValueChanged+=custom;
            Shown+=delegate{Glass.Apply(this);Loc.Apply(this);width.Focus();width.Select(0,width.Text.Length);};
        }
    }
}
