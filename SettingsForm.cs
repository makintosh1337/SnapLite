using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed class SettingsForm : Form
    {
        private readonly Settings draft;
        private readonly Func<Settings,string> apply;
        private readonly TextBox hotkeyBox,folder;
        private readonly NumericUpDown delay,scale,quality;
        private readonly CheckBox magnifier,cursor,notifications,closeAfter,autoStart;
        private readonly ComboBox format,action;
        public bool EditingHotkey { get { return hotkeyBox.Focused; } }
        public SettingsForm(Settings current,Func<Settings,string> apply)
        {
            draft=current.Clone();this.apply=apply;
            Text="SnapLite — настройки";Icon=Theme.Icon();Font=new Font("Segoe UI",10);BackColor=Theme.Background;ForeColor=Theme.Ink;
            AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(660,570);MinimumSize=new Size(660,600);StartPosition=FormStartPosition.CenterScreen;
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(20)};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,80));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55));Controls.Add(layout);
            var header=new Panel {Dock=DockStyle.Fill};
            header.Controls.Add(new Label {Text="SnapLite",Font=new Font("Segoe UI",23,FontStyle.Bold),AutoSize=true,Location=new Point(0,0)});
            header.Controls.Add(new Label {Text="Быстрый снимок. Точная область. Всё под рукой.",AutoSize=true,ForeColor=Theme.Muted,Location=new Point(2,46)});layout.Controls.Add(header,0,0);
            var tabs=new TabControl {Dock=DockStyle.Fill};layout.Controls.Add(tabs,0,1);
            var capture=Page(tabs,"Захват");var saving=Page(tabs,"Сохранение");var general=Page(tabs,"Общие");
            hotkeyBox=new TextBox {ReadOnly=true,Width=200,Text=Hotkeys.Label(draft.Hotkey,draft.Modifiers),BackColor=Color.White};
            hotkeyBox.KeyDown+=HotkeyDown;hotkeyBox.ShortcutsEnabled=false;
            var keys=new TableLayoutPanel {ColumnCount=2,Dock=DockStyle.Fill,Height=36,Margin=Padding.Empty};keys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));keys.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,115));keys.Controls.Add(hotkeyBox,0,0);
            var reset=Theme.Button("Print Screen",110,false);reset.Height=28;reset.Margin=new Padding(4,2,0,0);reset.Click+=delegate{draft.Hotkey=(int)Keys.PrintScreen;draft.Modifiers=0;hotkeyBox.Text=Hotkeys.Label(draft.Hotkey,0);};keys.Controls.Add(reset,1,0);
            Row(capture,"Горячая клавиша",keys);
            Hint(capture,"Нажмите сочетание в поле. Можно выбрать Print Screen.\nДля букв и цифр используйте Ctrl или Alt.");
            delay=Number(0,10,draft.DelaySeconds);Row(capture,"Задержка, секунд",delay);
            action=Combo(new[]{"Открывать редактор","Сразу копировать","Сразу сохранять"},draft.CaptureAction=="Copy"?1:(draft.CaptureAction=="Save"?2:0));Row(capture,"После выделения",action);
            magnifier=Check("Лупа при выборе области",draft.Magnifier);Wide(capture,magnifier);
            cursor=Check("Показывать курсор на скриншоте",draft.IncludeCursor);Wide(capture,cursor);
            folder=new TextBox {Text=draft.SaveFolder,Dock=DockStyle.Fill};
            var folderRow=new TableLayoutPanel {ColumnCount=2,Dock=DockStyle.Fill,Height=38,Margin=Padding.Empty};folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,45));folderRow.Controls.Add(folder,0,0);
            var browse=Theme.Button("…",36,false);browse.Height=28;browse.Margin=Padding.Empty;browse.Click+=Browse;folderRow.Controls.Add(browse,1,0);Row(saving,"Папка",folderRow);
            format=Combo(new[]{"PNG","JPG"},draft.Format=="JPG"?1:0);Row(saving,"Формат файла",format);
            quality=Number(1,100,draft.JpegQuality);Row(saving,"Качество JPG, %",quality);quality.Enabled=format.SelectedIndex==1;format.SelectedIndexChanged+=delegate{quality.Enabled=format.SelectedIndex==1;};
            scale=Number(10,400,draft.OutputScale);scale.Increment=10;Row(saving,"Масштаб результата, %",scale);
            Hint(saving,"100% — исходные пиксели. 50% — вдвое меньше по каждой стороне.\nМасштаб также можно изменить в редакторе перед экспортом.");
            closeAfter=Check("Закрывать редактор после копирования / сохранения",draft.CloseAfterAction);Wide(saving,closeAfter);
            notifications=Check("Уведомления о сохранении и копировании",draft.Notifications);Wide(general,notifications);
            autoStart=Check("Запускать вместе с Windows",draft.AutoStart);Wide(general,autoStart);
            Hint(general,"Левая кнопка по значку в трее — выбрать область.\nПравая кнопка — меню: захват экрана, настройки, выход.\nЕсли значок скрыт, откройте стрелку ↑ рядом с часами.");
            Hint(general,"В ожидании нет таймеров захвата и опроса экрана.\nСнимки хранятся локально. Программа не использует сеть.\nНастройки: %LocalAppData%\\SnapLite\\settings.xml");
            var footer=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,10,0,0)};
            var save=Theme.Button("Сохранить",128,true);save.Click+=Save;
            var cancel=Theme.Button("Отмена",100,false);cancel.Click+=delegate{Close();};footer.Controls.Add(save);footer.Controls.Add(cancel);layout.Controls.Add(footer,0,2);CancelButton=cancel;
        }
        private static TableLayoutPanel Page(TabControl tabs,string title)
        {
            var page=new TabPage(title){BackColor=Color.White,Padding=new Padding(14)};tabs.TabPages.Add(page);
            var table=new TableLayoutPanel {Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(0,8,0,0)};
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));page.Controls.Add(table);return table;
        }
        private static void Row(TableLayoutPanel t,string label,Control c)
        {
            int n=t.RowCount++;t.RowStyles.Add(new RowStyle(SizeType.Absolute,43));
            var l=new Label {Text=label,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,0,8,0)};
            c.Anchor=AnchorStyles.Left|AnchorStyles.Right;t.Controls.Add(l,0,n);t.Controls.Add(c,1,n);
        }
        private static void Wide(TableLayoutPanel t,Control c)
        {
            int n=t.RowCount++;t.RowStyles.Add(new RowStyle(SizeType.AutoSize));c.Margin=new Padding(0,9,0,9);t.Controls.Add(c,0,n);t.SetColumnSpan(c,2);
        }
        private static void Hint(TableLayoutPanel t,string text){Wide(t,new Label {Text=text,AutoSize=true,ForeColor=Theme.Muted,Font=new Font("Segoe UI",9),MaximumSize=new Size(570,0)});}
        private static CheckBox Check(string text,bool value){return new CheckBox {Text=text,Checked=value,AutoSize=true};}
        private static NumericUpDown Number(int min,int max,int value){return new NumericUpDown {Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Width=110};}
        private static ComboBox Combo(string[] items,int index){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=260};c.Items.AddRange(items);c.SelectedIndex=index;return c;}
        private void HotkeyDown(object sender,KeyEventArgs e)
        {
            e.SuppressKeyPress=true;e.Handled=true;
            uint mods=(uint)((e.Control?2:0)|(e.Alt?1:0)|(e.Shift?4:0));
            if(!Hotkeys.IsValid(e.KeyCode,mods))return;draft.Hotkey=(int)e.KeyCode;draft.Modifiers=mods;hotkeyBox.Text=Hotkeys.Label(draft.Hotkey,mods);
        }
        private void Browse(object sender,EventArgs e)
        {
            using(var d=new FolderBrowserDialog {Description="Папка для скриншотов",SelectedPath=folder.Text})if(d.ShowDialog(this)==DialogResult.OK)folder.Text=d.SelectedPath;
        }
        private void Save(object sender,EventArgs e)
        {
            try
            {
                string path=Environment.ExpandEnvironmentVariables(folder.Text.Trim());
                if(!System.IO.Path.IsPathRooted(path))throw new ArgumentException("Укажите полный путь к папке.");
                draft.SaveFolder=System.IO.Path.GetFullPath(path);draft.DelaySeconds=(int)delay.Value;draft.Magnifier=magnifier.Checked;draft.IncludeCursor=cursor.Checked;
                draft.CaptureAction=action.SelectedIndex==1?"Copy":(action.SelectedIndex==2?"Save":"Editor");
                draft.Format=format.SelectedIndex==1?"JPG":"PNG";draft.JpegQuality=(int)quality.Value;draft.OutputScale=(int)scale.Value;
                draft.Notifications=notifications.Checked;draft.CloseAfterAction=closeAfter.Checked;draft.AutoStart=autoStart.Checked;
                string error=apply(draft);if(error!=null){MessageBox.Show(this,error,"Не удалось применить настройки",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Проверьте настройки",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}
