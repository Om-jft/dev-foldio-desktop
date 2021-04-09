#region

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CameraControl;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.ViewModel;
using CameraControl.windows;
using DSLR_Tool_PC.ViewModels;
using MahApps.Metro.Controls;

#endregion

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for Controler.xaml
    /// </summary>
    public partial class LVControler : UserControl
    {
        LVViewModel __lvVM = LVViewModel.lvInstance();

        private DateTime _focusMoveTime = DateTime.Now;

        public LiveViewData LiveViewData { get; set; }

        private CameraProperty _cameraProperty;

        public CameraProperty CameraProperty
        {
            get { return _cameraProperty; }
            set
            {
                _cameraProperty = value;
                //NotifyPropertyChanged("CameraProperty");
            }
        }
        public LVControler()
        {
            try
            {
                ICameraDevice SelectedPortableDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
                LiveViewManager.PreviewLoaded += LiveViewManager_PreviewLoaded;

                ExecuteCommand("LiveViewWnd_Show", SelectedPortableDevice);
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }
            Init();
        }

        public void ExecuteCommand(string _cmd)
        {
            ExecuteCommand(_cmd, ServiceProvider.DeviceManager.SelectedCameraDevice);
        }

        private void LiveViewManager_PreviewLoaded(ICameraDevice cameraDevice, string file)
        {
            try
            {
                Task.Factory.StartNew(() =>
                           {
                               Thread.Sleep(500);
                               App.Current.BeginInvoke(zoomAndPanControl.ScaleToFit);
                           });
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }

        }

        private void Init()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Error("Live view init error ", ex);
            }
        }
        private void ExecuteCommand(string cmd, object param)
        {
            if (param == null) { param = ServiceProvider.DeviceManager.SelectedCameraDevice; }

            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    if (DataContext != null)
                        ((LVViewModel)(DataContext)).WindowsManager_Event(cmd, param);
                }
                catch (Exception)
                {


                }
            }));
            switch (cmd)
            {
                case WindowsCmdConsts.LiveViewWnd_Show:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        try
                        {
                            ICameraDevice cameraparam = param as ICameraDevice;
                            var properties = cameraparam.LoadProperties();
                            if (properties.SaveLiveViewWindow && properties.WindowRect.Width > 0 &&
                                properties.WindowRect.Height > 0)
                            {
                                //this.Left = properties.WindowRect.Left;
                                //this.Top = properties.WindowRect.Top;
                                //this.Width = properties.WindowRect.Width;
                                //this.Height = properties.WindowRect.Height;
                            }
                            else
                            {
                                //this.WindowState =
                                //    ((Window)ServiceProvider.PluginManager.SelectedWindow).WindowState;
                            }

                            if (cameraparam == ServiceProvider.DeviceManager.SelectedCameraDevice && IsVisible)
                            {
                                //Activate();
                                //Focus();
                                return;
                            }
                            //__lvVM.UnInit();
                            __lvVM.LoadIntialize(cameraparam);
                            DataContext = __lvVM;

                            //DataContext = new LVViewModel(cameraparam);

                            //Show();
                            //Activate();
                            //Focus();

                        }
                        catch (Exception exception)
                        {
                            Log.Error("Error initialize live view window ", exception);
                        }
                    }
                    ));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Hide:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        try
                        {
                            ICameraDevice cameraparam = ((LVViewModel)DataContext).CameraDevice;
                            var properties = cameraparam.LoadProperties();
                            if (properties.SaveLiveViewWindow)
                            {
                                //properties.WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height);
                            }
                            ((LVViewModel)DataContext).UnInit();
                        }
                        catch (Exception exception)
                        {
                            Log.Error("Unable to stop live view", exception);
                        }
                        //Hide();
                        //ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.FocusStackingWnd_Hide);
                    }));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Message:
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            //if (this.IsLoaded)
                            //    this.ShowMessageAsync("", (string)param);
                            //else
                            //{
                            //    MessageBox.Show((string)param);
                            //}
                        }));
                    }
                    break;
                case CmdConsts.All_Close:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (DataContext != null)
                        {
                            ICameraDevice cameraparam = ((LVViewModel)DataContext).CameraDevice;
                            var properties = cameraparam.LoadProperties();
                            if (properties.SaveLiveViewWindow)
                            {
                                // properties.WindowRect = new Rect(this.Left, this.Top, this.Width, this.Height);
                            }
                            ((LVViewModel)DataContext).UnInit();
                            //Hide();
                            //Close();
                        }
                    }));
                    break;
                case CmdConsts.All_Minimize:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        //WindowState = WindowState.Minimized;
                    }));
                    break;
                case WindowsCmdConsts.LiveViewWnd_Maximize:
                    Dispatcher.Invoke(new Action(delegate
                    {
                        //WindowState = WindowState.Maximized;
                    }));
                    break;
            }
        }
        private void _image_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Left)
                {
                    //if (ServiceProvider.DeviceManager.SelectedCameraDevice.LiveViewImageZoomRatio == null) { return; }
                    //if (ServiceProvider.DeviceManager.SelectedCameraDevice.LiveViewImageZoomRatio.Value == "All")
                    //{
                    try
                    {
                        ((LVViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_image), _image.ActualWidth, _image.ActualHeight);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Focus Error", exception);
                        StaticHelper.Instance.SystemMessage = "Focus error: " + exception.Message;
                    }
                    //}
                }
            }
        }

        private void image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Left)
                {
                    //if (ServiceProvider.DeviceManager.SelectedCameraDevice.LiveViewImageZoomRatio == null) { return; }
                    //if (ServiceProvider.DeviceManager.SelectedCameraDevice.LiveViewImageZoomRatio.Value == "All")
                    //{
                    try
                    {
                        ((LVViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_image), _image.ActualWidth, _image.ActualHeight);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Focus Error", exception);
                        StaticHelper.Instance.SystemMessage = "Focus error: " + exception.Message;
                    }
                    //}
                }
            }
        }


        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            Point curContentMousePoint = e.GetPosition(PreviewBitmap);
            if (e.Delta > 0)
            {
                zoomAndPanControl.ZoomIn(curContentMousePoint);
            }
            else if (e.Delta < 0)
            {
                // don't allow zoomout les that original image 
                if (zoomAndPanControl.ContentScale - 0.2 > zoomAndPanControl.FitScale())
                {
                    zoomAndPanControl.ZoomOut(curContentMousePoint);
                }
                else
                {
                    zoomAndPanControl.ScaleToFit();
                }
            }
        }

        private void zoomAndPanControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            zoomAndPanControl.ScaleToFit();
        }

        private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point curContentMousePoint = e.GetPosition(PreviewBitmap);
            if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
            {
                zoomAndPanControl.ZoomAboutPoint(4, curContentMousePoint);
            }
            else
            {
                zoomAndPanControl.ScaleToFit();
            }
        }

        private void ButtonZoomMinus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomMinus_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void ButtonZoomPlus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ((LVViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
            }
            catch (Exception exception)
            {
                Log.Debug("Zoom error", exception);
            }
        }

        private void _image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ((LVViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.NextValue();
            }
            else
            {
                ((LVViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.PrevValue();
            }
        }

        private void canvas_image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
                {
                    try
                    {
                        ((LVViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_previeImage), _previeImage.ActualWidth, _previeImage.ActualHeight);

                    }
                    catch (Exception exception)
                    {
                        Log.Error("Focus Error", exception);
                        StaticHelper.Instance.SystemMessage = "Focus error: " + exception.Message;
                    }
                }
            }
        }

        //public void ZoomScroller(double _value)
        //{
        //    try
        //    {
        //        if (_value <= 100 || _value > 500) { zoomAndPanControl.ScaleToFit(); }
        //        else
        //        {
        //            double _newcontentscale = _value / 100;
        //            Point curContentMousePoint = new Point(StaticClass.ImageMouse_X, StaticClass.ImageMouse_Y);
        //            zoomAndPanControl.ZoomAboutPoint(_newcontentscale, curContentMousePoint);
        //        }
        //    }
        //    catch (Exception ex) { }
        //}

        //private void _image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    Point _newPOint = e.MouseDevice.GetPosition(_image);
        //    ((LVViewModel)DataContext).LiveViewData.FocusX = (int)_newPOint.X;
        //    ((LVViewModel)DataContext).LiveViewData.FocusY = (int)_newPOint.Y;
        //}

        //public void _ImageStretchChange(bool _IsStretchFill = true)
        //{
        //    if (_IsStretchFill == false) { _image.Stretch = System.Windows.Media.Stretch.UniformToFill; }
        //    else { _image.Stretch = System.Windows.Media.Stretch.Fill; }

        //}
    }
}