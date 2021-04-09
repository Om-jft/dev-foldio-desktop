using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using CameraControl.Core;
using CameraControl.Core.Translation;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.Layouts;
using CameraControl.windows;
using Timer = System.Timers.Timer;
using Path = System.IO.Path;

using DSLR_Tool_PC.Controles;
using DSLR_Tool_PC.ViewModels;

using System.Timers;
using CameraControl.Core.Database;
using CameraControl.Core.TclScripting;
using CameraControl.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using HelpProvider = CameraControl.Core.Classes.HelpProvider;
using System.Windows.Controls.Primitives;

namespace DSLR_Tool_PC
{
    public partial class MainWindowNew : IMainWindowPlugin, INotifyPropertyChanged
    {
        public string DisplayName { get; set; }

        private object _locker = new object();
        private FileItem _selectedItem = null;
        private Timer _selectiontimer = new Timer(4000);
        private DateTime _lastLoadTime = DateTime.Now;

        private bool _sortCameraOreder = true;

        public RelayCommand<AutoExportPluginConfig> ConfigurePluginCommand { get; set; }
        public RelayCommand<IAutoExportPlugin> AddPluginCommand { get; set; }
        public RelayCommand<CameraPreset> SelectPresetCommand { get; private set; }
        public RelayCommand<CameraPreset> DeletePresetCommand { get; private set; }
        public RelayCommand<CameraPreset> LoadInAllPresetCommand { get; private set; }
        public RelayCommand<CameraPreset> VerifyPresetCommand { get; private set; }

        //Added Code
        PathUpdate pathUpdate = PathUpdate.getInstance();
        LVControler __lvControler = null;
        public double CameraGrid_width;
        public double CameraGrid_height;
        public double EditGrid_width;
        public double EditGrid_height;
        public int rotateLeftCounter = 1;
        public int rotateRightCounter = 1;
        public PhotoSession Session { get; set; }
        ///////////////////////////////////////////

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowAdvanced" /> class.
        /// </summary>
        public MainWindowNew()
        {
            DisplayName = "Default";
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (sender1, args) => this.Close()));

            SelectPresetCommand = new RelayCommand<CameraPreset>(SelectPreset);
            DeletePresetCommand = new RelayCommand<CameraPreset>(DeletePreset, (o) => ServiceProvider.Settings.CameraPresets.Count > 0);
            LoadInAllPresetCommand = new RelayCommand<CameraPreset>(LoadInAllPreset);
            VerifyPresetCommand = new RelayCommand<CameraPreset>(VerifyPreset);
            ConfigurePluginCommand = new RelayCommand<AutoExportPluginConfig>(ConfigurePlugin);
            AddPluginCommand = new RelayCommand<IAutoExportPlugin>(AddPlugin);

            InitializeComponent();

            //Added code
            this.EditLevelGraph.DataContext = pathUpdate.EditLevelGraphVM;
            this.CaptureLevelGraph.DataContext = pathUpdate.CaptureLevelGraphVM;


            //---------------------------------------------------------------------------------

