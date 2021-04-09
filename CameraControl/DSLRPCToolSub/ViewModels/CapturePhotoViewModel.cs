using System.Windows;
using CameraControl.Devices;
using System.IO;
using CameraControl.Devices.Classes;
using System.Threading;
using System;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace DSLR_Tool_PC.ViewModels
{
    public class CapturePhotoViewModel : BaseFieldClass
    {
        private CameraDeviceManager DeviceManager { get; set; }
        private string FolderForPhotos { get; set; }

        private byte[] _CaptureImageByte;
        public byte[] CaptureImageByte
        {
            get { return _CaptureImageByte; }
            set
            {
                if (_CaptureImageByte != value)
                {
                    _CaptureImageByte = value;
                    NotifyPropertyChanged("CaptureImageByte");
                }
            }
        }

        private LevelGraphViewModel _CaptureLevelGraphVM = new LevelGraphViewModel();
        public LevelGraphViewModel CaptureLevelGraphVM
        {
            get { return _CaptureLevelGraphVM; }
            set { _CaptureLevelGraphVM = value; }
        }

        public CapturePhotoViewModel()
        {
            DeviceManager = new CameraDeviceManager();
            //DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            //DeviceManager.CameraConnected += DeviceManager_CameraConnected;
            //DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            //DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
            // For experimental Canon driver support- to use canon driver the canon sdk files should be copied in application folder
            DeviceManager.UseExperimentalDrivers = true;
            DeviceManager.DisableNativeDrivers = false;
            FolderForPhotos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Test");
            DeviceManager.ConnectToCamera();

            RefreshDisplay();
        }

        private string _CapImagePath = "";
        public string CapImagePath
        {
            get { return _CapImagePath; }
            set
            {
                _CapImagePath = value;
                CaptureLevelGraphVM.ImagePath = value;
            }
        }

        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            //System.Windows.Forms.MethodInvoker method = delegate
            //{
            //    btn_liveview.Enabled = newcameraDevice.GetCapability(CapabilityEnum.LiveView);
            //};
            //if (InvokeRequired)
            //    BeginInvoke(method);
            //else
            //    method.Invoke();
        }
        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }
        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            // to prevent UI freeze start the transfer process in a new thread
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }
        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            //System.Windows.Forms.MethodInvoker method = delegate
            // {
            //cmb_cameras.BeginUpdate();
            //cmb_cameras.Items.Clear();
            foreach (ICameraDevice cameraDevice in DeviceManager.ConnectedDevices)
            {
                if (cameraDevice.DeviceName == "Canon EOS 1300D")
                    DeviceManager.SelectedCameraDevice = cameraDevice;
                //cmb_cameras.Items.Add(cameraDevice);
            }
            //cmb_cameras.DisplayMember = "DeviceName";
            //cmb_cameras.SelectedItem = DeviceManager.SelectedCameraDevice;
            DeviceManager.SelectedCameraDevice.CaptureInSdRam = true;
            // check if camera support live view
            //btn_liveview.Enabled = DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.LiveView);
            // cmb_cameras.EndUpdate();
            //};

            //if (InvokeRequired)
            //    BeginInvoke(method);
            //else
            //    method.Invoke();
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null) return;
            try
            {
                string fileName = Path.Combine(FolderForPhotos, Path.GetFileName(eventArgs.FileName));
                // if file exist try to generate a new filename to prevent file lost. 
                // This useful when camera is set to record in ram the the all file names are same.
                if (File.Exists(fileName))
                    fileName = StaticHelper.GetUniqueFilename(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0, Path.GetExtension(fileName));

                // check the folder of filename, if not found create it
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                // the IsBusy may used internally, if file transfer is done should set to false  
                eventArgs.CameraDevice.IsBusy = false;
                CapImagePath = fileName;

                CaptureImageByte = DSLR_Tool_PC.StaticClass.ConvertImageToByteArray(fileName);
            }
            catch (Exception exception)
            {
                eventArgs.CameraDevice.IsBusy = false;
                MessageBox.Show("Error download photo from camera :\n" + exception.Message);
            }
        }

        public void CaptureImage()
        {
            //Thread thread = new Thread(Capture);            
            //thread.Start();

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
                        //this.ShowMessageAsync("Error", TranslationStrings.MsgBulbModeNotSupported);
                        return;
                    }
                }
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

        void AssignPicture()
        {
            try
            {
                while (true)
                {
                    //if (CurrentImage != CapImagePath)
                    //{
                    //    BitmapImage bitmap = new BitmapImage();
                    //    bitmap.BeginInit();
                    //    bitmap.UriSource = new Uri(CapImagePath);
                    //    bitmap.EndInit();
                    //    img_photo.Source = bitmap;
                    //    CapImagePath = "";
                    //    break;
                    //}
                }
            }
            catch (Exception ex) { Log.Debug("AssignPicture", ex); }
        }

        private void Capture()
        {
            bool retry;
            do
            {
                retry = false;
                try
                {
                    //RefreshDisplay();
                    DeviceManager.SelectedCameraDevice.CapturePhoto();
                }
                catch (DeviceException exception)
                {
                    // if device is bussy retry after 100 miliseconds
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy ||
                        exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        // !!!!this may cause infinite loop
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        MessageBox.Show("Error occurred :" + exception.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred :" + ex.Message);
                }

            } while (retry);
        }
    }
}
