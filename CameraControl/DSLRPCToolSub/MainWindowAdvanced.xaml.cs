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
using System.Timers;
using CameraControl.Core.Database;
using CameraControl.Core.TclScripting;
using CameraControl.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using DSLR_Tool_PC.ViewModels;
using DSLR_Tool_PC;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using ImageMagick;
using System;
using FileInfo = System.IO.FileInfo;
using System.Threading.Tasks;
using System.Drawing;

namespace CameraControl
{
    /// <summary>
    /// Interaction logic for MainWindowAdvanced.xaml
    /// </summary>
    public partial class MainWindowAdvanced : IMainWindowPlugin, INotifyPropertyChanged
    {
        public string OGFolder = null;
        public string DisplayName { get; set; }
        private int _ListBoxSelectedIndex = -1;

        private object _locker = new object();
        private FileItem _selectedItem = null;
        private Timer _selectiontimer = new Timer(4000);
        private DateTime _lastLoadTime = DateTime.Now;
        private string selectedpath = string.Empty;

        private bool _sortCameraOreder = true;

        public RelayCommand<AutoExportPluginConfig> ConfigurePluginCommand { get; set; }
        public RelayCommand<IAutoExportPlugin> AddPluginCommand { get; set; }
        public RelayCommand<CameraPreset> SelectPresetCommand { get; private set; }
        public RelayCommand<CameraPreset> DeletePresetCommand { get; private set; }
        public RelayCommand<CameraPreset> LoadInAllPresetCommand { get; private set; }
        public RelayCommand<CameraPreset> VerifyPresetCommand { get; private set; }

        //Added Code
        PathUpdate __Pathupdate = PathUpdate.getInstance();
        PhotoEditModel __photoEditModel = PhotoEditModel.GetInstance();
        ExportZipModel __exportZipModel = ExportZipModel.getInstance();
        ExportMP4ViewModel __exportMP4ViewModel = ExportMP4ViewModel.getInstance();
        LVControler __lvControler = null;
        //public double CameraGrid_width;
        //public double CameraGrid_height;
        public double EditGrid_width;
        public double EditGrid_height;
        public int rotateLeftCounter = 1;
        public int rotateRightCounter = 1;
        public PhotoSession Session { get; set; }
        ///////////////////////////////////////////

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowAdvanced" /> class.
        /// </summary>
        public MainWindowAdvanced()
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
            this.EditLevelGraph.DataContext = __Pathupdate.EditLevelGraphVM;
            this.CaptureLevelGraph.DataContext = __Pathupdate.CaptureLevelGraphVM;

            if (!string.IsNullOrEmpty(ServiceProvider.Branding.ApplicationTitle))
            {
                Title = ServiceProvider.Branding.ApplicationTitle;
            }
            if (!string.IsNullOrEmpty(ServiceProvider.Branding.LogoImage) && File.Exists(ServiceProvider.Branding.LogoImage))
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

            //zoomAndPanControl.ScaleToFit();

            //try
            //{
            //    string searchQuery = "og_" + "*";
            //    string folderName = Path.GetTempPath();

            //    var directory = new DirectoryInfo(folderName);
            //    var directories = directory.GetDirectories(searchQuery, SearchOption.AllDirectories);

            //    foreach (var d in directories)
            //    {
            //        d.Delete(true);
            //    }
            //}
            //catch (Exception ex) { Log.Debug("", ex); }

            BrowseHistoryImages("");
            SortCameras();