            if (!string.IsNullOrEmpty(ServiceProvider.Branding.ApplicationTitle))
            {
                Title = ServiceProvider.Branding.ApplicationTitle;
            }
            if (!string.IsNullOrEmpty(ServiceProvider.Branding.LogoImage) &&
                File.Exists(ServiceProvider.Branding.LogoImage))
            {
                BitmapImage bi = new BitmapImage();
                // BitmapImage.UriSource must be in a BeginInit/EndInit block.
                bi.BeginInit();
                bi.UriSource = new Uri(PhotoUtils.GetFullPath(ServiceProvider.Branding.LogoImage));
                bi.EndInit();
                Icon = bi;
            }
            _selectiontimer.Elapsed += _selectiontimer_Elapsed;
            _selectiontimer.AutoReset = false;
            ServiceProvider.WindowsManager.Event += WindowsManager_Event;
        }

        public virtual event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //WiaManager = new WIAManager();
            //ServiceProvider.Settings.Manager = WiaManager;
            ServiceProvider.DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;

            DataContext = ServiceProvider.Settings;

            ServiceProvider.DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
            ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
            SetLayout(ServiceProvider.Settings.SelectedLayout);

            var thread = new Thread(StartupThread);
            thread.Start();

            if (ServiceProvider.Settings.StartMinimized)
                this.WindowState = WindowState.Minimized;
            //if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.Started)
            //{
            //    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.TimeLapseWnd_Show);
            //    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.TimeLapse_Start);
            //}
            SortCameras();
            __lvControler = new LVControler();
            //zoomAndPanControl.ScaleToFit();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.All_Close);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ServiceProvider.Settings.MinimizeToTrayIcon)
            {
                WindowState = WindowState.Minimized;
                e.Cancel = true;
            }

            ServiceProvider.DeviceManager.CameraSelected -= DeviceManager_CameraSelected;
            ServiceProvider.DeviceManager.CameraConnected -= DeviceManager_CameraConnected;
            ServiceProvider.DeviceManager.CameraDisconnected -= DeviceManager_CameraDisconnected;
        }

        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (txt_CaptureName.IsFocused)
                return;
            TriggerClass.KeyDown(e);

            string _keyvalue = TriggerClass.KeyDownReturn(e);
            if (_keyvalue == "") { return; }

            Keys_Shortcuts(_keyvalue);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Del_Image);
                e.Handled = true;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && ServiceProvider.Settings.MinimizeToTrayIcon &&
                !ServiceProvider.Settings.HideTrayNotifications)
            {
                this.Hide();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MyNotifyIcon.HideBalloonTip();
                    MyNotifyIcon.ShowBalloonTip("OrangeMonkie", "Application was minimized \n Double click to restore",
                        BalloonIcon.Info);
                }));
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(e.Delta > 0
                ? WindowsCmdConsts.Next_Image
                : WindowsCmdConsts.Prev_Image);
        }

        private static double __CameraGrid_ActualWidth = 0.0;
        private static double __CameraGrid_ActualHeight = 0.0;
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.RefreshDisplay);
            __CameraGrid_ActualWidth = CameraGrid.ActualWidth;
            __CameraGrid_ActualHeight = CameraGrid.ActualHeight;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //DSLR_Tool_PC.MainWindowNew wnd = new DSLR_Tool_PC.MainWindowNew();
            //wnd.ShowDialog();
            //this.Close();
        }

        private void AddPlugin(IAutoExportPlugin obj)
        {
            ConfigurePlugin(ServiceProvider.Settings.DefaultSession.AddPlugin(obj));
        }

        private void ConfigurePlugin(AutoExportPluginConfig plugin)
        {
            var pluginEdit = new AutoExportPluginEdit
            {
                DataContext = new AutoExportPluginEditViewModel(plugin),
                Owner = this
            };
            pluginEdit.ShowDialog();
        }

        private void WindowsManager_Event(string cmd, object o)
        {
            switch (cmd)
            {
                case CmdConsts.SortCameras:
                    SortCameras();
                    break;
                case WindowsCmdConsts.MainWnd_Message:
                    this.ShowMessageAsync("", o.ToString());
                    break;
                case WindowsCmdConsts.SetLayout:
                    SetLayout(o.ToString());
                    break;
                case WindowsCmdConsts.Restore:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            this.Show();
                            this.WindowState = WindowState.Normal;
                            this.Activate();
                            this.Focus();
                        }
                        catch (Exception e)
                        {
                            Log.Debug("Unable to restore window", e);
                        }
                    }));
                    break;
                case CmdConsts.All_Minimize:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        WindowState = WindowState.Minimized;
                    }));
                    break;
            }
        }

        private void LoadInAllPreset(CameraPreset preset)
        {
            if (preset == null)
                return;
            var dlg = new ProgressWindow();
            dlg.Show();
            try
            {
                int i = 0;
                dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
                foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
                {
                    if (connectedDevice == null || !connectedDevice.IsConnected)
                        continue;
                    try
                    {

                        dlg.Label = connectedDevice.DisplayName;
                        dlg.Progress = i;
                        i++;
                        preset.Set(connectedDevice);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Unable to set property ", exception);
                    }
                    Thread.Sleep(250);
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to set property ", exception);
            }
            dlg.Hide();
        }

        private void VerifyPreset(CameraPreset preset)
        {
            if (preset == null)
                return;
            var dlg = new ProgressWindow();
            dlg.Show();
            try
            {
                int i = 0;
                dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
                foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
                {
                    if (connectedDevice == null || !connectedDevice.IsConnected)
                        continue;
                    try
                    {
                        dlg.Label = connectedDevice.DisplayName;
                        dlg.Progress = i;
                        i++;
                        preset.Verify(connectedDevice);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Unable to set property ", exception);
                    }
                    Thread.Sleep(250);
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to set property ", exception);
            }
            dlg.Hide();
        }

        private void DeletePreset(CameraPreset obj)
        {
            if (obj == null)
                return;
            ServiceProvider.Settings.CameraPresets.Remove(obj);
            try
            {
                File.Delete(obj.FileName);
            }
            catch (Exception)
            {

            }
        }

        private void SelectPreset(CameraPreset preset)
        {
            if (preset == null)
                return;
            try
            {
                preset.Set(ServiceProvider.DeviceManager.SelectedCameraDevice);
            }
            catch (Exception exception)
            {
                Log.Error("Error set preset", exception);
            }
        }

        private void _selectiontimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_selectedItem != null)
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Select_Image, _selectedItem);
        }

        private void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!ServiceProvider.Settings.HideTrayNotifications)
                {
                    MyNotifyIcon.HideBalloonTip();
                    MyNotifyIcon.ShowBalloonTip("Camera connected", cameraDevice.LoadProperties().DeviceName, BalloonIcon.Info);
                    ServiceProvider.Branding.ShowCameraPropertiesMainWindow = true;

                    if (__lvControler == null) { __lvControler = new LVControler(); }
                    __lvControler.ExecuteCommand("LiveViewWnd_Show");
                }
                SortCameras();
            }));
        }

        private void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            Dispatcher.BeginInvoke(
                new Action(
                    delegate
                    {
                        Title = (ServiceProvider.Branding.ApplicationTitle ?? "OrangeMonkie") + " - " + (newcameraDevice == null ? "" : newcameraDevice.DisplayName);
                        ServiceProvider.Branding.ShowCameraPropertiesMainWindow = true;

                        if (__lvControler == null) { __lvControler = new LVControler(); }
                        __lvControler.ExecuteCommand("LiveViewWnd_Show");
                    }));
        }

        private void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            if (ServiceProvider.Settings.UseParallelTransfer)
            {
                PhotoCaptured(eventArgs);
            }
            else
            {
                lock (_locker)
                {
                    PhotoCaptured(eventArgs);
                }
            }
        }

        private void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!ServiceProvider.Settings.HideTrayNotifications)
                {
                    MyNotifyIcon.HideBalloonTip();
                    MyNotifyIcon.ShowBalloonTip("Camera disconnected", cameraDevice.LoadProperties().DeviceName, BalloonIcon.Info);
                    ServiceProvider.Branding.ShowCameraPropertiesMainWindow = false;
                    StaticClass.CapturedImageCount = 0; tab_video.IsEnabled = true; tab_Single.IsEnabled = true;
                }
            }));
        }

        private void StartupThread()
        {
            foreach (var cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
            {
                Log.Debug("cameraDevice_CameraInitDone 1");
                var property = cameraDevice.LoadProperties();
                CameraPreset preset = ServiceProvider.Settings.GetPreset(property.DefaultPresetName);
                // multiple canon cameras block with this settings

                if ((cameraDevice is CanonSDKBase && ServiceProvider.Settings.LoadCanonTransferMode) || !(cameraDevice is CanonSDKBase))
                    cameraDevice.CaptureInSdRam = property.CaptureInSdRam;

                Log.Debug("cameraDevice_CameraInitDone 1a");
                if (ServiceProvider.Settings.SyncCameraDateTime)
                {
                    try
                    {
                        cameraDevice.DateTime = DateTime.Now;
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Unable to sysnc date time", exception);
                    }
                }
                if (preset != null)
                {
                    try
                    {
                        Thread.Sleep(500);
                        cameraDevice.WaitForCamera(5000);
                        preset.Set(cameraDevice);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unable to load default preset", e);
                    }
                }
            }
            var scriptFile = ServiceProvider.Settings.StartupScript;
            if (scriptFile != null && File.Exists(scriptFile))
            {
                if (Path.GetExtension(scriptFile.ToLower()) == ".tcl")
                {
                    try
                    {
                        var manager = new TclScripManager();
                        manager.Execute(File.ReadAllText(scriptFile));
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Script error", exception);
                        StaticHelper.Instance.SystemMessage = "Script error :" + exception.Message;
                    }
                }
                else
                {
                    var script = ServiceProvider.ScriptManager.Load(scriptFile);
                    script.CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                    ServiceProvider.ScriptManager.Execute(script);
                }
            }
            if ((DateTime.Now - ServiceProvider.Settings.LastUpdateCheckDate).TotalDays > 7)
            {
                if (!ServiceProvider.Branding.CheckForUpdate)
                    return;

                Thread.Sleep(2000);
                ServiceProvider.Settings.LastUpdateCheckDate = DateTime.Now;
                ServiceProvider.Settings.Save();
                Dispatcher.Invoke(new Action(() => NewVersionWnd.CheckForUpdate(false)));
            }
            else
            {
                if (!ServiceProvider.Branding.ShowWelcomeScreen || !ServiceProvider.Branding.OnlineReference)
                    return;

                // show welcome screen only if not start minimized
                if (!ServiceProvider.Settings.StartMinimized)
                {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var wnd = new Welcome();
                            wnd.ShowDialog();
                        }
                        catch
                        {
                        }
                    });
                }
            }

            Dispatcher.BeginInvoke(
                new Action(
                    delegate
                    {
                        Thread.Sleep(1500);
                        ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Zoom_Image_Fit);
                    }));
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
                return;
            try
            {
                Log.Debug(TranslationStrings.MsgPhotoTransferBegin);
                eventArgs.CameraDevice.IsBusy = true;
                var extension = Path.GetExtension(eventArgs.FileName);

                // the capture is for live view preview 
                if (LiveViewManager.PreviewRequest.ContainsKey(eventArgs.CameraDevice) &&
                    LiveViewManager.PreviewRequest[eventArgs.CameraDevice])
                {
                    LiveViewManager.PreviewRequest[eventArgs.CameraDevice] = false;
                    var file = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName() + extension);
                    eventArgs.CameraDevice.TransferFile(eventArgs.Handle, file);
                    eventArgs.CameraDevice.IsBusy = false;
                    eventArgs.CameraDevice.TransferProgress = 0;
                    eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
                    LiveViewManager.Preview[eventArgs.CameraDevice] = file;
                    LiveViewManager.OnPreviewCaptured(eventArgs.CameraDevice, file);
                    return;
                }

                CameraProperty property = eventArgs.CameraDevice.LoadProperties();
                PhotoSession session = (PhotoSession)eventArgs.CameraDevice.AttachedPhotoSession ??
                                       ServiceProvider.Settings.DefaultSession;
                StaticHelper.Instance.SystemMessage = "";


                if (!eventArgs.CameraDevice.CaptureInSdRam || PhotoUtils.IsMovie(eventArgs.FileName))
                {
                    if (property.NoDownload)
                    {
                        eventArgs.CameraDevice.IsBusy = false;
                        eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
                        StaticHelper.Instance.SystemMessage = "File transfer disabled";
                        return;
                    }
                    if (extension != null && (session.DownloadOnlyJpg && extension.ToLower() != ".jpg"))
                    {
                        eventArgs.CameraDevice.IsBusy = false;
                        eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
                        return;
                    }
                }

                StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferBegin;

                string tempFile = Path.GetTempFileName();

                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                Stopwatch stopWatch = new Stopwatch();
                // transfer file from camera to temporary folder 
                // in this way if the session folder is used as hot folder will prevent write errors
                stopWatch.Start();
                if (!eventArgs.CameraDevice.CaptureInSdRam && session.DownloadThumbOnly)
                    eventArgs.CameraDevice.TransferFileThumb(eventArgs.Handle, tempFile);
                else
                    eventArgs.CameraDevice.TransferFile(eventArgs.Handle, tempFile);
                eventArgs.CameraDevice.TransferProgress = 0;
                eventArgs.CameraDevice.IsBusy = false;
                stopWatch.Stop();
                string strTransfer = "Transfer time : " + stopWatch.Elapsed.TotalSeconds.ToString("##.###") + " Speed :" +
                                     Math.Round(
                                         new System.IO.FileInfo(tempFile).Length / 1024.0 / 1024 /
                                         stopWatch.Elapsed.TotalSeconds, 2) + " Mb/s";
                Log.Debug(strTransfer);

                string fileName = "";
                if (!session.UseOriginalFilename || eventArgs.CameraDevice.CaptureInSdRam)
                {
                    fileName =
                        session.GetNextFileName(eventArgs.FileName, eventArgs.CameraDevice, tempFile);
                }
                else
                {
                    fileName = Path.Combine(session.Folder, eventArgs.FileName);
                    if (File.Exists(fileName) && !session.AllowOverWrite)
                        fileName =
                            StaticHelper.GetUniqueFilename(
                                Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) +
                                "_", 0,
                                Path.GetExtension(fileName));
                }

                if (session.AllowOverWrite && File.Exists(fileName))
                {
                    PhotoUtils.WaitForFile(fileName);
                    File.Delete(fileName);
                }

                // make lower case extension 
                if (session.LowerCaseExtension && !string.IsNullOrEmpty(Path.GetExtension(fileName)))
                {
                    fileName = Path.Combine(Path.GetDirectoryName(fileName),
                        Path.GetFileNameWithoutExtension(fileName) + Path.GetExtension(fileName).ToLower());
                }


                if (session.AskSavePath)
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = "All files|*.*";
                    dialog.Title = "Save captured photo";
                    dialog.FileName = fileName;
                    dialog.InitialDirectory = Path.GetDirectoryName(fileName);
                    if (dialog.ShowDialog() == true)
                    {
                        fileName = dialog.FileName;
                    }
                    else
                    {
                        eventArgs.CameraDevice.IsBusy = false;
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                        return;
                    }
                }

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }


                string backupfile = null;
                if (session.BackUp)
                {
                    backupfile = session.CopyBackUp(tempFile, fileName);
                    if (string.IsNullOrEmpty(backupfile))
                        StaticHelper.Instance.SystemMessage = "Unable to save the backup";
                }

                // execute plugins which are executed before transfer 
                if (ServiceProvider.Settings.DefaultSession.AutoExportPluginConfigs.Count((x) => !x.RunAfterTransfer) > 0)
                {
                    FileItem tempitem = new FileItem(tempFile);
                    tempitem.Name = Path.GetFileName(fileName);
                    tempitem.BackupFileName = backupfile;
                    tempitem.Series = session.Series;
                    tempitem.AddTemplates(eventArgs.CameraDevice, session);
                    ExecuteAutoexportPlugins(eventArgs.CameraDevice, tempitem, false);
                }


                if ((!eventArgs.CameraDevice.CaptureInSdRam || PhotoUtils.IsMovie(fileName)) && session.DeleteFileAfterTransfer)
                    eventArgs.CameraDevice.DeleteObject(new DeviceObject() { Handle = eventArgs.Handle });

                File.Copy(tempFile, fileName);

                if (File.Exists(tempFile))
                {
                    PhotoUtils.WaitForFile(tempFile);
                    File.Delete(tempFile);
                }

                if (session.WriteComment)
                {
                    if (!string.IsNullOrEmpty(session.Comment))
                        Exiv2Helper.SaveComment(fileName, session.Comment);
                    if (session.SelectedTag1 != null && !string.IsNullOrEmpty(session.SelectedTag1.Value))
                        Exiv2Helper.AddKeyword(fileName, session.SelectedTag1.Value);
                    if (session.SelectedTag2 != null && !string.IsNullOrEmpty(session.SelectedTag2.Value))
                        Exiv2Helper.AddKeyword(fileName, session.SelectedTag2.Value);
                    if (session.SelectedTag3 != null && !string.IsNullOrEmpty(session.SelectedTag3.Value))
                        Exiv2Helper.AddKeyword(fileName, session.SelectedTag3.Value);
                    if (session.SelectedTag4 != null && !string.IsNullOrEmpty(session.SelectedTag4.Value))
                        Exiv2Helper.AddKeyword(fileName, session.SelectedTag4.Value);
                }

                if (session.ExternalData != null)
                    session.ExternalData.FileName = fileName;

                // prevent crash og GUI when item count updated
                Dispatcher.Invoke(new Action(delegate
                {
                    try
                    {
                        _selectedItem = session.GetNewFileItem(fileName);
                        _selectedItem.BackupFileName = backupfile;
                        _selectedItem.Series = session.Series;
                        // _selectedItem.Transformed = tempitem.Transformed;
                        _selectedItem.AddTemplates(eventArgs.CameraDevice, session);
                        ServiceProvider.Database.Add(new DbFile(_selectedItem, eventArgs.CameraDevice.SerialNumber, eventArgs.CameraDevice.DisplayName, session.Name));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("PhotoCaptured", ex);
                    }
                }));

                // execute plugins which are executed after transfer  
                ExecuteAutoexportPlugins(eventArgs.CameraDevice, _selectedItem, true);

                Dispatcher.Invoke(() =>
                {
                    _selectedItem.RemoveThumbs();
                    session.Add(_selectedItem);
                    ServiceProvider.OnFileTransfered(_selectedItem);
                });


                if (ServiceProvider.Settings.MinimizeToTrayIcon && !IsVisible && !ServiceProvider.Settings.HideTrayNotifications)
                {
                    MyNotifyIcon.HideBalloonTip();
                    MyNotifyIcon.ShowBalloonTip("Photo transfered", fileName, BalloonIcon.Info);
                }

                ServiceProvider.DeviceManager.LastCapturedImage[eventArgs.CameraDevice] = fileName;

                //select the new file only when the multiple camera support isn't used to prevent high CPU usage on raw files
                if (ServiceProvider.Settings.AutoPreview &&
                    !ServiceProvider.WindowsManager.Get(typeof(MultipleCameraWnd)).IsVisible &&
                    !ServiceProvider.Settings.UseExternalViewer)
                {
                    if ((Path.GetExtension(fileName).ToLower() == ".jpg" && ServiceProvider.Settings.AutoPreviewJpgOnly) ||
                        !ServiceProvider.Settings.AutoPreviewJpgOnly)
                    {
                        if ((DateTime.Now - _lastLoadTime).TotalSeconds < 4)
                        {
                            _selectiontimer.Stop();
                            _selectiontimer.Start();
                        }
                        else
                        {
                            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.Select_Image, _selectedItem);
                        }
                    }
                }
                _lastLoadTime = DateTime.Now;
                //ServiceProvider.Settings.Save(session);
                StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferDone + " " + strTransfer;

                if (ServiceProvider.Settings.UseExternalViewer &&
                    File.Exists(ServiceProvider.Settings.ExternalViewerPath))
                {
                    string arg = ServiceProvider.Settings.ExternalViewerArgs;
                    arg = arg.Contains("%1") ? arg.Replace("%1", fileName) : arg + " " + fileName;
                    PhotoUtils.Run(ServiceProvider.Settings.ExternalViewerPath, arg, ProcessWindowStyle.Normal);
                }
                if (ServiceProvider.Settings.PlaySound)
                {
                    PhotoUtils.PlayCaptureSound();
                }
                eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);

                //show fullscreen only when the multiple camera support isn't used
                if (ServiceProvider.Settings.Preview &&
                    !ServiceProvider.WindowsManager.Get(typeof(MultipleCameraWnd)).IsVisible &&
                    !ServiceProvider.Settings.UseExternalViewer)

                    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.FullScreenWnd_ShowTimed);

                Log.Debug("Photo transfer done.");
                DSLR_Tool_PC.StaticClass.FileName_LastCapturedImage = fileName;
                DSLR_Tool_PC.StaticClass.IsCapturedPhoto = true;
            }
            catch (Exception ex)
            {
                eventArgs.CameraDevice.IsBusy = false;
                StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferError + " " + ex.Message;
                Log.Error("Transfer error !", ex);
            }
            // not indicated to be used 
            GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        private void ExecuteAutoexportPlugins(ICameraDevice cameraDevice, FileItem fileItem, bool runOnTransfered)
        {
            foreach (AutoExportPluginConfig plugin in ServiceProvider.Settings.DefaultSession.AutoExportPluginConfigs)
            {
                if (plugin.RunAfterTransfer != runOnTransfered)
                    continue;
                if (!plugin.IsEnabled)
                    continue;
                if (!plugin.Evaluate(cameraDevice))
                    continue;

                var pl = ServiceProvider.PluginManager.GetAutoExportPlugin(plugin.Type);
                try
                {
                    pl.Execute(fileItem, plugin);
                    ServiceProvider.Analytics.PluginExecute(plugin.Type);
                    Log.Debug("AutoexportPlugin executed " + plugin.Type);
                }
                catch (Exception ex)
                {
                    plugin.IsError = true;
                    plugin.Error = ex.Message;
                    plugin.IsRedy = true;
                    Log.Error("Error to apply plugin", ex);
                }
            }
        }

        private void EditSession()
        {
            try
            {
                EditSession editSession = new EditSession(ServiceProvider.Settings.DefaultSession);
                editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
                editSession.ShowDialog();
                ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

        private void SetLayout(string enumname)
        {
            LayoutTypeEnum type;
            if (Enum.TryParse(enumname, true, out type))
            {
                SetLayout(type);
            }
        }

        private void SetLayout(LayoutTypeEnum type)
        {
            ServiceProvider.Settings.SelectedLayout = type.ToString();
            if (StackLayout.Children.Count > 0)
            {
                var cnt = StackLayout.Children[0] as LayoutBase;
                if (cnt != null)
                    cnt.UnInit();
            }
            switch (type)
            {
                case LayoutTypeEnum.Normal:
                    {
                        StackLayout.Children.Clear();
                        LayoutNormal control = new LayoutNormal();
                        StackLayout.Children.Add(control);
                    }
                    break;
                case LayoutTypeEnum.Grid:
                    {
                        StackLayout.Children.Clear();
                        LayoutGrid control = new LayoutGrid();
                        StackLayout.Children.Add(control);
                    }
                    break;
                case LayoutTypeEnum.GridRight:
                    {
                        StackLayout.Children.Clear();
                        LayoutGridRight control = new LayoutGridRight();
                        StackLayout.Children.Add(control);
                    }
                    break;
                case LayoutTypeEnum.GridBottom:
                    {
                        StackLayout.Children.Clear();
                        LayoutGridBottom control = new LayoutGridBottom();
                        StackLayout.Children.Add(control);
                    }
                    break;
            }
        }

        private void SortCameras()
        {
            SortCameras(_sortCameraOreder);
        }

        private void SortCameras(bool asc)
        {
            _sortCameraOreder = asc;

            // making sure the camera names are refreshed from properties
            foreach (var device in ServiceProvider.DeviceManager.ConnectedDevices)
            {
                device.LoadProperties();
            }
            if (asc)
            {
                ServiceProvider.DeviceManager.ConnectedDevices =
                    new AsyncObservableCollection<ICameraDevice>(
                        ServiceProvider.DeviceManager.ConnectedDevices.OrderBy(x => x.LoadProperties().SortOrder).ThenBy(x => x.DisplayName));
            }
            else
            {
                ServiceProvider.DeviceManager.ConnectedDevices =
                    new AsyncObservableCollection<ICameraDevice>(
                        ServiceProvider.DeviceManager.ConnectedDevices.OrderByDescending(x => x.LoadProperties().SortOrder).ThenByDescending(x => x.DisplayName));
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var res = e.GetPosition(PrviewImage);
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.ZoomPoint + "_" +
                                                          (res.X / PrviewImage.ActualWidth) + "_" +
                                                          (res.Y / PrviewImage.ActualHeight));
        }

        private void PrviewImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var res = e.GetPosition(PrviewImage);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.ZoomPoint + "_" +
                                                              (res.X / PrviewImage.ActualWidth) + "_" +
                                                              (res.Y / PrviewImage.ActualHeight) + "_!");
            }
        }

        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        Timer _timer = new Timer(1000);
        TurnTableViewModel _ttVM = TurnTableViewModel.GetInstance();
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (StaticClass.CapturedImageCount == 0)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
                //tab_Single.IsEnabled = true;
                //tab_video.IsEnabled = true;
            }
        }

        private void CapturePhoto()
        {
            if (ServiceProvider.DeviceManager.SelectedCameraDevice == null)
                return;
            Log.Debug("Main window capture started");
            try
            {
                if (ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed != null &&
                    ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed.Value == "Bulb")
                {
                    if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.Bulb))
                    {
                        ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.BulbWnd_Show,
                                                                      ServiceProvider.DeviceManager.SelectedCameraDevice);
                        return;
                    }
                    else
                    {
                        this.ShowMessageAsync("Error", TranslationStrings.MsgBulbModeNotSupported);
                        return;
                    }
                }
                //CameraHelper.Capture(ServiceProvider.DeviceManager.SelectedCameraDevice);
                ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.Capture);

            }
            catch (DeviceException exception)
            {
                StaticHelper.Instance.SystemMessage = exception.Message;
                Log.Error("Take photo", exception);
            }
            catch (Exception exception)
            {
                StaticHelper.Instance.SystemMessage = exception.Message;
                Log.Error("Take photo", exception);
            }
        }

        private void but_timelapse_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.TimeLapseWnd_Show);
        }

        private void but_fullscreen_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.FullScreenWnd_Show);
        }

        private void btn_br_Click(object sender, RoutedEventArgs e)
        {
            BraketingWnd wnd = new BraketingWnd(ServiceProvider.DeviceManager.SelectedCameraDevice,
                                                ServiceProvider.Settings.DefaultSession);
            wnd.ShowDialog();
        }

        private void btn_browse_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.BrowseWnd_Show);
        }

        private void but_download_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.DownloadPhotosWnd_Show,
                                                          ServiceProvider.DeviceManager.SelectedCameraDevice);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MultipleCameraWnd_Show);
        }

        private void but_star_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.BulbWnd_Show,
                                                          ServiceProvider.DeviceManager.SelectedCameraDevice);
        }

        private void but_wifi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(ServiceProvider.Settings.WifiIp))
                    ServiceProvider.Settings.WifiIp = "192.168.1.1";
                var wnd = new GetIpWnd();
                wnd.Owner = this;
                if (wnd.ShowDialog() == true)
                {
                    ServiceProvider.DeviceManager.AddDevice(wnd.WifiDeviceProvider.Connect(wnd.Ip));
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to connect to WiFi device", exception);
                this.ShowMessageAsync("Error", "Unable to connect to WiFi device " + exception.Message);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CameraPreset cameraPreset = new CameraPreset();
            SavePresetWnd wnd = new SavePresetWnd(cameraPreset);
            wnd.Owner = this;
            if (wnd.ShowDialog() == true)
            {
                foreach (CameraPreset preset in ServiceProvider.Settings.CameraPresets)
                {
                    if (preset.Name == cameraPreset.Name)
                    {
                        cameraPreset = preset;
                        break;
                    }
                }
                cameraPreset.Get(ServiceProvider.DeviceManager.SelectedCameraDevice);
                if (!ServiceProvider.Settings.CameraPresets.Contains(cameraPreset))
                    ServiceProvider.Settings.CameraPresets.Add(cameraPreset);
                ServiceProvider.Settings.Save();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PresetEditWnd wnd = new PresetEditWnd();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void but_print_Click(object sender, RoutedEventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.PrintWnd_Show);
        }

        private void but_qr_Click(object sender, RoutedEventArgs e)
        {
            QrCodeWnd wnd = new QrCodeWnd();
            wnd.Owner = this;
            wnd.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.ShowInputAsync(TranslationStrings.LabelEmailPublicWebAddress, "Email").ContinueWith(s =>
            {
                //if (!string.IsNullOrEmpty(s.Result))
                //    HelpProvider.SendEmail(
                //        "digiCamControl public web address " + ServiceProvider.Settings.PublicWebAdress,
                //        "digiCamControl public web address ", "postmaster@digicamcontrol.com", s.Result);
            }
            );

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            EditSession();
        }

        private void Tg_Btn6_Checked(object sender, RoutedEventArgs e)
        {
            V_CenterLineApply(true);
        }
        private void Tg_Btn6_Unchecked(object sender, RoutedEventArgs e)
        {
            V_CenterLineApply(false);
        }

        private void V_CenterLineApply(bool _checkedStatus)
        {
            if (_checkedStatus == true)
            {
                if (EditTab.IsSelected)
                {
                    EV_Line1.Y2 = CameraGrid.ActualHeight;
                    EV_Line1.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (EditTab.IsSelected)
                {
                    EV_Line1.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Tg_Btn7_Checked(object sender, RoutedEventArgs e)
        {
            H_CenterLineApply(true);
        }
        private void Tg_Btn7_Unchecked(object sender, RoutedEventArgs e)
        {
            H_CenterLineApply(false);
        }

        private void H_CenterLineApply(bool _checkedStatus)
        {
            if (_checkedStatus == true)
            {
                if (EditTab.IsSelected)
                {
                    EH_Line1.X2 = CameraGrid.ActualWidth;
                    EH_Line1.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (EditTab.IsSelected)
                {
                    EH_Line1.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void grid2_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = ((EditPicGrid.ActualWidth - 10) / 25) * 20;
                double height = (EditPicGrid.ActualHeight / 60) * 50;
                EVG_Line1.X1 = width / 3;
                EVG_Line1.X2 = width / 3;
                EVG_Line1.Y2 = height;
                EVG_Line1.Y1 = 0;
                EVG_Line2.X1 = (2 * width) / 3;
                EVG_Line2.X2 = (2 * width) / 3;
                EVG_Line2.Y1 = 0;
                EVG_Line2.Y2 = height;
                EHG_Line1.X1 = 0;
                EHG_Line1.Y1 = height / 3;
                EHG_Line1.X2 = width;
                EHG_Line1.Y2 = height / 3;
                EHG_Line2.X1 = 0;
                EHG_Line2.Y1 = (2 * height) / 3;
                EHG_Line2.X2 = width;
                EHG_Line2.Y2 = (2 * height) / 3;
                EVG_Line1.Visibility = Visibility.Visible;
                EVG_Line2.Visibility = Visibility.Visible;
                EHG_Line1.Visibility = Visibility.Visible;
                EHG_Line2.Visibility = Visibility.Visible;
            }
        }
        private void grid2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = Visibility.Collapsed;
                EVG_Line2.Visibility = Visibility.Collapsed;
                EHG_Line1.Visibility = Visibility.Collapsed;
                EHG_Line2.Visibility = Visibility.Collapsed;
            }
        }
        private void grid3_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = ((EditPicGrid.ActualWidth - 10) / 25) * 20;
                double height = (EditPicGrid.ActualHeight / 60) * 50;
                EVG_Line1.X1 = width / 6;
                EVG_Line1.X2 = width / 6;
                EVG_Line1.Y2 = height;
                EVG_Line1.Y1 = 0;
                EVG_Line2.X1 = width / 3;
                EVG_Line2.X2 = width / 3;
                EVG_Line2.Y2 = height;
                EVG_Line2.Y1 = 0;
                EVG_Line3.X1 = width / 2;
                EVG_Line3.X2 = width / 2;
                EVG_Line3.Y2 = height;
                EVG_Line3.Y1 = 0;
                EVG_Line4.X1 = (2 * width) / 3;
                EVG_Line4.X2 = (2 * width) / 3;
                EVG_Line4.Y2 = height;
                EVG_Line4.Y1 = 0;
                EVG_Line5.X1 = (5 * width) / 6;
                EVG_Line5.X2 = (5 * width) / 6;
                EVG_Line5.Y2 = height;
                EVG_Line5.Y1 = 0;
                EHG_Line1.X1 = 0;
                EHG_Line1.Y1 = height / 4;
                EHG_Line1.X2 = width;
                EHG_Line1.Y2 = height / 4;
                EHG_Line2.X1 = 0;
                EHG_Line2.Y1 = height / 2;
                EHG_Line2.X2 = width;
                EHG_Line2.Y2 = height / 2;
                EHG_Line3.X1 = 0;
                EHG_Line3.Y1 = (3 * height) / 4;
                EHG_Line3.X2 = width;
                EHG_Line3.Y2 = (3 * height) / 4;
                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = EVG_Line5.Visibility = Visibility.Visible;
                EHG_Line1.Visibility = EHG_Line2.Visibility = EHG_Line3.Visibility = Visibility.Visible;
            }
        }
        private void grid3_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = EVG_Line5.Visibility = Visibility.Collapsed;
                EHG_Line1.Visibility = EHG_Line2.Visibility = EHG_Line3.Visibility = Visibility.Collapsed;
            }
        }
        private void grid4_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = ((EditPicGrid.ActualWidth - 10) / 25) * 20;
                double height = (EditPicGrid.ActualHeight / 60) * 50;
                EVG_Line1.X1 = width / 3;
                EVG_Line1.X2 = width / 3;
                EVG_Line1.Y2 = height;
                EVG_Line1.Y1 = 0;
                EVG_Line2.X1 = (2 * width) / 3;
                EVG_Line2.X2 = (2 * width) / 3;
                EVG_Line2.Y1 = 0;
                EVG_Line2.Y2 = height;
                EHG_Line1.X1 = 0;
                EHG_Line1.Y1 = height / 3;
                EHG_Line1.X2 = width;
                EHG_Line1.Y2 = height / 3;
                EHG_Line2.X1 = 0;
                EHG_Line2.Y1 = (2 * height) / 3;
                EHG_Line2.X2 = width;
                EHG_Line2.Y2 = (2 * height) / 3;
                ////Diagonals
                EVG_Line3.X1 = 0;
                EVG_Line3.X2 = width;
                EVG_Line3.Y2 = height;
                EVG_Line4.X1 = width;
                EVG_Line4.X2 = 0;
                EVG_Line4.Y1 = 0;
                EVG_Line4.Y2 = height;

                //EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = Visibility.Visible;
                //EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Visible;

                EVG_Line1.Visibility = EVG_Line2.Visibility = Visibility.Visible;
                EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Visible;
                EVG_Line3.Visibility = EVG_Line4.Visibility = Visibility.Visible;

            }
        }
        private void grid4_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = Visibility.Collapsed;
                EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Collapsed;
            }
        }
        private void ratio1_Checked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                CameraGrid_height = __CameraGrid_ActualHeight;
                CameraGrid_width = __CameraGrid_ActualWidth;
                if (CameraGrid_height < CameraGrid_width)
                {
                    CameraGrid.Width = CameraGrid_height;
                    CameraGrid.Height = CameraGrid_height;
                }
                else if (CameraGrid_width < CameraGrid_height)
                {
                    CameraGrid.Height = CameraGrid_width;
                    CameraGrid.Width = CameraGrid_width;
                }
            }
            else if (EditTab.IsSelected)
            {
                EditGrid_height = EditPicGrid.ActualHeight;
                EditGrid_width = EditPicGrid.ActualWidth;
                if (EditGrid_height < EditGrid_width)
                {
                    CropOut.Width = EditGrid_height;
                    CropOut.Height = EditGrid_height;
                }
                else if (CameraGrid_width < CameraGrid_height)
                {
                    CropOut.Height = EditGrid_width;
                    CropOut.Width = EditGrid_width;
                }
                if (EditPicGrid.Width >= 0)
                {
                    Text_X.Text = ((EditPicGrid.Width - CropOut.Width) / 2).ToString();
                    Text_Y.Text = ((EditPicGrid.Height - CropOut.Height) / 2).ToString();
                }
                else
                {
                    Text_X.Text = ((EditPicGrid.ActualWidth - CropOut.Width) / 2).ToString();
                    Text_Y.Text = ((EditPicGrid.ActualHeight - CropOut.Height) / 2).ToString();
                }
                Text_Width.Text = CropOut.Width.ToString();
                Text_Height.Text = CropOut.Height.ToString();
                CropOut.Visibility = Visibility.Visible;
            }
        }
        private void ratio1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;
            }
            else if (EditTab.IsSelected)
            {
                EditPicGrid.Width = CameraGrid_width;
                EditPicGrid.Height = CameraGrid_height;
                CropOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ratio2_Checked(object sender, RoutedEventArgs e)
        {
            double height, width;
            if (CaptureTab.IsSelected)
            {
                if (CameraGrid_height > CameraGrid_width)
                {
                    height = width = CameraGrid_width;
                }
                else
                {
                    width = CameraGrid_width;
                    height = CameraGrid_height;
                }
                int remainder = (int)width % 4;
                width = width - remainder;
                int i = (int)width;
                while (i > 0)
                {
                    if (i % 4 == 0 && ((i / 4) * 3) <= height)
                    {
                        width = i;
                        height = (i / 4) * 3;
                        break;
                    }
                    i = i - 1;
                }
                CameraGrid.Width = width;
                CameraGrid.Height = height;
            }
            else if (EditTab.IsSelected)
            {
                if (EditGrid_height > EditGrid_width)
                {
                    height = width = EditGrid_width;
                }
                else
                {
                    width = EditGrid_width;
                    height = EditGrid_height;
                }
                int remainder = (int)width % 4;
                width = width - remainder;
                int i = (int)width;
                while (i > 0)
                {
                    if (i % 4 == 0 && ((i / 4) * 3) <= height)
                    {
                        width = i;
                        height = (i / 4) * 3;
                        break;
                    }
                    i = i - 1;
                }
                CropOut.Width = width;
                CropOut.Height = height;
                Text_X.Text = ((EditPicGrid.Width - CropOut.Width) / 2).ToString();
                Text_Y.Text = ((EditPicGrid.Height - CropOut.Height) / 2).ToString();
                Text_Width.Text = CropOut.Width.ToString();
                Text_Height.Text = CropOut.Height.ToString();
                CropOut.Visibility = Visibility.Visible;
            }
        }
        private void ratio2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;
            }
            else if (EditTab.IsSelected)
            {
                EditPicGrid.Width = CameraGrid_width;
                EditPicGrid.Height = CameraGrid_height;
                CropOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ratio3_Checked(object sender, RoutedEventArgs e)
        {
            double height, width;
            if (CaptureTab.IsSelected)
            {
                if (CameraGrid_height > CameraGrid_width)
                {
                    height = width = CameraGrid_width;
                }
                else
                {
                    width = CameraGrid_width;
                    height = CameraGrid_height;
                }
                int remainder = (int)width % 16;
                width = width - remainder;
                int i = (int)width;
                while (i > 0)
                {
                    if (i % 16 == 0 && ((i / 16) * 9) <= height)
                    {
                        width = i;
                        height = (i / 16) * 9;
                        break;
                    }
                    i = i--;
                }
                CameraGrid.Width = width;
                CameraGrid.Height = height;
            }
            else if (EditTab.IsSelected)
            {
                if (EditGrid_height > EditGrid_width)
                {
                    height = width = EditGrid_width;
                }
                else
                {
                    width = EditGrid_width;
                    height = EditGrid_height;
                }
                int remainder = (int)width % 16;
                width = width - remainder;
                int i = (int)width;
                while (i > 0)
                {
                    if (i % 16 == 0 && ((i / 16) * 9) <= height)
                    {
                        width = i;
                        height = (i / 16) * 9;
                        break;
                    }
                    i = i--;
                }
                CropOut.Width = width;
                CropOut.Height = height;
                Text_X.Text = ((EditPicGrid.Width - CropOut.Width) / 2).ToString();
                Text_Y.Text = ((EditPicGrid.Height - CropOut.Height) / 2).ToString();
                Text_Width.Text = CropOut.Width.ToString();
                Text_Height.Text = CropOut.Height.ToString();
                CropOut.Visibility = Visibility.Visible;
            }
        }
        private void ratio3_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;
            }
            else if (EditTab.IsSelected)
            {
                EditPicGrid.Width = CameraGrid_width;
                EditPicGrid.Height = CameraGrid_height;
                CropOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ETg_Btn8_Checked(object sender, RoutedEventArgs e)
        {
            Overlay1.Visibility = OverLay2.Visibility = Overlay3.Visibility = Overlay4.Visibility = Visibility.Visible;
        }
        private void ETg_Btn8_Unchecked(object sender, RoutedEventArgs e)
        {
            Overlay1.Visibility = OverLay2.Visibility = Overlay3.Visibility = Overlay4.Visibility = Visibility.Collapsed;
        }
        private void ETg_Btn9_Checked(object sender, RoutedEventArgs e)
        {
            LabelX.Visibility = LabelY.Visibility = LabelW.Visibility = LabelH.Visibility =
            Text_X.Visibility = Text_Y.Visibility = Text_Width.Visibility = Text_Height.Visibility = Eratio1.Visibility =
                Eratio2.Visibility = Eratio3.Visibility = ButtonOK.Visibility = Visibility.Visible;
        }
        private void ETg_Btn9_Unchecked(object sender, RoutedEventArgs e)
        {
            LabelX.Visibility = LabelY.Visibility = LabelW.Visibility = LabelH.Visibility =
           Text_X.Visibility = Text_Y.Visibility = Text_Width.Visibility = Text_Height.Visibility = Eratio1.Visibility =
               Eratio2.Visibility = Eratio3.Visibility = ButtonOK.Visibility = Visibility.Collapsed;
        }
        private void ERotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateTransform rt1 = new RotateTransform();
            switch (rotateRightCounter)
            {
                case 1:
                    rt1.Angle = 270;
                    rotateRightCounter = 2;
                    rotateLeftCounter = 4;
                    break;
                case 2:
                    rt1.Angle = 180;
                    rotateRightCounter = 3;
                    rotateLeftCounter = 3;
                    break;
                case 3:
                    rt1.Angle = 90;
                    rotateRightCounter = 4;
                    rotateLeftCounter = 2;
                    break;
                case 4:
                    rt1.Angle = 0;
                    rotateRightCounter = 1;
                    rotateLeftCounter = 1;
                    break;
            }

            EditFramePic.LayoutTransform = rt1;

            //PhotoUtils.WaitForFile(pathUpdate.PathImg);
            ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(pathUpdate.PathImg, BitmapLoader.LargeThumbSize, (int)rt1.Angle);
        }
        private void ERotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateTransform rt1 = new RotateTransform();
            switch (rotateLeftCounter)
            {
                case 1:
                    rt1.Angle = 90;
                    rotateLeftCounter = 2;
                    rotateRightCounter = 4;
                    break;
                case 2:
                    rt1.Angle = 180;
                    rotateLeftCounter = 3;
                    rotateRightCounter = 3;
                    break;
                case 3:
                    rt1.Angle = 270;
                    rotateLeftCounter = 4;
                    rotateRightCounter = 2;
                    break;
                case 4:
                    rt1.Angle = 0;
                    rotateLeftCounter = 1;
                    rotateRightCounter = 1;
                    break;
            }

            EditFramePic.LayoutTransform = rt1;
            ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(pathUpdate.PathImg, BitmapLoader.LargeThumbSize, (int)rt1.Angle);

            //double x = EditFramePic.ActualWidth / EditFramePic.ActualHeight;
            //double temp = EditFramePic.ActualWidth;
            //EditFramePic.Width = EditFramePic.ActualHeight;
            //EditFramePic.Height = temp / x;
        }
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage EditPicImg = new BitmapImage();
            EditPicImg.BeginInit();
            EditPicImg.UriSource = new Uri(EditFramePic.Source.ToString());
            EditPicImg.EndInit();
            EditFramePic.Source = new CroppedBitmap(EditPicImg, new Int32Rect((int)double.Parse(Text_X.Text), (int)double.Parse(Text_Y.Text), (int)double.Parse(Text_Width.Text), (int)double.Parse(Text_Height.Text)));
            //   new Int32Rect((int)CropOut.RadiusX, (int)CropOut.RadiusY, (int)CropOut.ActualWidth, (int)CropOut.ActualHeight));
            CropOut.Visibility = Visibility.Collapsed;
        }
        private void Text_X_TextChanged(object sender, TextChangedEventArgs e)
        {
            // CropOut.RadiusX = double.Parse(Text_X.Text);
        }
        private void Text_Y_TextChanged(object sender, TextChangedEventArgs e)
        {
            //   CropOut.RadiusY = double.Parse(Text_Y.Text);
        }
        private void Text_Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  CropOut.Width = double.Parse(Text_Width.Text);
        }
        private void Text_Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            //CropOut.Height = double.Parse(Text_Height.Text);
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup exp = new ExportPopup();
            exp.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            exp.ShowDialog();
        }

        private void tabCaptureOptions_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void btnCapture360_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCaptureVideo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void tbItm360_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Panel.SetZIndex(btnCapture360, 1);
            //Panel.SetZIndex(btnCaptureSingle, 0);
            //Panel.SetZIndex(btnCaptureVideo, 0);
        }

        private void tbItmSingle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Panel.SetZIndex(btnCapture360, 0);
            //Panel.SetZIndex(btnCaptureSingle, 1);
            //Panel.SetZIndex(btnCaptureVideo, 0);
        }

        private void tbItmVideo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Panel.SetZIndex(btnCapture360, 0);
            //Panel.SetZIndex(btnCaptureSingle, 0);
            //Panel.SetZIndex(btnCaptureVideo, 1);
        }
        //private void shutterButton_Click(object sender, RoutedEventArgs e)
        //{
        //    pathUpdate.CapturePhotoVM.CaptureImage();
        //}

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                if (StaticClass.CapturedImageCount == 0) { tab_video.IsEnabled = true; tab_Single.IsEnabled = true; }
                if (button3 == null) { return; }

                button3.Visibility = Visibility.Collapsed;
                btn_liveview.Visibility = Visibility.Collapsed;

                grid_360SingleMode.Visibility = Visibility.Collapsed;
                grid_VedioMode.Visibility = Visibility.Visible;

                bool __showstatus = true;
                if (tab_360.IsSelected || tab_Single.IsSelected)
                {
                    button3.Visibility = Visibility.Visible;
                    //__showstatus = false;
                }
                if (tab_video.IsSelected)
                {
                    btn_liveview.Visibility = Visibility.Visible;
                    //__showstatus = true;
                }
                Dispatcher.Invoke(new Action(delegate { LVViewModel.lvInstance().ShowLiveImagePart(__showstatus); }));
            }
            catch (Exception ex)
            {
            }
        }

        private void btn_liveview_Click(object sender, RoutedEventArgs e)
        {
            if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy && ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
            {
                Dispatcher.Invoke(new Action(delegate { LVViewModel.lvInstance().RecordLiveView(); }));
            }

        }

        private void Keys_Shortcuts(string _keyvalue)
        {
            if (_keyvalue == "Alt+V")
            {
                if ((bool)Tg_Btn6.IsChecked)
                {
                    V_CenterLineApply(true);
                    Tg_Btn6.IsChecked = false;
                }
                else
                {
                    V_CenterLineApply(false);
                    Tg_Btn6.IsChecked = true;
                }
            }
            if (_keyvalue == "Alt+H")
            {
                if ((bool)Tg_Btn7.IsChecked)
                {
                    H_CenterLineApply(true);
                    Tg_Btn7.IsChecked = false;
                }
                else
                {
                    H_CenterLineApply(false);
                    Tg_Btn7.IsChecked = true;
                }
            }

            if (_keyvalue == "Ctrl+E" || _keyvalue == "Ctrl+Shift+E")
            {
                if (EditTab.IsSelected && ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null)
                {
                    RoutedEventArgs routedEventArgs = new RoutedEventArgs(ButtonBase.ClickEvent, ExportButton);
                    ExportButton.RaiseEvent(routedEventArgs);
                }
            }

            if (_keyvalue == "Ctrl+1")
            {
                if (__lvControler == null) { __lvControler = new LVControler(); }
                __lvControler.ExecuteCommand(CmdConsts.LiveView_Zoom_All);
            }
            if (_keyvalue == "Ctrl+2")
            {
                if (__lvControler == null) { __lvControler = new LVControler(); }
                __lvControler.ExecuteCommand(CmdConsts.LiveView_Zoom_5x);
            }
            if (_keyvalue == "Ctrl+3")
            {
                if (__lvControler == null) { __lvControler = new LVControler(); }
                __lvControler.ExecuteCommand(CmdConsts.LiveView_Zoom_10x);
            }

            if (_keyvalue == "Ctrl++") { ZoomInOut_EditMode(__ZoomValue + 10); }
            if (_keyvalue == "Ctrl+-") { ZoomInOut_EditMode(__ZoomValue - 10); }
        }

        //protected void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    e.Handled = true;
        //    e.Handled = true;
        //    Point curContentMousePoint = e.GetPosition(EditFramePicEdit);
        //    if (e.Delta > 0)
        //    {
        //        zoomAndPanControl.ZoomIn(curContentMousePoint);
        //    }
        //    else if (e.Delta < 0)
        //    {
        //        // don't allow zoomout les that original image 
        //        if (zoomAndPanControl.ContentScale - 0.2 > zoomAndPanControl.FitScale())
        //        {
        //            zoomAndPanControl.ZoomOut(curContentMousePoint);
        //        }
        //        else
        //        {
        //            zoomAndPanControl.ScaleToFit();
        //        }
        //    }
        //}
        //private void zoomAndPanControl_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    zoomAndPanControl.ScaleToFit();
        //}
        //private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    Point curContentMousePoint = e.GetPosition(EditFramePicEdit);
        //    if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
        //    {
        //        zoomAndPanControl.ZoomAboutPoint(4, curContentMousePoint);
        //    }
        //    else
        //    {
        //        zoomAndPanControl.ScaleToFit();
        //    }
        //}


        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                //if (LVViewModel.lvInstance() == null) { return; }
                //if (LVViewModel.lvInstance().CameraDevice == null) { return; }
                //if (LVViewModel.lvInstance().CameraDevice.LiveViewImageZoomRatio == null) { return; }
                ////Dispatcher.Invoke(new Action(delegate { __lvControler.ZoomScroller(e.NewValue); }));
                //if (e.NewValue > e.OldValue)
                //{ 
                //    LVViewModel.lvInstance().CameraDevice.LiveViewImageZoomRatio.NextValue();
                //}
                //else
                //{
                //    LVViewModel.lvInstance().CameraDevice.LiveViewImageZoomRatio.PrevValue();
                //}
            }
            catch (Exception ex) { }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LVViewModel.lvInstance() == null) { return; }
                Dispatcher.Invoke(new Action(delegate { LVViewModel.lvInstance().ToggleZoom(); }));
            }
            catch (Exception ex) { }
        }

        private double __ZoomValue = 100;
        private void ZoomInOut_EditMode(double _value)
        {
            //double contentScale = _value / 100;
            //Point curContentMousePoint = __EditImage_Position; //e.GetPosition(EditFramePicEdit);
            //if (_value <= 100) { zoomAndPanControl.ScaleToFit(); __ZoomValue = 100; }
            //else if (_value > 500) { return; }
            //else { zoomAndPanControl.ZoomAboutPoint(contentScale, curContentMousePoint); __ZoomValue = _value; }
        }

        private Point __EditImage_Position = new Point(0, 0);
        private void zoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            __EditImage_Position = e.GetPosition(EditFramePicEdit);
        }

    }
}

