using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("SnapLite")]
[assembly: AssemblyDescription("Лёгкий инструмент для локальных скриншотов")]
[assembly: AssemblyVersion("1.1.0.0")]
namespace SnapLite
{
    internal static class Program
    {
        [STAThread] private static void Main(string[] args)
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException+=delegate(object s,ThreadExceptionEventArgs e){MessageBox.Show(e.Exception.Message,"SnapLite — ошибка",MessageBoxButtons.OK,MessageBoxIcon.Error);};
            int self=Array.IndexOf(args,"--self-test");
            if(self>=0){Tests.Run(self+1<args.Length?args[self+1]:Path.Combine(Path.GetTempPath(),"SnapLite-tests.txt"));return;}
            int profile=Array.IndexOf(args,"--test-profile");if(profile>=0&&profile+1<args.Length)Settings.ProfileDirectory=Path.GetFullPath(args[profile+1]);
            bool created;
            using(var openSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\SnapLite.ShowSettings.v1"))
            using(var mutex=new Mutex(true,"Local\\SnapLite.Application.v1",out created))
            {
                if(!created){openSignal.Set();return;}
                try{using(var ctx=new TrayApp(args,openSignal))Application.Run(ctx);}
                catch(Exception e){MessageBox.Show(e.Message,"SnapLite — запуск не удался",MessageBoxButtons.OK,MessageBoxIcon.Error);}
                finally{mutex.ReleaseMutex();}
            }
        }
    }
    internal sealed class HotkeyWindow : NativeWindow,IDisposable
    {
        private int activeId=100;private int key;private uint mods;private bool registered;
        public event Action CaptureRequested;public event Action SettingsRequested;
        public HotkeyWindow(){CreateHandle(new CreateParams{Caption="SnapLite.MessageWindow.v1"});}
        public bool TrySet(int key,uint mods)
        {
            if(registered&&this.key==key&&this.mods==mods)return true;
            int id=activeId==100?101:100;
            if(!Native.RegisterHotKey(Handle,id,mods|0x4000,(uint)key))return false;
            if(registered)Native.UnregisterHotKey(Handle,activeId);activeId=id;this.key=key;this.mods=mods;registered=true;return true;
        }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x0312&&m.WParam.ToInt32()==activeId){if(CaptureRequested!=null)CaptureRequested();}
            else if(m.Msg==0x8001){if(SettingsRequested!=null)SettingsRequested();}
            base.WndProc(ref m);
        }
        public void Dispose(){if(registered)Native.UnregisterHotKey(Handle,activeId);DestroyHandle();}
    }
    internal sealed class TrayApp : ApplicationContext
    {
        private Settings settings;
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private Icon icon;
        private readonly HotkeyWindow hotkey;
        private SettingsForm settingsForm;
        private readonly List<EditorForm> editors=new List<EditorForm>();
        private System.Windows.Forms.Timer captureTimer;
        private bool busy;
        private bool exit;
        private bool showSettingsAfterCapture;
        private readonly RegisteredWaitHandle openWait;
        public TrayApp(string[] args,EventWaitHandle openSignal)
        {
            bool first=!File.Exists(Settings.FilePath);string warning;settings=Settings.Load(out warning);
            Glass.Configure(settings);
            settings.AutoStart=IsAutoStart();
            icon=Theme.Icon();menu=new ContextMenuStrip{Font=new Font("Segoe UI",10)};
            menu.Items.Add("Выбрать область",null,delegate{BeginCapture(false);});
            menu.Items.Add("Весь экран под курсором",null,delegate{BeginCapture(true);});
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Открыть папку снимков",null,delegate{OpenFolder();});
            menu.Items.Add("Настройки…",null,delegate{ShowSettings();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Выход",null,delegate{Quit();});
            Glass.StyleMenu(menu);
            tray=new NotifyIcon {Icon=icon,Text="SnapLite",ContextMenuStrip=menu,Visible=true};
            tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)BeginCapture(false);};
            hotkey=new HotkeyWindow();hotkey.CaptureRequested+=delegate{if(settingsForm==null||!settingsForm.EditingHotkey)BeginCapture(false);};hotkey.SettingsRequested+=ShowSettings;
            openWait=ThreadPool.RegisterWaitForSingleObject(openSignal,delegate{Native.PostMessage(hotkey.Handle,0x8001,IntPtr.Zero,IntPtr.Zero);},null,Timeout.Infinite,false);
            if(!hotkey.TrySet(settings.Hotkey,settings.Modifiers))
            {
                settings.Hotkey=(int)Keys.S;settings.Modifiers=6;
                if(!hotkey.TrySet(settings.Hotkey,settings.Modifiers)){settings.Hotkey=(int)Keys.F8;settings.Modifiers=3;hotkey.TrySet(settings.Hotkey,settings.Modifiers);}
                warning="Исходная горячая клавиша занята. Проверьте сочетание в настройках.";
            }
            UpdateTray();
            if(Array.IndexOf(args,"--demo")>=0)ShowEditor(Tests.MakeDemo());
            else if(Array.IndexOf(args,"--capture")>=0)BeginCapture(false);
            else if(Array.IndexOf(args,"--tray")<0&&(first||Array.IndexOf(args,"--settings")>=0||warning!=null))ShowSettings();
            if(warning!=null)Notify(warning);
        }
        private void UpdateTray()
        {
            string key=Hotkeys.Label(settings.Hotkey,settings.Modifiers);tray.Text="SnapLite • "+key;
            menu.Items[0].Text="Выбрать область  ("+key+")";
        }
        private void ShowSettings()
        {
            if(exit)return;
            if(busy||captureTimer!=null){showSettingsAfterCapture=true;return;}
            if(settingsForm!=null&&!settingsForm.IsDisposed){settingsForm.Show();settingsForm.WindowState=FormWindowState.Normal;settingsForm.Activate();return;}
            settingsForm=new SettingsForm(settings,ApplySettings);settingsForm.FormClosed+=delegate{settingsForm=null;};settingsForm.Show();settingsForm.Activate();
        }
        private string ApplySettings(Settings value)
        {
            if(!hotkey.TrySet(value.Hotkey,value.Modifiers))return "Эта горячая клавиша уже занята другой программой. Выберите другое сочетание.";
            bool changeAuto=value.AutoStart!=settings.AutoStart;
            try
            {
                if(changeAuto)SetAutoStart(value.AutoStart);
                value.Save(Settings.FilePath);settings=value;Glass.Configure(settings);Glass.StyleMenu(menu);
                var priorIcon=icon;icon=Theme.Icon();tray.Icon=icon;priorIcon.Dispose();
                foreach(var editor in editors)if(!editor.IsDisposed)editor.ApplyAppearance(settings);
                UpdateTray();return null;
            }
            catch(Exception e)
            {
                hotkey.TrySet(settings.Hotkey,settings.Modifiers);
                if(changeAuto){try{SetAutoStart(settings.AutoStart);}catch{}}
                return e.Message;
            }
        }
        private bool IsAutoStart()
        {
            using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {var v=key==null?null:key.GetValue("SnapLite") as string;return String.Equals(v,"\""+Application.ExecutablePath+"\"",StringComparison.OrdinalIgnoreCase);}
        }
        private void SetAutoStart(bool enabled)
        {
            using(var key=Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {if(enabled)key.SetValue("SnapLite","\""+Application.ExecutablePath+"\"");else key.DeleteValue("SnapLite",false);}
        }
        private void BeginCapture(bool full)
        {
            if(exit)return;
            if(captureTimer!=null){captureTimer.Stop();captureTimer.Dispose();captureTimer=null;RestoreWindows();Notify("Захват отменён.");if(showSettingsAfterCapture){showSettingsAfterCapture=false;ShowSettings();}return;}
            if(busy)return;
            var monitor=Screen.FromPoint(Cursor.Position).Bounds;
            if(settingsForm!=null&&settingsForm.Visible)settingsForm.Hide();
            foreach(var f in editors)if(!f.IsDisposed)f.Hide();
            captureTimer=new System.Windows.Forms.Timer {Interval=settings.DelaySeconds*1000+160};
            captureTimer.Tick+=delegate
            {
                captureTimer.Stop();captureTimer.Dispose();captureTimer=null;
                busy=true;Bitmap selected=null;string requestedAction=settings.CaptureAction;
                try
                {
                    if(full)selected=Images.Capture(monitor,settings.IncludeCursor);
                    else
                    {
                        Rectangle desktop=SystemInformation.VirtualScreen;
                        using(var shot=Images.Capture(desktop,settings.IncludeCursor))
                        using(var selector=new SelectionForm(shot,desktop,settings.Magnifier,settings.CaptureAction,settings))
                        {if(selector.ShowDialog()==DialogResult.OK){selected=shot.Clone(selector.Selection,PixelFormat.Format32bppRgb);requestedAction=selector.RequestedAction;}}
                    }
                    RestoreWindows();
                    if(selected!=null)
                    {
                        if(requestedAction=="Editor"){ShowEditor(selected);selected=null;}
                        else using(var image=Images.Resize(selected,settings.OutputScale))
                        {if(requestedAction=="Copy"){Images.Copy(image);Notify("Скриншот скопирован в буфер обмена.");}else Notify("Сохранено: "+Images.SaveUnique(image,settings));}
                    }
                }
                catch(Exception e){RestoreWindows();MessageBox.Show(e.Message,"SnapLite — захват не удался",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                finally{if(selected!=null)selected.Dispose();busy=false;if(showSettingsAfterCapture){showSettingsAfterCapture=false;ShowSettings();}}
            };
            captureTimer.Start();
        }
        private void RestoreWindows()
        {
            if(settingsForm!=null&&!settingsForm.IsDisposed)settingsForm.Show();
            foreach(var f in editors)if(!f.IsDisposed)f.Show();
        }
        private void ShowEditor(Bitmap image)
        {
            var f=new EditorForm(image,settings,Notify);editors.Add(f);f.FormClosed+=delegate{editors.Remove(f);};f.Show();f.Activate();
        }
        private void OpenFolder()
        {
            try{Directory.CreateDirectory(settings.SaveFolder);Process.Start(new ProcessStartInfo{FileName=settings.SaveFolder,UseShellExecute=true});}
            catch(Exception e){MessageBox.Show(e.Message,"SnapLite — папка недоступна",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        private void Notify(string message)
        {
            if(!settings.Notifications)return;tray.ShowBalloonTip(2500,"SnapLite",message,ToolTipIcon.Info);
        }
        private void Quit(){exit=true;ExitThread();}
        protected override void ExitThreadCore(){tray.Visible=false;base.ExitThreadCore();}
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(captureTimer!=null){captureTimer.Stop();captureTimer.Dispose();}
                if(settingsForm!=null)settingsForm.Dispose();foreach(var f in editors.ToArray())f.Dispose();
                if(openWait!=null)openWait.Unregister(null);hotkey.Dispose();tray.Visible=false;tray.Dispose();menu.Dispose();icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
