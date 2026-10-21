using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SnapLite
{
    public sealed partial class SettingsForm : GlassForm
    {
        private readonly Settings original,draft;
        private readonly Func<Settings,string> apply;
        private readonly TextBox hotkeyBox,folder;
        private readonly NumericUpDown delay,scale,quality,opacity,radius;
        private readonly CheckBox magnifier,cursor,notifications,closeAfter,autoStart,labels;
        private readonly ComboBox format,action,presets,dock,align,selectionDock,language;
        private readonly ListBox actionsOrder,toolsOrder;
        private readonly Panel pagesHost;
        private readonly List<FlowLayoutPanel> pages=new List<FlowLayoutPanel>();
        private readonly List<GlassButton> navigation=new List<GlassButton>();
        private bool committed,updating;
        public bool EditingHotkey {get{return hotkeyBox.Focused;}}
        internal void RestorePreview(){Loc.Configure(draft.Language);Glass.Configure(draft);Loc.Apply(this);Glass.Apply(this);}

        public SettingsForm(Settings current,Func<Settings,string> apply)
        {
            original=current.Clone();draft=current.Clone();this.apply=apply;
            Loc.Configure(current.Language);
            Text=Loc.T("SnapLite · Настройки");Icon=Theme.Icon();Font=new Font("Segoe UI",9);
            AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
            ClientSize=new Size(580,492);FormBorderStyle=FormBorderStyle.FixedSingle;
            MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18,10,18,14),ColumnCount=1,RowCount=3,BackColor=Color.Transparent};
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,54));Controls.Add(root);
            var header=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};root.Controls.Add(header,0,0);
            header.Controls.Add(new Label{Text=Loc.T("Настройки"),Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true,Location=new Point(0,1),BackColor=Color.Transparent});
            header.Controls.Add(new Label{Text=Loc.T("SnapLite  /  пространство для твоих снимков"),Tag="muted",AutoSize=true,Location=new Point(1,37),BackColor=Color.Transparent});
            var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty,BackColor=Color.Transparent};
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,118));body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,10));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(body,0,1);
            var sidebar=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.Transparent,Padding=new Padding(0,4,0,0),Margin=Padding.Empty};body.Controls.Add(sidebar,0,0);
            pagesHost=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent,Margin=Padding.Empty};body.Controls.Add(pagesHost,2,0);
            string[] names={Loc.T("Захват"),Loc.T("Сохранение"),Loc.T("Оформление"),Loc.T("Кнопки"),Loc.T("Общие")};string[] glyphs={"⌖","▧","◈","☷","⚙"};
            for(int i=0;i<names.Length;i++)
            {
                int index=i;
                var page=new CompactScrollPage{Dock=DockStyle.Fill,Visible=i==0,Margin=Padding.Empty};
                page.SizeChanged+=delegate(object sender,EventArgs e){var p=(FlowLayoutPanel)sender;foreach(Control c in p.Controls)c.Width=Math.Max(310,p.ClientSize.Width-p.Padding.Horizontal-3);};
                pages.Add(page);pagesHost.Controls.Add(page);
                var tab=new GlassButton{Text=names[i],Glyph=glyphs[i],Width=118,Height=39,Font=new Font("Segoe UI",8.5f),Padding=new Padding(6,0,6,0),Selected=i==0,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0,0,0,5)};
                tab.Click+=delegate{SelectPage(index);};navigation.Add(tab);sidebar.Controls.Add(tab);
            }
            var keyCard=Card(pages[0],100);Caption(keyCard,Loc.T("Горячая клавиша"),16,13);
            var keyField=new GlassField{Text=Hotkeys.Label(draft.Hotkey,draft.Modifiers),Location=new Point(16,38),Size=new Size(222,34)};keyCard.Controls.Add(keyField);
            hotkeyBox=keyField.Input;hotkeyBox.ReadOnly=true;hotkeyBox.ShortcutsEnabled=false;hotkeyBox.KeyDown+=HotkeyDown;
            var reset=new GlassButton{Text="Print Screen",Location=new Point(246,38),Size=new Size(108,34)};reset.Click+=delegate{draft.Hotkey=(int)Keys.PrintScreen;draft.Modifiers=0;hotkeyBox.Text=Hotkeys.Label(draft.Hotkey,0);};keyCard.Controls.Add(reset);
            Note(keyCard,Loc.T("Нажмите сочетание в поле. Для букв — Ctrl или Alt."),16,82,338);
            var captureCard=Card(pages[0],82);Caption(captureCard,Loc.T("После выделения"),16,13);Caption(captureCard,Loc.T("Задержка, с"),254,13);
            action=Combo(new[]{Loc.T("Открыть редактор"),Loc.T("Скопировать"),Loc.T("Сохранить в папку")},draft.CaptureAction=="Copy"?1:(draft.CaptureAction=="Save"?2:0));Put(captureCard,action,16,39,224,32);
            delay=Number(0,10,draft.DelaySeconds);Put(captureCard,delay,254,40,99,30);
            magnifier=Toggle(pages[0],Loc.T("Лупа у курсора"),Loc.T("Помогает точно выбрать границы"),draft.Magnifier);
            cursor=Toggle(pages[0],Loc.T("Курсор на снимке"),Loc.T("Сохранять указатель вместе с областью"),draft.IncludeCursor);
            var folderCard=Card(pages[1],91);Caption(folderCard,Loc.T("Папка для снимков"),16,13);
            var folderField=new GlassField{Text=draft.SaveFolder,Location=new Point(16,39),Size=new Size(291,34)};folder=folderField.Input;folderCard.Controls.Add(folderField);
            var browse=new GlassButton{Text="…",Location=new Point(315,39),Size=new Size(38,34)};browse.Click+=Browse;folderCard.Controls.Add(browse);
            var exportCard=Card(pages[1],116);Caption(exportCard,Loc.T("Формат"),16,13);Caption(exportCard,Loc.T("Качество JPG"),117,13);Caption(exportCard,Loc.T("Масштаб, %"),249,13);
            format=Combo(new[]{"PNG","JPG"},draft.Format=="JPG"?1:0);Put(exportCard,format,16,39,87,32);
            quality=Number(1,100,draft.JpegQuality);Put(exportCard,quality,117,40,116,30);quality.Enabled=format.SelectedIndex==1;format.SelectedIndexChanged+=delegate{quality.Enabled=format.SelectedIndex==1;};
            scale=Number(10,400,draft.OutputScale);scale.Increment=10;Put(exportCard,scale,249,40,104,30);
            Note(exportCard,Loc.T("100% — исходные пиксели; 50% — вдвое меньше.\nПросмотр в редакторе на размер файла не влияет."),16,80,338);
            closeAfter=Toggle(pages[1],Loc.T("Закрывать редактор"),Loc.T("После копирования или сохранения"),draft.CloseAfterAction);
            AddPreview(pages[2]);var presetCard=Card(pages[2],83);Caption(presetCard,Loc.T("Готовая тема"),16,12);
            presets=Combo(new[]{Loc.T("Midnight · фиолетовое стекло"),Loc.T("Aurora · северное сияние"),Loc.T("Pearl · светлый жемчуг"),Loc.T("Rose · розовый кварц"),Loc.T("Graphite · графит"),Loc.T("Своя палитра")},PresetIndex(draft.ThemePreset));Put(presetCard,presets,16,37,337,32);
            presets.SelectedIndexChanged+=delegate{if(updating||Loc.Applying||presets.SelectedIndex<0||presets.SelectedIndex==5)return;SetPreset(presets.SelectedIndex);RefreshAppearance();};
            var colors=Card(pages[2],188);Caption(colors,Loc.T("Твоя палитра"),16,12);
            AddColor(colors,Loc.T("Акцент"),0,delegate{return draft.AccentColor;},delegate(int c){draft.AccentColor=c;});
            AddColor(colors,Loc.T("Второй акцент"),1,delegate{return draft.Accent2Color;},delegate(int c){draft.Accent2Color=c;});
            AddColor(colors,Loc.T("Фон"),2,delegate{return draft.BackgroundColor;},delegate(int c){draft.BackgroundColor=c;});
            AddColor(colors,Loc.T("Панели"),3,delegate{return draft.PanelColor;},delegate(int c){draft.PanelColor=c;});
            AddColor(colors,Loc.T("Текст"),4,delegate{return draft.TextColor;},delegate(int c){draft.TextColor=c;});
            AddColor(colors,Loc.T("Меню"),5,delegate{return draft.MenuColor;},delegate(int c){draft.MenuColor=c;});
            AddColor(colors,Loc.T("Кнопки"),6,delegate{return draft.ButtonColor;},delegate(int c){draft.ButtonColor=c;});
            AddColor(colors,Loc.T("Границы"),7,delegate{return draft.BorderColor;},delegate(int c){draft.BorderColor=c;});
            var glassCard=Card(pages[2],87);Caption(glassCard,Loc.T("Плотность стекла, %"),16,13);Caption(glassCard,Loc.T("Скругление, px"),211,13);
            opacity=Number(30,100,draft.GlassOpacity);Put(glassCard,opacity,16,40,170,30);radius=Number(0,28,draft.CornerRadius);Put(glassCard,radius,211,40,142,30);
            opacity.ValueChanged+=delegate{if(updating)return;draft.GlassOpacity=(int)opacity.Value;RefreshAppearance();};radius.ValueChanged+=delegate{if(updating)return;draft.CornerRadius=(int)radius.Value;RefreshAppearance();};
            var resetLook=new GlassButton{Text=Loc.T("Вернуть стандартное оформление"),Height=34,Margin=new Padding(0,3,0,9)};resetLook.Click+=delegate{updating=true;SetPreset(0);draft.GlassOpacity=82;draft.CornerRadius=16;opacity.Value=82;radius.Value=16;presets.SelectedIndex=0;updating=false;RefreshAppearance();};pages[2].Controls.Add(resetLook);
            AddPreview(pages[3]);var placement=Card(pages[3],151);Caption(placement,Loc.T("Панель инструментов"),16,12);
            dock=Combo(new[]{Loc.T("Сверху"),Loc.T("Снизу"),Loc.T("Слева"),Loc.T("Справа")},Array.IndexOf(new[]{"Top","Bottom","Left","Right"},draft.ToolbarPosition));Put(placement,dock,16,36,337,32);
            Caption(placement,Loc.T("Действия в редакторе"),16,83);Caption(placement,Loc.T("Панель выделения"),211,83);
            align=Combo(new[]{Loc.T("Справа"),Loc.T("Слева")},draft.ActionPosition=="Left"?1:0);Put(placement,align,16,106,180,32);
            selectionDock=Combo(new[]{Loc.T("Под областью"),Loc.T("Над областью")},draft.SelectionToolbarPosition=="Above"?1:0);Put(placement,selectionDock,211,106,142,32);
            dock.SelectedIndexChanged+=delegate{if(Loc.Applying||dock.SelectedIndex<0)return;draft.ToolbarPosition=new[]{"Top","Bottom","Left","Right"}[dock.SelectedIndex];RefreshPreview();};
            align.SelectedIndexChanged+=delegate{if(Loc.Applying||align.SelectedIndex<0)return;draft.ActionPosition=align.SelectedIndex==1?"Left":"Right";RefreshPreview();};selectionDock.SelectedIndexChanged+=delegate{if(Loc.Applying||selectionDock.SelectedIndex<0)return;draft.SelectionToolbarPosition=selectionDock.SelectedIndex==1?"Above":"Below";RefreshPreview();};
            actionsOrder=OrderCard(pages[3],Loc.T("Порядок кнопок действий"),draft.ButtonOrder,new[]{"Copy","Save","QuickSave"},new[]{Loc.T("Копировать"),Loc.T("Сохранить как…"),Loc.T("В папку")},false);
            toolsOrder=OrderCard(pages[3],Loc.T("Порядок инструментов"),draft.ToolOrder,new[]{"Pen","Arrow","Rectangle","Marker","Text","Cover"},new[]{Loc.T("Перо"),Loc.T("Стрелка"),Loc.T("Рамка"),Loc.T("Маркер"),Loc.T("Текст"),Loc.T("Заливка")},true);
            labels=Toggle(pages[3],Loc.T("Подписи инструментов"),Loc.T("Выключите для панели только со значками"),draft.ShowToolLabels);
            var resetLayout=new GlassButton{Text=Loc.T("Вернуть стандартное расположение"),Height=34,Margin=new Padding(0,3,0,9)};resetLayout.Click+=delegate{dock.SelectedIndex=0;align.SelectedIndex=0;selectionDock.SelectedIndex=0;labels.Checked=true;ResetOrder(actionsOrder,new[]{"Copy","Save","QuickSave"},new[]{Loc.T("Копировать"),Loc.T("Сохранить как…"),Loc.T("В папку")});ResetOrder(toolsOrder,new[]{"Pen","Arrow","Rectangle","Marker","Text","Cover"},new[]{Loc.T("Перо"),Loc.T("Стрелка"),Loc.T("Рамка"),Loc.T("Маркер"),Loc.T("Текст"),Loc.T("Заливка")});RefreshPreview();};pages[3].Controls.Add(resetLayout);
            var languageCard=Card(pages[4],104);Caption(languageCard,Loc.T("Язык интерфейса"),16,12);
            language=Combo(new[]{"Русский","English"},draft.Language=="en"?1:0);Put(languageCard,language,16,37,337,32);
            Note(languageCard,Loc.T("Выбор сохраняется после перезапуска."),16,78,337);
            language.SelectedIndexChanged+=delegate{if(updating||Loc.Applying||language.SelectedIndex<0)return;draft.Language=language.SelectedIndex==1?"en":"ru";Loc.Configure(draft.Language);Loc.Apply(this);RefreshPreview();};
            notifications=Toggle(pages[4],Loc.T("Уведомления"),Loc.T("О сохранении и копировании снимков"),draft.Notifications);autoStart=Toggle(pages[4],Loc.T("Запуск вместе с Windows"),Loc.T("Сразу в трей, без лишних окон"),draft.AutoStart);
            var help=Card(pages[4],165);Caption(help,Loc.T("Всегда под рукой"),16,14);Note(help,Loc.T("Левая кнопка по значку — выбрать область.\nПравая — открыть меню и настройки.\n\nЕсли значок скрыт, откройте ↑ рядом с часами.\nСнимки остаются на вашем компьютере."),16,44,335);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,BackColor=Color.Transparent,WrapContents=false,Padding=new Padding(0,12,0,0),Margin=Padding.Empty};root.Controls.Add(footer,0,2);
            var save=new GlassButton{Text=Loc.T("Сохранить"),Primary=true,Width=126,Height=35,Margin=new Padding(6,0,0,0)};save.Click+=Save;var cancel=new GlassButton{Text=Loc.T("Отмена"),Width=90,Height=35,Margin=Padding.Empty};cancel.Click+=delegate{Close();};footer.Controls.Add(save);footer.Controls.Add(cancel);CancelButton=cancel;
            Shown+=delegate{Glass.Apply(this);Loc.Apply(this);SelectPage(0);};
            FormClosed+=delegate{if(!committed){Loc.Configure(original.Language);Glass.Configure(original);foreach(Form f in Application.OpenForms){if(f is SelectionForm)continue;var editor=f as EditorForm;if(editor!=null)editor.ApplyAppearance(original);else{Loc.Apply(f);Glass.Apply(f);}f.Invalidate(true);}}};
        }
        private void HotkeyDown(object sender,KeyEventArgs e)
        {
            e.SuppressKeyPress=true;e.Handled=true;uint mods=(uint)((e.Control?2:0)|(e.Alt?1:0)|(e.Shift?4:0));if(!Hotkeys.IsValid(e.KeyCode,mods))return;draft.Hotkey=(int)e.KeyCode;draft.Modifiers=mods;hotkeyBox.Text=Hotkeys.Label(draft.Hotkey,mods);
        }
        private void Browse(object sender,EventArgs e){using(var d=new FolderBrowserDialog{Description=Loc.T("Папка для скриншотов"),SelectedPath=folder.Text})if(d.ShowDialog(this)==DialogResult.OK)folder.Text=d.SelectedPath;}
        private void Save(object sender,EventArgs e)
        {
            try
            {
                string path=Environment.ExpandEnvironmentVariables(folder.Text.Trim());if(!System.IO.Path.IsPathRooted(path))throw new ArgumentException(Loc.T("Укажите полный путь к папке."));draft.SaveFolder=System.IO.Path.GetFullPath(path);
                draft.DelaySeconds=(int)delay.Value;draft.Magnifier=magnifier.Checked;draft.IncludeCursor=cursor.Checked;draft.CaptureAction=action.SelectedIndex==1?"Copy":(action.SelectedIndex==2?"Save":"Editor");draft.Format=format.SelectedIndex==1?"JPG":"PNG";draft.JpegQuality=(int)quality.Value;draft.OutputScale=(int)scale.Value;
                draft.Notifications=notifications.Checked;draft.CloseAfterAction=closeAfter.Checked;draft.AutoStart=autoStart.Checked;draft.ShowToolLabels=labels.Checked;draft.ButtonOrder=Order(actionsOrder);draft.ToolOrder=Order(toolsOrder);draft.Normalize();
                string error=apply(draft);if(error!=null){MessageBox.Show(this,error,Loc.T("Настройки не сохранены"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}committed=true;DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,Loc.T("Проверьте настройки"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}
