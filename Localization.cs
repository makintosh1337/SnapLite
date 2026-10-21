using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace SnapLite
{

    public static class Loc
    {
        private static readonly Dictionary<string,string> english=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {" px   ·   Вывод "," px   ·   Output "},
            {" px   ·   Просмотр "," px   ·   View "},
            {"%   ·   Ctrl+Z — отмена","%   ·   Ctrl+Z — undo"},
            {"100% — исходные пиксели; 50% — вдвое меньше.\nПросмотр в редакторе на размер файла не влияет.","100% keeps original pixels; 50% halves each side.\nEditor zoom does not change the output size."},
            {"Aurora · северное сияние","Aurora · northern lights"},
            {"Graphite · графит","Graphite · charcoal"},
            {"HEX-цвет  #RRGGBB","HEX color  #RRGGBB"},
            {"Midnight · фиолетовое стекло","Midnight · violet glass"},
            {"Pearl · светлый жемчуг","Pearl · light glass"},
            {"Rose · розовый кварц","Rose · rose quartz"},
            {"Shift — квадрат  ·  Ctrl+A — весь экран  ·  Esc — отмена","Shift — square  ·  Ctrl+A — full desktop  ·  Esc — cancel"},
            {"SnapLite  /  пространство для твоих снимков","SnapLite  /  a space for your screenshots"},
            {"SnapLite · Настройки","SnapLite · Settings"},
            {"SnapLite — выделите область","SnapLite — select an area"},
            {"SnapLite — действие не выполнено","SnapLite — action failed"},
            {"SnapLite — запуск не удался","SnapLite — startup failed"},
            {"SnapLite — захват не удался","SnapLite — capture failed"},
            {"SnapLite — ошибка","SnapLite — error"},
            {"SnapLite — папка недоступна","SnapLite — folder unavailable"},
            {"SnapLite — редактор","SnapLite — editor"},
            {"Акцент","Accent"},
            {"Быстро сохранить в папку из настроек (Ctrl+Shift+S)","Quick save to your folder (Ctrl+Shift+S)"},
            {"В папку","To folder"},
            {"Введите цвет в формате #RRGGBB.","Enter a color in #RRGGBB format."},
            {"Вернуть (Ctrl+Y)","Redo (Ctrl+Y)"},
            {"Вернуть стандартное оформление","Reset appearance"},
            {"Вернуть стандартное расположение","Reset layout"},
            {"Весь экран под курсором","Capture screen under cursor"},
            {"Всегда под рукой","Always within reach"},
            {"Второй акцент","Second accent"},
            {"Выбор сохраняется после перезапуска.","Your choice is remembered after restarting."},
            {"Выбрать область","Select an area"},
            {"Выбрать область  (","Select an area  ("},
            {"Вывод","Output"},
            {"Выделите область мышью","Drag to select an area"},
            {"Выключите для панели только со значками","Turn off to show icons only"},
            {"Высота, px","Height, px"},
            {"Выход","Exit"},
            {"Горячая клавиша","Capture hotkey"},
            {"Готовая тема","Theme preset"},
            {"Готовый размер","Size preset"},
            {"Границы","Borders"},
            {"Действия в редакторе","Editor actions"},
            {"Добавить","Add"},
            {"Добавить подпись","Add a caption"},
            {"Задержка, с","Delay, sec"},
            {"Закрывать редактор","Close the editor"},
            {"Заливка","Cover"},
            {"Заливка: закройте фрагмент выбранным цветом","Cover: hide an area with a solid color"},
            {"Запуск вместе с Windows","Start with Windows"},
            {"Захват","Capture"},
            {"Захват отменён.","Capture cancelled."},
            {"Изображение слишком большое. Уменьшите масштаб или область (лимит 80 млн пикселей).","The image is too large. Reduce the scale or area (80 million pixel limit)."},
            {"Исходная горячая клавиша занята. Проверьте сочетание в настройках.","The original hotkey is in use. Check the shortcut in Settings."},
            {"Качество JPG","JPG quality"},
            {"Кнопки","Layout"},
            {"Копировать","Copy"},
            {"Копировать (Ctrl+C)","Copy (Ctrl+C)"},
            {"Копия","Copy"},
            {"Курсор на снимке","Include the cursor"},
            {"Левая кнопка по значку — выбрать область.\nПравая — открыть меню и настройки.\n\nЕсли значок скрыт, откройте ↑ рядом с часами.\nСнимки остаются на вашем компьютере.","Left-click the tray icon to select an area.\nRight-click to open the menu and settings.\n\nIf the icon is hidden, open ↑ by the clock.\nScreenshots stay on your computer."},
            {"Лупа у курсора","Pixel magnifier"},
            {"Лёгкий инструмент для локальных скриншотов","Lightweight local screenshot tool"},
            {"Маркер","Marker"},
            {"Масштаб просмотра; размер сохраняемого файла задаётся ниже","View zoom; output size is set below"},
            {"Масштаб, %","Scale, %"},
            {"Меню","Menu"},
            {"Над областью","Above the area"},
            {"Нажмите сочетание в поле. Для букв — Ctrl или Alt.","Press a shortcut here. Use Ctrl or Alt with letters."},
            {"Нажмите, чтобы задать размер (F2)","Click to enter a size (F2)"},
            {"Настройки","Settings"},
            {"Настройки не сохранены","Settings not saved"},
            {"Настройки…","Settings…"},
            {"Не удалось выбрать свободное имя файла.","Could not find an available file name."},
            {"Не удалось прочитать настройки. Используются стандартные.\n","Could not read settings. Using defaults.\n"},
            {"О сохранении и копировании снимков","Notify when a screenshot is saved or copied"},
            {"Общие","General"},
            {"Открыть папку снимков","Open screenshots folder"},
            {"Открыть редактор","Open editor"},
            {"Отмена","Cancel"},
            {"Отмена (Esc)","Cancel (Esc)"},
            {"Отменить (Ctrl+Z)","Undo (Ctrl+Z)"},
            {"Оформление","Appearance"},
            {"Палитра…","Color picker…"},
            {"Панели","Panels"},
            {"Панель выделения","Selection toolbar"},
            {"Панель инструментов","Tools panel"},
            {"Папка для скриншотов","Screenshot folder"},
            {"Папка для снимков","Screenshot folder"},
            {"Перо","Pen"},
            {"Плотность стекла, %","Glass density, %"},
            {"По окну","Fit to window"},
            {"Под областью","Below the area"},
            {"Подписи инструментов","Show button labels"},
            {"Помогает точно выбрать границы","Helps you place the edges precisely"},
            {"Порядок инструментов","Tool order"},
            {"Порядок кнопок действий","Action button order"},
            {"После выделения","After selecting an area"},
            {"После копирования или сохранения","After copying or saving a screenshot"},
            {"Правка","Edit"},
            {"Предпросмотр","Preview"},
            {"Применить","Apply"},
            {"Проверьте настройки","Check your settings"},
            {"Размер задаётся в исходных пикселях экрана.","The size uses original screen pixels."},
            {"Размер изображения при копировании и сохранении (10–400%)","Image size when copying and saving (10–400%)"},
            {"Размер области","Area size"},
            {"Рамка","Rectangle"},
            {"Редактор","Editor"},
            {"Сверху","Top"},
            {"Свой размер","Custom size"},
            {"Своя палитра","Custom palette"},
            {"Скопировать","Copy"},
            {"Скриншот скопирован в буфер обмена.","Screenshot copied to the clipboard."},
            {"Скругление, px","Corners, px"},
            {"Слева","Left"},
            {"Снизу","Bottom"},
            {"Сохранение","Saving"},
            {"Сохранено: ","Saved: "},
            {"Сохранить","Save"},
            {"Сохранить в папку","Save to folder"},
            {"Сохранить как","Save as"},
            {"Сохранить как…","Save as…"},
            {"Сохранить как… (Ctrl+S)","Save as… (Ctrl+S)"},
            {"Сохранить скриншот","Save screenshot"},
            {"Сохранять указатель вместе с областью","Capture the mouse pointer in the image"},
            {"Справа","Right"},
            {"Сразу в трей, без лишних окон","Start quietly in the system tray"},
            {"Стрелка","Arrow"},
            {"Твоя палитра","Your palette"},
            {"Текст","Text"},
            {"Текст на скриншоте","Screenshot text"},
            {"Толщина пера в пикселях","Pen width in pixels"},
            {"Точный размер выделения","Exact selection size"},
            {"Уведомления","Notifications"},
            {"Укажите полный путь к папке.","Enter an absolute folder path."},
            {"Фон","Background"},
            {"Формат","Format"},
            {"Цвет","Color"},
            {"Цвет пера, стрелок, текста и заливки","Color for pen, arrows, text and cover"},
            {"Ширина, px","Width, px"},
            {"Эта горячая клавиша уже занята другой программой. Выберите другое сочетание.","This hotkey is already in use by another app. Choose a different shortcut."},
            {"Язык интерфейса","Interface language"},
            {"сверху","top"},
            {"слева","left"},
            {"снизу","bottom"},
            {"справа","right"}
        };
        private static string language="ru";
        public static bool Applying {get;private set;}
        public static string Language {get{return language;}}
        public static string Normalize(string value){return String.Equals(value,"en",StringComparison.OrdinalIgnoreCase)?"en":"ru";}
        public static void Configure(string value){language=Normalize(value);}
        public static string T(string source){if(String.IsNullOrEmpty(source))return source??"";string value;return language=="en"&&english.TryGetValue(source,out value)?value:source;}
        public static string Display(string text){return T(Source(text));}
        private static string Source(string text)
        {
            if(String.IsNullOrEmpty(text)||english.ContainsKey(text))return text;
            string value;return reverse.TryGetValue(text,out value)?value:text;
        }
        private static readonly Dictionary<string,string> reverse=Reverse();
        private static Dictionary<string,string> Reverse(){var result=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var item in english)if(!result.ContainsKey(item.Value))result.Add(item.Value,item.Key);return result;}
        private sealed class TipBinding{public ToolTip Owner;public string Source;}
        private static readonly ConditionalWeakTable<Control,TipBinding> tips=new ConditionalWeakTable<Control,TipBinding>();
        public static void SetToolTip(ToolTip owner,Control control,string text)
        {
            var binding=tips.GetValue(control,delegate(Control key){return new TipBinding();});binding.Owner=owner;binding.Source=Source(text);owner.SetToolTip(control,T(binding.Source));
        }
        public static void Apply(Control root)
        {
            bool previous=Applying;Applying=true;
            try{ApplyTree(root);}finally{Applying=previous;}
        }
        private static void ApplyTree(Control root)
        {
            var combo=root as ComboBox;
            if(combo!=null)
            {
                int selected=combo.SelectedIndex;bool changed=false;
                for(int i=0;i<combo.Items.Count;i++)if(combo.Items[i] is string&&T(Source((string)combo.Items[i]))!=(string)combo.Items[i]){changed=true;break;}
                if(changed)
                {
                    combo.BeginUpdate();try{for(int i=0;i<combo.Items.Count;i++)if(combo.Items[i] is string)combo.Items[i]=T(Source((string)combo.Items[i]));combo.SelectedIndex=selected;}finally{combo.EndUpdate();}
                }
            }
            else if(!(root is TextBoxBase)&&!(root is NumericUpDown)&&!(root is GlassField))root.Text=T(Source(root.Text));
            TipBinding tip;if(tips.TryGetValue(root,out tip)&&tip.Owner!=null)tip.Owner.SetToolTip(root,T(tip.Source));
            foreach(Control child in root.Controls)ApplyTree(child);
            root.Invalidate();
        }
        public static void ApplyMenu(ToolStrip menu){foreach(ToolStripItem item in menu.Items)item.Text=T(Source(item.Text));menu.Invalidate();}
    }
}