            __lvControler = new LVControler();
            __photoEditModel.ExecuteInti(this);
            __exportZipModel.ExecuteInti(this);
            __exportMP4ViewModel.ExecuteInti(this);

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.All_Close);
            App.Current.Shutdown();
            Application.Current.Shutdown();
            LVViewModel.lvInstance().UnInit();
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
        private static double __EditPicGrid_ActualWidth = 0.0;
        private static double __EditPicGrid_ActualHeight = 0.0;
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            __EditPicGrid_ActualWidth = EditPicGrid.ActualWidth;
            __EditPicGrid_ActualHeight = EditPicGrid.ActualHeight;
            __CameraGrid_ActualWidth = CameraGrid.ActualWidth;
            __CameraGrid_ActualHeight = CameraGrid.ActualHeight;
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.RefreshDisplay);
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
                case WindowsCmdConsts.Next_Image:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            if (ListBoxSnapshots.SelectedIndex < ListBoxSnapshots.Items.Count - 1)
                            {
                                ImageDetails item = ListBoxSnapshots.SelectedItem as ImageDetails;
                                if (item != null)
                                {
                                    int ind = ListBoxSnapshots.Items.IndexOf(item);
                                    ListBoxSnapshots.SelectedIndex = ind + 1;
                                    ListBoxSnapshots.ScrollIntoView(ListBoxSnapshots.SelectedItem);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Debug("Unable to fetch Next Image", e);
                        }
                    }));
                    break;
                case WindowsCmdConsts.Prev_Image:
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            if (ListBoxSnapshots.SelectedIndex > 0)
                            {
                                ImageDetails item = ListBoxSnapshots.SelectedItem as ImageDetails;
                                if (item != null)
                                {
                                    int ind = ListBoxSnapshots.Items.IndexOf(item);
                                    ListBoxSnapshots.SelectedIndex = ind - 1;
                                    ListBoxSnapshots.ScrollIntoView(ListBoxSnapshots.SelectedItem);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Debug("Unable to fetch Prev Image", e);
                        }
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
            catch (Exception) { }
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
                SortCameras();
                if (!ServiceProvider.Settings.HideTrayNotifications)
                {
                    MyNotifyIcon.HideBalloonTip();
                    MyNotifyIcon.ShowBalloonTip("Camera connected", cameraDevice.LoadProperties().DeviceName, BalloonIcon.Info);
                    controler1.Visibility = controler2.Visibility = LVCSimple1.Visibility = Visibility.Visible;
                    _NoImage.Visibility = Visibility.Hidden;
                    myLVControler.Visibility = Visibility.Visible;
                    //Pnl_ToggleButton.IsEnabled = true;
                    //grd_ToggleButton.IsEnabled = true;
                    tab_video.IsSelected = true;
                    tab_360.IsSelected = true;

                    LVViewModel.lvInstance().InitCommands();
                    if (ServiceProvider.DeviceManager.ConnectedDevices.Count > 0) { ServiceProvider.DeviceManager.SelectedCameraDevice = ServiceProvider.DeviceManager.ConnectedDevices[0]; }

                    if (__lvControler == null) { __lvControler = new LVControler(); } else { __lvControler = null; __lvControler = new LVControler(); }
                    __lvControler.ExecuteCommand("LiveViewWnd_Show");
                }
            }));
        }

        private void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            Dispatcher.BeginInvoke(
                new Action(
                    delegate
                    {
                        Title = (ServiceProvider.Branding.ApplicationTitle ?? "OrangeMonkie") + " - " + (newcameraDevice == null ? "" : newcameraDevice.DisplayName);
                        controler1.Visibility = controler2.Visibility = LVCSimple1.Visibility = Visibility.Visible;
                        _NoImage.Visibility = Visibility.Hidden;
                        myLVControler.Visibility = Visibility.Visible;
                        //Pnl_ToggleButton.IsEnabled = true;
                        //grd_ToggleButton.IsEnabled = true;
                        tab_video.IsSelected = true;
                        tab_360.IsSelected = true;

                        if (__lvControler == null) { __lvControler = new LVControler(); } else { __lvControler = null; __lvControler = new LVControler(); }
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
                    controler1.Visibility = controler2.Visibility = LVCSimple1.Visibility = Visibility.Hidden;
                    StaticClass.CapturedImageCount = 0; tab_video.IsEnabled = true; tab_Single.IsEnabled = true;
                    _NoImage.Visibility = Visibility.Visible;
                    myLVControler.Visibility = Visibility.Hidden;
                    //Pnl_ToggleButton.IsEnabled = false;
                    //grd_ToggleButton.IsEnabled = false;

                    LVViewModel.lvInstance().UnInit();
                    __Pathupdate.PathCaptureImg = "";
                    //__Pathupdate.PathImg = "";
                }
            }));
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
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
                            free_Timer();
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
                if (PhotoUtils.IsMovie(eventArgs.FileName))
                {
                    StaticHelper.Instance.SystemMessage = "Video Transfer start...";
                }
                else { StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferBegin; }

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
                    fileName = session.GetNextFileName(eventArgs.FileName, eventArgs.CameraDevice, tempFile);
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
                    fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + Path.GetExtension(fileName).ToLower());
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

                if (!Directory.Exists(Path.GetDirectoryName(fileName))) { Directory.CreateDirectory(Path.GetDirectoryName(fileName)); }

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

                if (LVViewModel.lvInstance().ZoomSliderValue > 0)
                {
                    int _cRatio = LVViewModel.lvInstance().ZoomSliderValue;
                    using (MagickImage image = new MagickImage(tempFile))
                    {
                        int _X = image.Width / 2 * _cRatio / 100;
                        int _Y = image.Height / 2 * _cRatio / 100;

                        int _Width = image.Width - (_X * 2);
                        int _Height = image.Height - (_Y * 2);

                        MagickGeometry geometry = new MagickGeometry();
                        geometry.Width = _Width;
                        geometry.Height = _Height;
                        geometry.X = _X;
                        geometry.Y = _Y;
                        image.Crop(geometry);
                        //image.Format = MagickFormat.Jpeg;
                        image.Write(tempFile);
                    }
                }

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


                //if (ServiceProvider.Settings.MinimizeToTrayIcon && !IsVisible && !ServiceProvider.Settings.HideTrayNotifications)
                //{
                //    MyNotifyIcon.HideBalloonTip();
                //    MyNotifyIcon.ShowBalloonTip("Photo transfered", fileName, BalloonIcon.Info);
                //}

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
                if (PhotoUtils.IsMovie(fileName))
                {
                    StaticHelper.Instance.SystemMessage = "Video transfer done" + " " + strTransfer;
                }
                else
                {
                    StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferDone + " " + strTransfer;
                }

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
            //if (StackLayout.Children.Count > 0)
            //{
            //    var cnt = StackLayout.Children[0] as LayoutBase;
            //    if (cnt != null)
            //        cnt.UnInit();
            //}
            //switch (type)
            //{
            //    case LayoutTypeEnum.Normal:
            //        {
            //            StackLayout.Children.Clear();
            //            LayoutNormal control = new LayoutNormal();
            //            StackLayout.Children.Add(control);
            //        }
            //        break;
            //    case LayoutTypeEnum.Grid:
            //        {
            //            StackLayout.Children.Clear();
            //            LayoutGrid control = new LayoutGrid();
            //            StackLayout.Children.Add(control);
            //        }
            //        break;
            //    case LayoutTypeEnum.GridRight:
            //        {
            //            StackLayout.Children.Clear();
            //            LayoutGridRight control = new LayoutGridRight();
            //            StackLayout.Children.Add(control);
            //        }
            //        break;
            //    case LayoutTypeEnum.GridBottom:
            //        {
            //            StackLayout.Children.Clear();
            //            LayoutGridBottom control = new LayoutGridBottom();
            //            StackLayout.Children.Add(control);
            //        }
            //        break;
            //}
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
            if (ServiceProvider.DeviceManager.ConnectedDevices.Count > 0) { ServiceProvider.DeviceManager.SelectedCameraDevice = ServiceProvider.DeviceManager.ConnectedDevices[0]; }
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
                        ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.BulbWnd_Show, ServiceProvider.DeviceManager.SelectedCameraDevice);
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
            ETg_Btn7.IsChecked = false;
            Tg_Btn7.IsChecked = false;
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
                if (CaptureTab.IsSelected)
                {
                    CV_Line1.Y2 = CameraGrid.ActualHeight;
                    CV_Line1.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (EditTab.IsSelected)
                {
                    EV_Line1.Visibility = Visibility.Collapsed;
                }
                if (CaptureTab.IsSelected)
                {
                    CV_Line1.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Tg_Btn7_Checked(object sender, RoutedEventArgs e)
        {
            ETg_Btn6.IsChecked = false;
            Tg_Btn6.IsChecked = false;
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
                if (CaptureTab.IsSelected)
                {
                    CH_Line1.X2 = CameraGrid.ActualWidth;
                    CH_Line1.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (EditTab.IsSelected)
                {
                    EH_Line1.Visibility = Visibility.Collapsed;
                }
                if (CaptureTab.IsSelected)
                {
                    CH_Line1.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void grid2_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = EditPicGrid.ActualWidth;
                double height = EditPicGrid.ActualHeight;
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

                EVG_Line1.Visibility = EVG_Line2.Visibility = EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Visible;
            }
            if (CaptureTab.IsSelected)
            {
                double width = CameraGrid.ActualWidth;
                double height = CameraGrid.ActualHeight;

                GridLines_Capture(1, width, height);
            }
        }
        private void grid2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = EVG_Line2.Visibility = EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Collapsed;
            }
            if (CaptureTab.IsSelected)
            {
                CVG_Line1.Visibility = CVG_Line2.Visibility = CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Collapsed;
            }
        }
        private void grid3_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = EditPicGrid.ActualWidth;
                double height = EditPicGrid.ActualHeight;
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

            if (CaptureTab.IsSelected)
            {
                double width = CameraGrid.ActualWidth;
                double height = CameraGrid.ActualHeight;

                GridLines_Capture(2, width, height);
            }
        }
        private void grid3_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = EVG_Line5.Visibility = Visibility.Collapsed;
                EHG_Line1.Visibility = EHG_Line2.Visibility = EHG_Line3.Visibility = Visibility.Collapsed;
            }
            if (CaptureTab.IsSelected)
            {
                CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = CVG_Line5.Visibility = Visibility.Collapsed;
                CHG_Line1.Visibility = CHG_Line2.Visibility = CHG_Line3.Visibility = Visibility.Collapsed;
            }
        }
        private void grid4_Checked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                double width = EditPicGrid.ActualWidth;
                double height = EditPicGrid.ActualHeight;
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

                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = Visibility.Visible;
                EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Visible;
            }
            if (CaptureTab.IsSelected)
            {
                double width = CameraGrid.ActualWidth;
                double height = CameraGrid.ActualHeight;

                GridLines_Capture(3, width, height);
            }
        }
        private void grid4_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EditTab.IsSelected)
            {
                EVG_Line1.Visibility = EVG_Line2.Visibility = EVG_Line3.Visibility = EVG_Line4.Visibility = Visibility.Collapsed;
                EHG_Line1.Visibility = EHG_Line2.Visibility = Visibility.Collapsed;
            }
            if (CaptureTab.IsSelected)
            {
                CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = Visibility.Collapsed;
                CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Collapsed;
            }
        }

        private void GridLines_Capture(int _From, double width, double height)
        {
            //double width = CameraGrid.ActualWidth;
            //double height = CameraGrid.ActualHeight;

            //if (_From == 0)
            //{
            //    CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = Visibility.Collapsed;
            //    CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Collapsed;

            //    CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = CVG_Line5.Visibility = Visibility.Collapsed;
            //    CHG_Line1.Visibility = CHG_Line2.Visibility = CHG_Line3.Visibility = Visibility.Collapsed;

            //    CVG_Line1.Visibility = CVG_Line2.Visibility = CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Collapsed;
            //}
            if (width == 0 || height == 0) { return; }

            if (_From == 3)
            {
                CVG_Line1.X1 = width / 3;
                CVG_Line1.X2 = width / 3;
                CVG_Line1.Y2 = height;
                CVG_Line1.Y1 = 0;
                CVG_Line2.X1 = (2 * width) / 3;
                CVG_Line2.X2 = (2 * width) / 3;
                CVG_Line2.Y1 = 0;
                CVG_Line2.Y2 = height;
                CHG_Line1.X1 = 0;
                CHG_Line1.Y1 = height / 3;
                CHG_Line1.X2 = width;
                CHG_Line1.Y2 = height / 3;
                CHG_Line2.X1 = 0;
                CHG_Line2.Y1 = (2 * height) / 3;
                CHG_Line2.X2 = width;
                CHG_Line2.Y2 = (2 * height) / 3;
                ///Diagonals
                CVG_Line3.X1 = 0;
                CVG_Line3.X2 = width;
                CVG_Line3.Y2 = height;
                CVG_Line4.X1 = width;
                CVG_Line4.X2 = 0;
                CVG_Line4.Y1 = 0;
                CVG_Line4.Y2 = height;

                CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = Visibility.Visible;
                CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Visible;
            }
            if (_From == 2)
            {

                CVG_Line1.X1 = width / 6;
                CVG_Line1.X2 = width / 6;
                CVG_Line1.Y2 = height;
                CVG_Line1.Y1 = 0;
                CVG_Line2.X1 = width / 3;
                CVG_Line2.X2 = width / 3;
                CVG_Line2.Y2 = height;
                CVG_Line2.Y1 = 0;
                CVG_Line3.X1 = width / 2;
                CVG_Line3.X2 = width / 2;
                CVG_Line3.Y2 = height;
                CVG_Line3.Y1 = 0;
                CVG_Line4.X1 = (2 * width) / 3;
                CVG_Line4.X2 = (2 * width) / 3;
                CVG_Line4.Y2 = height;
                CVG_Line4.Y1 = 0;
                CVG_Line5.X1 = (5 * width) / 6;
                CVG_Line5.X2 = (5 * width) / 6;
                CVG_Line5.Y2 = height;
                CVG_Line5.Y1 = 0;
                CHG_Line1.X1 = 0;
                CHG_Line1.Y1 = height / 4;
                CHG_Line1.X2 = width;
                CHG_Line1.Y2 = height / 4;
                CHG_Line2.X1 = 0;
                CHG_Line2.Y1 = height / 2;
                CHG_Line2.X2 = width;
                CHG_Line2.Y2 = height / 2;
                CHG_Line3.X1 = 0;
                CHG_Line3.Y1 = (3 * height) / 4;
                CHG_Line3.X2 = width;
                CHG_Line3.Y2 = (3 * height) / 4;

                CVG_Line1.Visibility = CVG_Line2.Visibility = CVG_Line3.Visibility = CVG_Line4.Visibility = CVG_Line5.Visibility = Visibility.Visible;
                CHG_Line1.Visibility = CHG_Line2.Visibility = CHG_Line3.Visibility = Visibility.Visible;
            }

            if (_From == 1)
            {

                CVG_Line1.X1 = width / 3;
                CVG_Line1.X2 = width / 3;
                CVG_Line1.Y2 = height;
                CVG_Line1.Y1 = 0;
                CVG_Line2.X1 = (2 * width) / 3;
                CVG_Line2.X2 = (2 * width) / 3;
                CVG_Line2.Y1 = 0;
                CVG_Line2.Y2 = height;
                CHG_Line1.X1 = 0;
                CHG_Line1.Y1 = height / 3;
                CHG_Line1.X2 = width;
                CHG_Line1.Y2 = height / 3;
                CHG_Line2.X1 = 0;
                CHG_Line2.Y1 = (2 * height) / 3;
                CHG_Line2.X2 = width;
                CHG_Line2.Y2 = (2 * height) / 3;

                CVG_Line1.Visibility = CVG_Line2.Visibility = CHG_Line1.Visibility = CHG_Line2.Visibility = Visibility.Visible;
            }
        }
        private void GridRatio_Capture(bool _Status, double _Width, double _Height)
        {
            if (_Status == true)
            {
                StaticClass.__Ratio_Diff_Width = (int)(__CameraGrid_ActualWidth - _Width);
                StaticClass.__Ratio_Diff_Height = (int)(__CameraGrid_ActualHeight - _Height);
                //Thread.Sleep(500);
            }
            else
            {
                StaticClass.__Ratio_Diff_Width = 0;
                StaticClass.__Ratio_Diff_Height = 0;
                //Thread.Sleep(500);
            }
        }

        private void ETg_Btn8_Checked(object sender, RoutedEventArgs e)
        {
            if (__Pathupdate.PathImg == null || __Pathupdate.PathImg == "") { return; }
            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage == null) { return; }

            try
            {
                Overlay1.Visibility = Overlay2.Visibility = Overlay4.Visibility = Overlay5.Visibility = Visibility.Collapsed;
                int ind = 0;
                if (tab_history.IsSelected)
                {
                    ind = ImageLIstBox.Items.IndexOf(__SelectedImage);
                    if (ind >= 2) { Overlay1.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_History[ind - 2].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay1.Visibility = Visibility.Visible; }
                    if (ind >= 1) { Overlay2.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_History[ind - 1].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay2.Visibility = Visibility.Visible; }
                    if (ind <= images_History.Count - 2) { Overlay4.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_History[ind + 1].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay4.Visibility = Visibility.Visible; }
                    if (ind <= images_History.Count - 3) { Overlay5.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_History[ind + 2].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay5.Visibility = Visibility.Visible; }
                }
                else
                {
                    ind = ImageListBox_Folder.Items.IndexOf(__SelectedImage);
                    if (ind >= 2) { Overlay1.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_Folder[ind - 2].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay1.Visibility = Visibility.Visible; }
                    if (ind >= 1) { Overlay2.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_Folder[ind - 1].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay2.Visibility = Visibility.Visible; }
                    if (ind <= images_History.Count - 2) { Overlay4.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_Folder[ind + 1].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay4.Visibility = Visibility.Visible; }
                    if (ind <= images_History.Count - 3) { Overlay5.Source = (WriteableBitmap)BitmapLoader.Instance.LoadImage(images_Folder[ind + 2].Path_Orginal, BitmapLoader.LargeThumbSize, 0); Overlay5.Visibility = Visibility.Visible; }
                }
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }
        private void ETg_Btn8_Unchecked(object sender, RoutedEventArgs e)
        {
            Overlay1.Visibility = Overlay2.Visibility = Overlay4.Visibility = Overlay5.Visibility = Visibility.Collapsed;
        }
        #region Rotate Image
        private void ERotateLeft_Click(object sender, RoutedEventArgs e)
        {
            if (__Pathupdate.PathImg == null || __Pathupdate.PathImg == "") { return; }
            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage == null) { return; }

            try
            {
                //RotateTransform rt1 = new RotateTransform();
                //switch (rotateRightCounter)
                //{
                //    case 1:
                //        rt1.Angle = 270;
                //        rotateRightCounter = 2;
                //        rotateLeftCounter = 4;
                //        //b.RotateFlip(RotateFlipType.Rotate90FlipXY);
                //        break;
                //    case 2:
                //        rt1.Angle = 180;
                //        rotateRightCounter = 3;
                //        rotateLeftCounter = 3;
                //        break;
                //    case 3:
                //        rt1.Angle = 90;
                //        rotateRightCounter = 4;
                //        rotateLeftCounter = 2;
                //        break;
                //    case 4:
                //        rt1.Angle = 0;
                //        rotateRightCounter = 1;
                //        rotateLeftCounter = 1;
                //        break;
                //}

                //EditFramePic.LayoutTransform = rt1;
                //ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(__Pathupdate.PathImg, BitmapLoader.LargeThumbSize, (int)rt1.Angle);

                ImageDetails _imageDetails = __Pathupdate.__SelectedImageDetails;
                Bitmap b = new Bitmap(_imageDetails.Path);
                string sourceDir = Path.GetDirectoryName(_imageDetails.Path).ToString();
                string exFilename = _imageDetails.FileName.ToString();
                if (_imageDetails == null) { return; }
                string xName = (Path.Combine(Settings.ApplicationTempFolder + "\\" + _imageDetails.FileName.ToString()));
                b.RotateFlip(RotateFlipType.Rotate270FlipNone);
                xName = StaticClass.saveBitmap2File(b, xName);
                //if (File.Exists(xName)) { File.Delete(xName); }
                string newSource = RecreateFiles(sourceDir, exFilename);
                BrowseFolderImages(newSource, _imageDetails.FileName.ToString());

            }
            catch (Exception ex) { Log.Debug("ERotateLeft_Click", ex); }
        }
        private void ERotateRight_Click(object sender, RoutedEventArgs e)
            {
            if (__Pathupdate.PathImg == null || __Pathupdate.PathImg == "") { return; }
            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage == null) { return; }
           
            try
            {
                //RotateTransform rt1 = new RotateTransform();
                //switch (rotateLeftCounter)
                //{
                //    case 1:
                //        rt1.Angle = 90;
                //        rotateLeftCounter = 2;
                //        rotateRightCounter = 4;
                //        b.RotateFlip(RotateFlipType.Rotate270FlipXY);
                //        break;
                //    case 2:
                //        rt1.Angle = 180;
                //        rotateLeftCounter = 3;
                //        rotateRightCounter = 3;
                //        b.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                //        break;
                //    case 3:
                //        rt1.Angle = 270;
                //        rotateLeftCounter = 4;
                //        rotateRightCounter = 2;
                //        b.RotateFlip(RotateFlipType.Rotate90FlipXY);
                //        break;
                //    case 4:
                //        rt1.Angle = 0;
                //        rotateLeftCounter = 1;
                //        rotateRightCounter = 1;
                //        b.RotateFlip(RotateFlipType.Rotate180FlipXY);
                //        break;
                //}

                //EditFramePic.LayoutTransform = rt1;

                //ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(__Pathupdate.PathImg, BitmapLoader.LargeThumbSize, (int)rt1.Angle);
                //UpdateImageData();

                ImageDetails _imageDetails = __Pathupdate.__SelectedImageDetails;
                Bitmap b = new Bitmap(_imageDetails.Path);
                string sourceDir = Path.GetDirectoryName(_imageDetails.Path).ToString();
                string exFilename = _imageDetails.FileName.ToString();
                if (_imageDetails == null) { return; }
                string xName = (Path.Combine(Settings.ApplicationTempFolder +"\\"+ _imageDetails.FileName.ToString()));
                b.RotateFlip(RotateFlipType.Rotate90FlipNone);
                xName = StaticClass.saveBitmap2File(b, xName);
                //if (File.Exists(xName)) { File.Delete(xName); }
                string newSource = RecreateFiles(sourceDir, exFilename);
                BrowseFolderImages(newSource,  _imageDetails.FileName.ToString());
            }
            catch (Exception ex) { Log.Debug("ERotateRight_Click", ex); }

        }
        #endregion

        #region Crop Image
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ETg_Btn6.IsChecked = false;
                ETg_Btn7.IsChecked = false;
                Egrid1.IsChecked = true;
                CropOut.Visibility = Visibility.Collapsed;
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)__EditPicGrid_ActualWidth, (int)__EditPicGrid_ActualWidth, 96d, 96d, System.Windows.Media.PixelFormats.Default);
                rtb.Render(EditPicGrid);

                var _x = (int)double.Parse(Text_X.Text);
                var _y = (int)double.Parse(Text_Y.Text);
                var _w = (int)double.Parse(Text_Width.Text);
                var _h = (int)double.Parse(Text_Height.Text);

                if ((_y + _h) > __EditPicGrid_ActualHeight)
                {
                    _h = (int)(__EditPicGrid_ActualHeight - _y);
                    if (_h < 0) { _h = 0; }
                }
                if ((_x + _w) > __EditPicGrid_ActualWidth)
                {
                    _w = (int)(__EditPicGrid_ActualWidth - _x);
                    if (_x < 0) { _x = 0; }
                }
                var cImage = new CroppedBitmap(rtb, new Int32Rect(_x, _y, _w, _h));
                //EditFramePic.Source = cImage;

                var filename = Path.Combine(Settings.ApplicationTempFolder, Path.GetFileNameWithoutExtension(__Pathupdate.PathImg) + Path.GetExtension(__Pathupdate.PathImg));
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cImage));
                System.IO.FileStream fs = File.Create(filename);
                using (fs)
                {
                    encoder.Save(fs);
                }
                
                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(filename, BitmapLoader.LargeThumbSize, 0);
                Bitmap b = new Bitmap(filename);
                ImageDetails _imageDetails = __Pathupdate.__SelectedImageDetails;
                string sourceDir=Path.GetDirectoryName(_imageDetails.Path).ToString();
                string exFilename = _imageDetails.FileName.ToString();
                if (_imageDetails == null) { return; }
                string xName = (Path.Combine(Settings.ApplicationTempFolder,"\\" + _imageDetails.FileName.ToString()));
                ImageDetails img = new ImageDetails();
                img.Path=StaticClass.saveBitmap2File(b, xName);
                b.Dispose();


                //if (File.Exists(filename)) { File.Delete(filename); }
                string newSource=RecreateFiles(sourceDir, exFilename);
                BrowseFolderImages(newSource, exFilename);

            }
            catch (Exception ex) { Log.Debug("ButtonOK_Click", ex); }
            CropOut.Visibility = Visibility.Collapsed;
            ETg_Btn9.IsChecked = false;
        }
        #endregion

        public string RecreateFiles(string source,string exFile)
        {
            string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
            if (!Directory.Exists(tempfolder))
                Directory.CreateDirectory(tempfolder);
            string[] filePaths = Directory.GetFiles(source);
            foreach (var filename in filePaths)
            {
                int a = filename.LastIndexOf("\\")+1;
                int b = (filename.Length)-a;
                string fl = filename.Substring(a,b);
                if (fl == exFile) {
                    string file = Settings.ApplicationTempFolder +"\\"+ fl;

                    //Do your job with "file"  
                    string str = tempfolder.ToString() + "\\" + fl;
                    if (!File.Exists(str))
                    {
                        File.Copy(file, str);
                    }
                } 
                else
                {
                    string file = filename.ToString();

                    //Do your job with "file"  
                    string str = tempfolder.ToString() + "\\"+fl;
                    if (!File.Exists(str))
                    {
                        File.Copy(file, str);
                    }
                }
            }
            return tempfolder;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (__Pathupdate.PathImg == null || __Pathupdate.PathImg == "") { return; }
            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage == null) { return; }

            try
            {
                ExportPopup exp = new ExportPopup();
                exp.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                exp.ShowDialog();
            }
            catch (Exception ex) { Log.Debug("ExportButton_Click", ex); }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StaticClass.Is360CaptureProcess == true) { ModeTabControl.SelectedIndex = __SelectedModeTabIndex; return; }
                if (StaticClass.CapturedImageCount == 0) { tab_video.IsEnabled = true; tab_Single.IsEnabled = true; }
                if (button3 == null) { return; }

                button3.Visibility = Visibility.Collapsed;
                btn_liveview.Visibility = Visibility.Collapsed;

                //grid_360SingleMode.Visibility = Visibility.Collapsed;
                CameraGrid.Visibility = Visibility.Visible;

                bool __showstatus = true;
                if (tab_360.IsSelected || tab_Single.IsSelected)
                {
                    button3.Visibility = Visibility.Visible;
                    __showstatus = false;
                    if (tab_360.IsSelected) { StaticClass.TypeOfCapture_SelectedTabIndex = 0; }
                    if (tab_Single.IsSelected) { StaticClass.TypeOfCapture_SelectedTabIndex = 1; }
                }
                if (tab_video.IsSelected)
                {
                    btn_liveview.Visibility = Visibility.Visible;
                    __showstatus = true;
                    StaticClass.TypeOfCapture_SelectedTabIndex = 2;
                }
                Dispatcher.Invoke(new Action(delegate { LVViewModel.lvInstance().ShowLiveImagePart(__showstatus); }));
                Dispatcher.Invoke(new Action(delegate { _ttVM.LoadSettingsDefaultValues(__showstatus); }));
            }
            catch (Exception ex) { Log.Debug("TabControl_SelectionChanged", ex); }
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
            int _zoomValue = LVViewModel.lvInstance().ZoomSliderValue;

            if (_keyvalue == "Ctrl++") { if (_zoomValue <= 45) { _zoomValue += 5; LVViewModel.lvInstance().ZoomSliderValue = _zoomValue; } }
            if (_keyvalue == "Ctrl+-") { if (_zoomValue >= 5) { _zoomValue -= 5; LVViewModel.lvInstance().ZoomSliderValue = _zoomValue; } }
        }

        #region "EditLeftControl"
        List<ImageDetails> images_History = new List<ImageDetails>();
        List<ImageDetails> images_Folder = new List<ImageDetails>();

        public void BrowseReload(string _strApplPath)
        {
            if (tab_history.IsSelected) { BrowseHistoryImages(_strApplPath); }
            if (tab_folder.IsSelected) { BrowseFolderImages(_strApplPath); }
        }

        private void rbDec_Checked(object sender, RoutedEventArgs e)
        {
            ReloadImages(true);
        }

        private void rbDec_Unchecked(object sender, RoutedEventArgs e)
        {
            ReloadImages(false);
        }

        private void ReloadImages(bool _StatusOrder)
        {
            if (tab_folder.IsSelected)
            {
                if (_StatusOrder == false) { images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime))); }
                if (_StatusOrder == true) { images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(y.CreationDateTime), Convert.ToDateTime(x.CreationDateTime))); }

                ListBoxSnapshots.Items.Clear();
                ImageListBox_Folder.Items.Clear();
                ImageLIstBox_Folder.Items.Clear();
                foreach (var img in images_Folder)
                {
                    ImageLIstBox_Folder.Items.Add(img);
                    ImageListBox_Folder.Items.Add(img);
                    ListBoxSnapshots.Items.Add(img);
                }
            }
            if (tab_history.IsSelected)
            {
                if (_StatusOrder == false) { images_History.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime))); }
                if (_StatusOrder == true) { images_History.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(y.CreationDateTime), Convert.ToDateTime(x.CreationDateTime))); }

                ListBoxSnapshots.Items.Clear();
                ImageListBox.Items.Clear();
                ImageLIstBox.Items.Clear();
                foreach (var img in images_History)
                {
                    ImageLIstBox.Items.Add(img);
                    ImageListBox.Items.Add(img);
                    ListBoxSnapshots.Items.Add(img);
                }
            }
        }
        private void btn_Folderbrowse_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (selectedpath != "")
                {
                    dialog.SelectedPath = selectedpath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {


                    tbFolderName.Text = dialog.SelectedPath;
                    OGFolder = dialog.SelectedPath.ToString();
                    tbFolderName_thmb.Text = dialog.SelectedPath;
                    BrowseFolderImages(tbFolderName.Text);

                    //__Pathupdate.PathImg = "";
                    // ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = null;
                    selectedpath = dialog.SelectedPath;
                }
                
            }
            catch (Exception ex)
            {
                Log.Error("Error set folder ", ex);
            }



        }
        private void BrowseHistoryImages(string _folderPath)
        {
            try
            {
                ImageListBox.Items.Clear();
                ImageLIstBox.Items.Clear();
                ListBoxSnapshots.Items.Clear();
                GC.Collect();
                images_History = new List<ImageDetails>();
                ServiceProvider.Settings.EditImageByte = null;
                string root = "";
                try
                {
                    if (_folderPath == "")
                    {
                        root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "OrangeMonkie", "History");
                        if (!Directory.Exists(root)) { Directory.CreateDirectory(root); }
                    }
                    else { root = _folderPath; }
                }
                catch (Exception exception)
                {
                    Log.Error("Error set My pictures folder", exception);
                    root = "C:\\";
                }

                //if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                //{
                //    root = System.IO.Path.GetDirectoryName(@"D:/Pics");
                //}

                string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };
                var files = Directory.GetFiles(root, "*.*").Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);

                foreach (var f in files)
                {
                    var file = Path.Combine(tempfolder, Path.GetFileName(f));
                    StaticClass.GenerateSmallThumb(f, file);

                    ImageDetails id = new ImageDetails()
                    {
                        Path_Orginal = f,
                        Path = file,
                        FileName = System.IO.Path.GetFileName(file),
                        Extension = System.IO.Path.GetExtension(file),
                        DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd"),
                        CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
                        TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
                    };
                    images_History.Add(id);
                    Thread.Sleep(10);
                }
                images_History.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));

                foreach (var img in images_History)
                {
                    ImageLIstBox.Items.Add(img);
                    ImageListBox.Items.Add(img);
                    ListBoxSnapshots.Items.Add(img);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("", ex);
            }
        }
        private void BrowseFolderImages(string _folderPath)
        {
            //try
            //{
            //    images_Folder = new List<ImageDetails>();

            //    string root = System.IO.Path.GetDirectoryName(_folderPath);
            //    string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };

            //    var files = Directory.GetFiles(_folderPath).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

            //    string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
            //    if (!Directory.Exists(tempfolder))
            //        Directory.CreateDirectory(tempfolder);

            //    foreach (var f in files)
            //    {
            //        var file = Path.Combine(tempfolder, Path.GetFileName(f));
            //        StaticClass.GenerateSmallThumb(f, file);

            //        ImageDetails id = new ImageDetails()
            //        {
            //            Path_Orginal = f,
            //            Path = file,
            //            FileName = System.IO.Path.GetFileName(file),
            //            Extension = System.IO.Path.GetExtension(file),
            //            DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd"),
            //            CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
            //            TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
            //        };
            //        images_Folder.Add(id);
            //        Thread.Sleep(10);
            //    }

            //    images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));

            //    ImageListBox_Folder.Items.Clear();
            //    ImageLIstBox_Folder.Items.Clear();
            //    ListBoxSnapshots.Items.Clear();
            //    foreach (var img in images_Folder)
            //    {
            //        ImageLIstBox_Folder.Items.Add(img);
            //        ImageListBox_Folder.Items.Add(img);
            //        ListBoxSnapshots.Items.Add(img);
            //    }
            //    // int ind = ImageLIstBox_Folder.Items.IndexOf(_item);
            //    ListBoxSnapshots.SelectedIndex = 0;
            //    //ListBoxSnapshots_SelectionChanged(null, null);


            //}

            //catch (Exception ex) { Log.Debug("BrowseFolderImages", ex); }


            try
            {
                _ListBoxSelectedIndex = -1;
                images_Folder = new List<ImageDetails>();

                string root = System.IO.Path.GetDirectoryName(_folderPath);
                string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };

                var files = Directory.GetFiles(_folderPath).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);
                int count = 0;
                foreach (var f in files)
                {
                    var file = Path.Combine(tempfolder, Path.GetFileName(f));
                    StaticClass.GenerateSmallThumb(f, file);

                    ImageDetails id = new ImageDetails()
                    {
                        Path_Orginal = f,
                        Path = file,
                        FileName = System.IO.Path.GetFileName(file),
                        Extension = System.IO.Path.GetExtension(file),
                        DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd"),
                        CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
                        TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
                    };
                    count++;
                    images_Folder.Add(id);
                    Thread.Sleep(10);
                }

                images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));
                ImageListBox_Folder.Items.Clear();
                ImageLIstBox_Folder.Items.Clear();
                ListBoxSnapshots.Items.Clear();

                bool selectedImage = false;
                ImageDetails imrLocal = new ImageDetails();
                foreach (var img in images_Folder)
                {
                    if (!selectedImage)
                    {
                        imrLocal = img;
                        selectedImage = true;
                    }
                    ImageLIstBox_Folder.Items.Add(img);
                    ImageListBox_Folder.Items.Add(img);
                    ListBoxSnapshots.Items.Add(img);
                }
              

                LoadFolderSelectedItem(imrLocal);
                UpdateImageData();
            }
            catch (Exception ex) { Log.Debug("BrowseFolderImages", ex); }
        }

        private void BrowseFolderImages(string _folderPath, string imr)
        {

            try
            {
                _ListBoxSelectedIndex = -1;
                images_Folder = new List<ImageDetails>();

                string root = System.IO.Path.GetDirectoryName(_folderPath);
                string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };

                var files = Directory.GetFiles(_folderPath).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);
                int count = 0;
                foreach (var f in files)
                {
                    var file = Path.Combine(tempfolder, Path.GetFileName(f));
                    StaticClass.GenerateSmallThumb(f, file);

                    ImageDetails id = new ImageDetails()
                    {
                        Path_Orginal = f,
                        Path = file,
                        FileName = System.IO.Path.GetFileName(file),
                        Extension = System.IO.Path.GetExtension(file),
                        DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd"),
                        CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
                        TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
                    };
                    count++;
                    images_Folder.Add(id);
                    Thread.Sleep(10);
                }

                images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));
                ImageListBox_Folder.Items.Clear();
                ImageLIstBox_Folder.Items.Clear();
                ListBoxSnapshots.Items.Clear();

                bool selectedImage = false;
                ImageDetails imrLocal = new ImageDetails();
                foreach (var img in images_Folder)
                {
                    if (img.FileName.Equals(imr) && !selectedImage)
                    {
                        imrLocal = img;
                        selectedImage = true;
                    }
                    ImageLIstBox_Folder.Items.Add(img);
                    ImageListBox_Folder.Items.Add(img);
                    ListBoxSnapshots.Items.Add(img);
                }

                LoadFolderSelectedItem(imrLocal);
                UpdateImageData();
            }
            catch (Exception ex) { Log.Debug("BrowseFolderImages", ex); }
        }
        private void ImgViewGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Image clickedOnItem = (Image)StaticClass.GetParentDependencyObjectFromVisualTree((DependencyObject)e.MouseDevice.DirectlyOver, typeof(Image));

            //if (clickedOnItem != null)
            //{
            //    var Item = (clickedOnItem.DataContext) as ImageDetails;
            //    __Pathupdate.PathImg = Item.Path_Orginal; //clickedOnItem.Source.ToString();
            //    ServiceProvider.Settings.EditImageByte = DSLR_Tool_PC.StaticClass.ConvertImageToByteArray(__Pathupdate.PathImg);//.Substring(8)) ;// ; //converterDemo(clickedOnItem);
            //    PhotoEditModel.GetInstance().ImageData = ServiceProvider.Settings.EditImageByte;
            //    ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(Item.Path, BitmapLoader.LargeThumbSize, 0);

            //    int ind = ListBoxSnapshots.Items.IndexOf(Item);
            //    ListBoxSnapshots.SelectedIndex = ind;
            //    ListBoxSnapshots.ScrollIntoView(Item);
            //}
        }

        private void LoadFolderSelectedItem(ImageDetails _item)
        {
            if (_item != null)
            {
                int ind = ImageLIstBox_Folder.Items.IndexOf(_item);
                if (_ListBoxSelectedIndex != ind)
                {
                    ImageLIstBox_Folder.ScrollIntoView(_item);
                    ImageListBox_Folder.ScrollIntoView(_item);
                    ListBoxSnapshots.ScrollIntoView(_item);

                    __Pathupdate.PathImg = _item.Path;
                    __Pathupdate.__SelectedImageDetails = _item;
                    ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(_item.Path, BitmapLoader.LargeThumbSize, 0);
                    _ListBoxSelectedIndex = ind;
                    __SelectedImage = _item;
                }
                ImageLIstBox_Folder.SelectedIndex = ind;
                ImageListBox_Folder.SelectedIndex = ind;
                ListBoxSnapshots.SelectedIndex = ind;

            }

        }
        private ImageDetails __SelectedImage = null;
        private void LoadHistorySelectedItem(ImageDetails _item)
        {
            if (_item != null)
            {
                int ind = ImageLIstBox.Items.IndexOf(_item);
                if (_ListBoxSelectedIndex != ind)
                {
                    ImageLIstBox.ScrollIntoView(_item);
                    ImageListBox.ScrollIntoView(_item);
                    ListBoxSnapshots.ScrollIntoView(_item);

                    __Pathupdate.PathImg = _item.Path;
                    __Pathupdate.__SelectedImageDetails = _item;
                    ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(_item.Path, BitmapLoader.LargeThumbSize, 0);
                    _ListBoxSelectedIndex = ind;
                    __SelectedImage = _item;
                }
                ImageLIstBox.SelectedIndex = ind;
                ImageListBox.SelectedIndex = ind;
                ListBoxSnapshots.SelectedIndex = ind;
            }

        }
        private void ListBoxSnapshots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    if (tab_folder.IsSelected)
                        LoadFolderSelectedItem(e.AddedItems[0] as ImageDetails);
                    else
                        LoadHistorySelectedItem(e.AddedItems[0] as ImageDetails);

                    //Thread _thread = new Thread(UpdateImageData);
                    //_thread.Start();
                    ETg_Btn8.IsChecked = false;
                    ETg_Btn9.IsChecked = false;
                }
            }
            catch (Exception)
            {
            }
        }
        private void UpdateImageData()
        {
            ServiceProvider.Settings.EditImageByte = DSLR_Tool_PC.StaticClass.ConvertImageToByteArray(__Pathupdate.PathImg);//.Substring(8)) ;// ; //converterDemo(clickedOnItem);
            PhotoEditModel.GetInstance().ImageData = ServiceProvider.Settings.EditImageByte;
        }

        private void ImageLIstBox_Folder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    //LoadFolderSelectedItem(e.AddedItems[0] as ImageDetails);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ImageLIstBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    //LoadHistorySelectedItem(e.AddedItems[0] as ImageDetails);
                }
            }
            catch (Exception)
            {
            }
        }
        private int __IndexOfImageSelTab = 0;
        private void tc_ImageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (__IndexOfImageSelTab == tc_ImageSelection.SelectedIndex) { return; }
            ListBoxSnapshots.Items.Clear();
            if (tab_history.IsSelected)
            {
                foreach (var img in images_History)
                {
                    ListBoxSnapshots.Items.Add(img);
                }
            }
            if (tab_folder.IsSelected)
            {
                foreach (var img in images_Folder)
                {
                    ListBoxSnapshots.Items.Add(img);
                }
            }
            Reset_ImageSelTabChange();
            __IndexOfImageSelTab = tc_ImageSelection.SelectedIndex;
        }

        private void Reset_ImageSelTabChange()
        {
            ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = null;
            ETg_Btn8.IsChecked = false;
            ETg_Btn9.IsChecked = false;
        }

        #endregion

        #region "CropOut"
        bool __CropChangeFromButtons = false;
        bool __CropChangeFromTextBoxes = false;

        private void CheckRatioGridLines_Capture()
        {
            double width = CameraGrid.Width;
            double height = CameraGrid.Height;

            if (CGrid1.IsChecked == true) { GridLines_Capture(1, width, height); }
            else if (CGrid2.IsChecked == true) { GridLines_Capture(2, width, height); }
            else if (CGrid3.IsChecked == true) { GridLines_Capture(3, width, height); }
            else { GridLines_Capture(0, width, height); }
        }

        private void ratio1_Checked(object sender, RoutedEventArgs e)
        {
            __CropChangeFromButtons = true;
            if (CaptureTab.IsSelected)
            {
                ratio2.IsChecked = false;
                ratio3.IsChecked = false;

                LVViewModel.lvInstance().IsStretchToFill = Stretch.UniformToFill;

                double W, H; W = H = 0;

                if (__CameraGrid_ActualHeight < __CameraGrid_ActualWidth)
                {
                    W = H = __CameraGrid_ActualHeight;
                }
                else if (__CameraGrid_ActualWidth < __CameraGrid_ActualHeight)
                {
                    H = W = __CameraGrid_ActualWidth;
                }
                GridRatio_Capture(true, W, H);

                CameraGrid.Width = W;
                CameraGrid.Height = H;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                double X, Y, W, H; X = Y = W = H = 0;
                EditGrid_height = (int)__EditPicGrid_ActualHeight;
                EditGrid_width = (int)__EditPicGrid_ActualWidth;
                if (EditGrid_height < EditGrid_width)
                {
                    W = H = EditGrid_height;
                }
                else if (EditGrid_width < EditGrid_height)
                {
                    W = H = EditGrid_width;
                }
                X = Math.Round(((EditGrid_width - W) / 2), 2);
                Y = Math.Round(((EditGrid_height - H) / 2), 2);

                Text_X.Text = ((int)X).ToString();
                Text_Y.Text = ((int)Y).ToString();
                Text_Width.Text = ((int)W).ToString();
                Text_Height.Text = ((int)H).ToString();
                CropOut.Visibility = Visibility.Visible;
            }
        }
        private void ratio1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                GridRatio_Capture(false, 0, 0);
                LVViewModel.lvInstance().IsStretchToFill = Stretch.Fill;

                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                //EditPicGrid.Width = __EditPicGrid_ActualWidth;
                //EditPicGrid.Height = __EditPicGrid_ActualHeight;
                //CropOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ratio2_Checked(object sender, RoutedEventArgs e)
        {
            double X, Y, W, H; X = Y = W = H = 0;
            if (CaptureTab.IsSelected)
            {
                ratio1.IsChecked = false;
                ratio3.IsChecked = false;

                LVViewModel.lvInstance().IsStretchToFill = Stretch.UniformToFill;

                if (__CameraGrid_ActualHeight > __CameraGrid_ActualWidth)
                {
                    H = W = __CameraGrid_ActualWidth;
                }
                else
                {
                    W = __CameraGrid_ActualWidth;
                    H = __CameraGrid_ActualHeight;
                }
                int remainder = (int)W % 4;
                W = W - remainder;
                int i = (int)W;
                while (i > 0)
                {
                    if (i % 4 == 0 && ((i / 4) * 3) <= H)
                    {
                        W = i;
                        H = (i / 4) * 3;
                        break;
                    }
                    i = i - 1;
                }
                GridRatio_Capture(true, W, H);
                Thread.Sleep(200);

                CameraGrid.Width = W;
                CameraGrid.Height = H;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                EditGrid_height = (int)__EditPicGrid_ActualHeight;
                EditGrid_width = (int)__EditPicGrid_ActualWidth;

                if (__EditPicGrid_ActualHeight > __EditPicGrid_ActualWidth)
                {
                    H = W = EditGrid_width;
                }
                else
                {
                    W = EditGrid_width;
                    H = EditGrid_height;
                }
                int remainder = (int)W % 4;
                W = W - remainder;
                int i = (int)W;
                while (i > 0)
                {
                    if (i % 4 == 0 && ((i / 4) * 3) <= H)
                    {
                        W = i;
                        H = (i / 4) * 3;
                        break;
                    }
                    i = i - 1;
                }
                X = Math.Round(((EditGrid_width - W) / 2), 2);
                Y = Math.Round(((EditGrid_height - H) / 2), 2);

                Text_X.Text = ((int)X).ToString();
                Text_Y.Text = ((int)Y).ToString();
                Text_Width.Text = ((int)W).ToString();
                Text_Height.Text = ((int)H).ToString();
                CropOut.Visibility = Visibility.Visible;
                __CropChangeFromButtons = true;
            }
        }
        private void ratio2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                GridRatio_Capture(false, 0, 0);
                LVViewModel.lvInstance().IsStretchToFill = Stretch.Fill;
                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                //EditPicGrid.Width = __EditPicGrid_ActualWidth;
                //EditPicGrid.Height = __EditPicGrid_ActualHeight;
                //CropOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ratio3_Checked(object sender, RoutedEventArgs e)
        {
            __CropChangeFromButtons = true;
            double X, Y, W, H; X = Y = W = H = 0;
            if (CaptureTab.IsSelected)
            {
                ratio2.IsChecked = false;
                ratio1.IsChecked = false;

                LVViewModel.lvInstance().IsStretchToFill = Stretch.UniformToFill;

                if (__CameraGrid_ActualHeight > __CameraGrid_ActualWidth)
                {
                    H = W = __CameraGrid_ActualWidth;
                }
                else
                {
                    W = __CameraGrid_ActualWidth;
                    H = __CameraGrid_ActualHeight;
                }
                int remainder = (int)W % 16;
                W = W - remainder;
                int i = (int)W;
                while (i > 0)
                {
                    if (i % 16 == 0 && ((i / 16) * 9) <= H)
                    {
                        W = i;
                        H = (i / 16) * 9;
                        break;
                    }
                    i = i--;
                }
                GridRatio_Capture(true, W, H);
                Thread.Sleep(200);

                CameraGrid.Width = W;
                CameraGrid.Height = H;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                EditGrid_height = (int)__EditPicGrid_ActualHeight;
                EditGrid_width = (int)__EditPicGrid_ActualWidth;

                if (EditGrid_height > EditGrid_width)
                {
                    H = W = EditGrid_width;
                }
                else
                {
                    W = EditGrid_width;
                    H = EditGrid_height;
                }
                int remainder = (int)W % 16;
                W = W - remainder;
                int i = (int)W;
                while (i > 0)
                {
                    if (i % 16 == 0 && ((i / 16) * 9) <= H)
                    {
                        W = i;
                        H = (i / 16) * 9;
                        break;
                    }
                    i = i--;
                }
                X = Math.Round(((EditGrid_width - W) / 2), 2);
                Y = Math.Round(((EditGrid_height - H) / 2), 2);

                Text_X.Text = ((int)X).ToString();
                Text_Y.Text = ((int)Y).ToString();
                Text_Width.Text = ((int)W).ToString();
                Text_Height.Text = ((int)H).ToString();
                CropOut.Visibility = Visibility.Visible;
                __CropChangeFromButtons = true;
            }
        }
        private void ratio3_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CaptureTab.IsSelected)
            {
                GridRatio_Capture(false, 0, 0);
                LVViewModel.lvInstance().IsStretchToFill = Stretch.Fill;
                CameraGrid.Width = __CameraGrid_ActualWidth;
                CameraGrid.Height = __CameraGrid_ActualHeight;

                CheckRatioGridLines_Capture();
            }
            else if (EditTab.IsSelected)
            {
                //EditPicGrid.Width = __EditPicGrid_ActualWidth;
                //EditPicGrid.Height = __EditPicGrid_ActualHeight;
                //CropOut.Visibility = Visibility.Collapsed;
            }
        }

        private void ETg_Btn9_Checked(object sender, RoutedEventArgs e)
        {
            if (__Pathupdate.PathImg == null || __Pathupdate.PathImg == "") { return; }
            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage == null) { return; }

            try
            {
                if (__EditPicGrid_ActualWidth <= 0) { __EditPicGrid_ActualWidth = EditPicGrid.ActualWidth; }
                if (__EditPicGrid_ActualHeight <= 0) { __EditPicGrid_ActualHeight = EditPicGrid.ActualHeight; }

                LabelX.Visibility = LabelY.Visibility = LabelW.Visibility = LabelH.Visibility =
                    Text_X.Visibility = Text_Y.Visibility = Text_Width.Visibility = Text_Height.Visibility = Eratio1.Visibility =
                    Eratio2.Visibility = Eratio3.Visibility = ButtonOK.Visibility = Visibility.Visible;
                Eratio1.IsChecked = Eratio2.IsChecked = Eratio3.IsChecked = false;
                CropOut.Visibility = Visibility.Visible;

                double X, Y, W, H; X = Y = W = H = 0;
                W = Math.Round(__EditPicGrid_ActualWidth, 2);
                H = Math.Round(__EditPicGrid_ActualHeight, 2);

                Text_X.Text = ((int)X).ToString();
                Text_Y.Text = ((int)Y).ToString();
                Text_Width.Text = ((int)W).ToString();
                Text_Height.Text = ((int)H).ToString();
            }
            catch (Exception ex) { Log.Debug("", ex); }

        }
        private void ETg_Btn9_Unchecked(object sender, RoutedEventArgs e)
        {
            LabelX.Visibility = LabelY.Visibility = LabelW.Visibility = LabelH.Visibility =
           Text_X.Visibility = Text_Y.Visibility = Text_Width.Visibility = Text_Height.Visibility = Eratio1.Visibility =
               Eratio2.Visibility = Eratio3.Visibility = ButtonOK.Visibility = CropOut.Visibility = Visibility.Collapsed;
            Reset_CropValues();
        }
        private void Reset_CropValues()
        {
            Text_X.Text = "";
            Text_Y.Text = "";
            Text_Width.Text = "";
            Text_Height.Text = "";
            if (__EditPicGrid_ActualWidth > 0) EditPicGrid.Width = __EditPicGrid_ActualWidth;
            if (__EditPicGrid_ActualHeight > 0) EditPicGrid.Height = __EditPicGrid_ActualHeight;
        }

        private void Text_Height_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) { Text_Height.Text = (Convert.ToDouble(Text_Height.Text) + 1).ToString(); }
            if (e.Key == Key.Down) { Text_Height.Text = (Convert.ToDouble(Text_Height.Text) - 1).ToString(); }
        }
        private void Text_Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            double value;
            if (!double.TryParse(Text_Height.Text, out value)) value = 0;
            RefreshCrop("H", value);
        }

        private void Text_Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            double value;
            if (!double.TryParse(Text_Width.Text, out value)) value = 0;
            RefreshCrop("W", value);
        }

        private void Text_Y_TextChanged(object sender, TextChangedEventArgs e)
        {
            double value;
            if (!double.TryParse(Text_Y.Text, out value)) value = 0;
            RefreshCrop("Y", value);
        }

        private void Text_X_TextChanged(object sender, TextChangedEventArgs e)
        {
            double value;
            if (!double.TryParse(Text_X.Text, out value)) value = 0;
            RefreshCrop("X", value);
        }
        private void RefreshCrop(string _source, double _value)
        {
            double x1, x2, y1, y2; x1 = x2 = y1 = y2 = 0;
            if (_source == "X")
            {
                Double.TryParse(Text_X.Text, out x1);
                x2 = CropOut.Width;
                y1 = CropOut.Margin.Top;
                y2 = CropOut.Height;
                //if ((x2 + x1) > __EditPicGrid_ActualWidth) { x2 = __EditPicGrid_ActualWidth - x1; }
            }
            if (_source == "W")
            {
                x1 = CropOut.Margin.Left;
                Double.TryParse(Text_Width.Text, out x2);
                y1 = CropOut.Margin.Top;
                y2 = CropOut.Height;
            }
            if (_source == "Y")
            {
                x1 = CropOut.Margin.Left;
                x2 = CropOut.Width;
                Double.TryParse(Text_Y.Text, out y1);
                y2 = CropOut.Height;
                //if ((y2 + y1) > __EditPicGrid_ActualHeight) { y2 = __EditPicGrid_ActualHeight - y1; }
                //if (y2 > 0) { CropOut.Height = (int)y2; }
            }
            if (_source == "H")
            {
                x1 = CropOut.Margin.Left;
                x2 = CropOut.Width;
                y1 = CropOut.Margin.Top;
                Double.TryParse(Text_Height.Text, out y2);
            }
            RefreshCrop(x1, y1, x2, y2);
            if (__CropChangeFromTextBoxes == true && __CropChangeFromButtons == true)
            {
                if (Eratio1.IsChecked == true) Eratio1.IsChecked = false;
                if (Eratio2.IsChecked == true) Eratio2.IsChecked = false;
                if (Eratio3.IsChecked == true) Eratio3.IsChecked = false;

                __CropChangeFromButtons = false;
            }
        }

        private void RefreshCrop(double X, double Y, double W, double H)
        {
            try
            {
                CropOut.Width = W;
                CropOut.Height = H;
                CropOut.RadiusX = 0;
                CropOut.RadiusY = 0;
                CropOut.Margin = new Thickness(X, Y, 0, 0);

                CropOut.Visibility = Visibility.Visible;
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }

        bool isDragging = false;
        private void ctlImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //-------------< ctlImage_MouseDown() >-------------
            if (isDragging == false)
            {
                isDragging = true;
            }
            //-------------</ ctlImage_MouseDown() >-------------
        }
        private void ctlImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //-------------< ctlImage_MouseUp() >-------------
            if (isDragging)
            {
                isDragging = false;
            }
            //-------------</ ctlImage_MouseUp() >-------------
        }

        private void ctlImage_MouseMove(object sender, MouseEventArgs e)
        {
            //-------------< ctlImage_MouseMove() >-------------
            if (isDragging)
            {
                double x = e.GetPosition(EditFramePicEdit).X;
                double y = e.GetPosition(EditFramePicEdit).Y;
                double posLeft = 10; //CropOut.Margin.Left;
                double posTop = 5; // CropOut.Margin.Top;
                CropOut.Margin = new Thickness(0, 0, 0, 0);
                CropOut.Width = x - posLeft;
                CropOut.Height = y - posTop;

                //< anzeigen >
                if (CropOut.Visibility != Visibility.Visible)
                { CropOut.Visibility = Visibility.Visible; }
                //</ anzeigen >
            }
            //-------------</ ctlImage_MouseMove() >-------------
        }
        #endregion

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (EditTab.IsSelected) { BrowseHistoryImages(); }
        }

        #region SidChanges

        private void btn_Editbg_1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_Edit_Buttons.Background = new System.Windows.Media.SolidColorBrush(Colors.White);
                grd_Edit_Canvasbg.Background = new System.Windows.Media.SolidColorBrush(Colors.White);
                grd_Edit_canvasUpper.Background = new System.Windows.Media.SolidColorBrush(Colors.White);
                grd_Edit_Bottom.Background = new System.Windows.Media.SolidColorBrush(Colors.White);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_33.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_BtnEditbg_int = 1;
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }
        private void btn_Editbg_2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_Edit_Buttons.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);
                grd_Edit_Canvasbg.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);
                grd_Edit_canvasUpper.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);
                grd_Edit_Bottom.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_32.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_BtnEditbg_int = 2;
            }

            catch (Exception ex) { Log.Debug("", ex); }
        }
        private void btn_Editbg_3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_Edit_Buttons.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
                grd_Edit_Canvasbg.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
                grd_Edit_canvasUpper.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
                grd_Edit_Bottom.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_31.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_BtnEditbg_int = 3;
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }


        private void btn_bg_1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_bg.Background = new System.Windows.Media.SolidColorBrush(Colors.White);
                CameraGrid.Background = new System.Windows.Media.SolidColorBrush(Colors.White);
                grd_downbutton.Background = new System.Windows.Media.SolidColorBrush(Colors.White);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_33.PNG"));
                Tg_Btnbg.Background = brush;

            }
            catch (Exception ex) { Log.Debug("", ex); }
        }

        private void btn_bg_2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_bg.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);
                CameraGrid.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);
                grd_downbutton.Background = new System.Windows.Media.SolidColorBrush(Colors.Gray);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_32.PNG"));
                Tg_Btnbg.Background = brush;
                Tg_Btnbg_int = 2;
            }

            catch (Exception ex) { Log.Debug("", ex); }
        }

        private void btn_bg_3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                grd_bg.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
                CameraGrid.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
                grd_downbutton.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);

                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_31.PNG"));
                Tg_Btnbg.Background = brush;
                Tg_Btnbg_int = 3;

            }
            catch (Exception ex) { Log.Debug("", ex); }
        }

        bool Tg_Btnbg_Click_bool = false;
        bool Tg_EditBtnbg_Click_bool = false;
        int Tg_Btnbg_int = 0;
        int Tg_BtnEditbg_int = 0;

        private void Tg_EditBtnbg_Click(object sender, RoutedEventArgs e)
        {
            if (Tg_EditBtnbg_Click_bool == false)
            {
                brd_Editbg.Visibility = Visibility.Visible;
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_31.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_BtnEditbg.Background = brush;
                Tg_EditBtnbg_Click_bool = true;
            }
            else
            {
                brd_Editbg.Visibility = Visibility.Collapsed;
                var brush = new ImageBrush();
                //if (Tg_Btnbg_int == 3) { brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_3.PNG")); }
                //else if (Tg_Btnbg_int == 2) { brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_2.PNG")); }
                //else
                //{
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_1.PNG"));
                //}
                Tg_BtnEditbg.Background = brush;
                Tg_BtnEditbg.Background = brush;
                Tg_EditBtnbg_Click_bool = false;
            }
        }

        private void Tg_Btnbg_Click(object sender, RoutedEventArgs e)
        {
            if (Tg_Btnbg_Click_bool == false)
            {
                brd_bg.Visibility = Visibility.Visible;
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_31.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_Btnbg.Background = brush;
                Tg_Btnbg_Click_bool = true;
            }
            else
            {
                brd_bg.Visibility = Visibility.Collapsed;
                var brush = new ImageBrush();
                //if (Tg_Btnbg_int == 3) { brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_3.PNG")); }
                //else if (Tg_Btnbg_int == 2) { brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_2.PNG")); }
                //else
                //{
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_1.PNG"));
                //}
                Tg_BtnEditbg.Background = brush;
                Tg_Btnbg.Background = brush;
                Tg_Btnbg_Click_bool = false;
            }
        }

        private void Tg_Btnbg_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Tg_Btnbg_Click_bool == false)
            {
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_2.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_Btnbg.Background = brush;
            }
        }

        private void Tg_Btnbg_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Tg_Btnbg_Click_bool == false)
            {
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/bg_1.PNG"));
                Tg_BtnEditbg.Background = brush;
                Tg_Btnbg.Background = brush;
            }
        }

        #endregion Sidchanges




        private void cmb_Zoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LVViewModel.lvInstance() == null) { return; }
                if (LVViewModel.lvInstance().CameraDevice == null) { return; }

                int value = 0;
                if (cmb_Zoom.SelectedIndex == 0) { value = 50; }
                if (cmb_Zoom.SelectedIndex == 1) { value = 45; }
                if (cmb_Zoom.SelectedIndex == 2) { value = 40; }
                if (cmb_Zoom.SelectedIndex == 3) { value = 35; }
                if (cmb_Zoom.SelectedIndex == 4) { value = 30; }
                if (cmb_Zoom.SelectedIndex == 5) { value = 25; }
                if (cmb_Zoom.SelectedIndex == 6) { value = 0; }

                LVViewModel.lvInstance().ZoomSliderValue = value;
            }
            catch (Exception ex) { Log.Debug("Slider_ValueChanged", ex); }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (LVViewModel.lvInstance() == null) { return; }
                if (LVViewModel.lvInstance().CameraDevice == null) { return; }

                LVViewModel.lvInstance().ZoomSliderValue = (int)sldZoom.Value;
            }
            catch (Exception ex) { Log.Debug("Slider_ValueChanged", ex); }
        }
        private void Tg_Btn5_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                cmb_Zoom.SelectedIndex = 6;
            }
            catch (Exception ex) { Log.Debug("Tg_Btn5_Checked", ex); }
        }

        private void Tg_Btn5_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LVViewModel.lvInstance() == null) { return; }
                if (LVViewModel.lvInstance().CameraDevice == null) { return; }

                LVViewModel.lvInstance().ZoomSliderValue = 0;
                sldZoom.Value = 0;
            }
            catch (Exception ex) { Log.Debug("Tg_Btn5_Unchecked", ex); }
        }

        int __SelectedModeTabIndex = -1;
        TurnTableViewModel _ttVM = TurnTableViewModel.GetInstance();

        private void free_Timer()
        {
            string _installDetectorFile = Path.Combine(Settings.ApplicationFolder, "VerifyInstall.txt");
            FileInfo fileInfo = new FileInfo(_installDetectorFile);
            DateTime creationDate = fileInfo.CreationTime;
            DateTime today = DateTime.Now;

            var daysUsed = today.Subtract(creationDate);
            int Usedfordays = daysUsed.Days;

            if (Usedfordays > 30)
            {
                trial_Over();
            }
            //Task.Delay(10000).ContinueWith(t => trial_Over());
        }

        private void trial_Over()
        {
            buyPremium.Visibility = Visibility.Visible;
            CaptureTab.IsEnabled = false;
            EditTab.IsEnabled = false;
            CaptureRightTab.IsEnabled = false;
            grd_downbutton.IsEnabled = false;
            ModeTabControl.IsEnabled = false;

        }

        private void purchaseButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://orangemonkie.com/");
        }


        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (DSLR_Tool_PC.StaticClass.CapturedImageCount != 0) { return; }
            if (tab_360.IsSelected)
            {
                if (_ttVM.mBluetoothLEDevice != null)
                {
                    __SelectedModeTabIndex = 0;
                    _ttVM.CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                    _ttVM.__StartRotation = true;
                    StaticClass.Is360CaptureProcess = true;
                    Dispatcher.Invoke(new Action(delegate { _ttVM.CapturePhoto_360_Recursive(); }));
                }
            }
            if (tab_Single.IsSelected)
            {
                CapturePhoto();
            }
        }

        private void btn_liveview_Click(object sender, RoutedEventArgs e)
        {
            if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy && ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
            {
                StaticClass.Is360CaptureProcess = true;
                __SelectedModeTabIndex = 2;
                Dispatcher.Invoke(new Action(delegate { LVViewModel.lvInstance().RecordLiveView(); }));
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        public void ShowProgress()
        {
            ProgressPannel.Visibility = Visibility.Visible;
        }
        public void HideProgress()
        {
            ProgressPannel.Visibility = Visibility.Collapsed;
        }
    }
}