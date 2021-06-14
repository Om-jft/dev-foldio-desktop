using DSLR_Tool_PC.Classes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using CameraControl.DSLRPCToolSub.Controles;
using CameraControl.Core;
using CameraControl.Core.Classes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Diagnostics;
using ImageMagick;
using CameraControl.Devices;
using CameraControl.Core.Translation;
using Windows.Storage.Streams;
using CameraControl.DSLRPCToolSub.Classes;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;

namespace DSLR_Tool_PC.ViewModels
{
    public class TurnTableViewModel : BaseFieldClass
    {
        Thread __Thread;
        private readonly object _Sliderlockobj = new object();

        private int mBTState = BTCmd.STATE_IDLE;
        int ExportWidth = 1920;
        int ExportHeight = 1080;


        private System.Timers.Timer _timer = new System.Timers.Timer(1000);
        private System.Timers.Timer _TimerSearchFoldioStatus = new System.Timers.Timer(1000);
        private bool __FoldioWriteReturnStatus = false;
        public BluetoothLEDevice mBluetoothLEDevice;

        public RelayCommand Minus_90_Command { get; set; }
        public RelayCommand Plus_90_Command { get; set; }
        public RelayCommand Minus_360_Command { get; set; }
        public RelayCommand Plus_360_Command { get; set; }
        public RelayCommand Left_with_Angle { get; set; }
        public RelayCommand Right_with_Angle { get; set; }
        public RelayCommand Left_Rotation { get; set; }
        public RelayCommand Right_Rotation { get; set; }
        public RelayCommand SelectBTDevice { get; set; }
        public RelayCommand cmd_DeviceActivity { get; set; }
        public GalaSoft.MvvmLight.Command.RelayCommand<string> Cmb_PrevValue { get; set; }
        public GalaSoft.MvvmLight.Command.RelayCommand<string> Cmb_NextValue { get; set; }

        private ICameraDevice _cameraDevice;
        public ICameraDevice CameraDevice
        {
            get { return _cameraDevice; }
            set
            {
                _cameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
            }
        }

        List<TodoItem> _Foldioitems = new List<TodoItem>();
        public List<TodoItem> FoldioItems
        {
            get { return _Foldioitems; }
            set
            {
                _Foldioitems = value;
                OnPropertyChanged();
                NotifyPropertyChanged("FoldioItems");
            }
        }

        private static TurnTableViewModel _ttVM_inst = null;
        public static TurnTableViewModel GetInstance()
        {
            if (_ttVM_inst == null) { _ttVM_inst = new TurnTableViewModel(); }
            return _ttVM_inst;
        }

        public TurnTableViewModel()
        {
            IsDomeMode = false;
            IsNotDomeMode = true;

            _frameNumbers.Add(new ComboItemClass() { ItemName = "24", ItemId = 1 });
            _frameNumbers.Add(new ComboItemClass() { ItemName = "36", ItemId = 2 });
            _frameNumbers.Add(new ComboItemClass() { ItemName = "48", ItemId = 3 });

            _speeds.Add(new ComboItemClass() { ItemName = "x1", ItemId = 1 });
            _speeds.Add(new ComboItemClass() { ItemName = "x2", ItemId = 2 });
            _speeds.Add(new ComboItemClass() { ItemName = "x3", ItemId = 3 });

            _angles.Add(new ComboItemClass() { ItemName = "15", ItemId = 1 });
            _angles.Add(new ComboItemClass() { ItemName = "10", ItemId = 2 });
            _angles.Add(new ComboItemClass() { ItemName = "7.5", ItemId = 3 });

            //_Foldioitems.Add(new TodoItem() { Title = "Foldio360", DeviceAddress = "asdfghjkq"});
            //_Foldioitems.Add(new TodoItem() { Title = "Foldio360D", DeviceAddress = "12345678"}); 
            //_Foldioitems.Add(new TodoItem() { Title = "FOLDIO360", DeviceAddress = "87654321" });


            Minus_90_Command = new RelayCommand(MoveLeft_90);
            Plus_90_Command = new RelayCommand(MoveRight_90);
            Minus_360_Command = new RelayCommand(MoveLeft_360);
            Plus_360_Command = new RelayCommand(MoveRight_360);

            Left_with_Angle = new RelayCommand(MoveLeft_with_Angle);
            Right_with_Angle = new RelayCommand(MoveRight_with_Angle);
            Left_Rotation = new RelayCommand(MoveLeft);
            Right_Rotation = new RelayCommand(MoveRight);
            SelectBTDevice = new RelayCommand(FindBluetoothDevices);
            cmd_DeviceActivity = new RelayCommand(StopDeviceProcessing);
            Cmb_PrevValue = new GalaSoft.MvvmLight.Command.RelayCommand<string>(Prev_Value);
            Cmb_NextValue = new GalaSoft.MvvmLight.Command.RelayCommand<string>(Next_Value);
            //BTgetdevices();
            //Task<bool> bb = BTCmd.GetBluetoothIsEnabledAsync();

            _timer.AutoReset = true;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void FindBluetoothDevices()
        {
            BTDeviceSearch _btSearch = new BTDeviceSearch();
            _btSearch.ShowDialog();
            mBluetoothLEDevice = _btSearch.CurrentSelectedDevice;
            if (mBluetoothLEDevice != null)
            {
                var t = mBluetoothLEDevice.DeviceInformation.Properties;
                IsDeviceControlEnable = true;
                IsSelectedDevicePanel = false;
                IsFrameSettingsEnable = true;
                IsDomeMode = mBluetoothLEDevice.Name.Equals("Foldio360") ? false : true;
                IsFoldioFound = true;
                DeviceName = mBluetoothLEDevice.Name;
                DeviceSlno = mBluetoothLEDevice.BluetoothAddress.ToString();
                if (IsDomeMode) { DisplayedDeviceImagePath = "pack://application:,,,/DSLRPCToolSub/Assets/Images/none/Component 5x1.png"; }
                else { DisplayedDeviceImagePath = "pack://application:,,,/DSLRPCToolSub/Assets/Images/none/360-device-white.png"; }

                haloEdgeStatusChanges(0);
                ColorTemparatureStatusChanges(0);
                //HaloEdge = 0;
                //ColorTemparature = 2300;
                _TimerSearchFoldioStatus.Interval = 1000;
                _TimerSearchFoldioStatus.Elapsed += _TimerSearchFoldioStatus_Elapsed;
                _TimerSearchFoldioStatus.Start();

                _rotationAngle_Text = 0;
                RotationAngle_Text = 0;
            }
            else
            {
                IsSelectedDevicePanel = true; IsFoldioFound = false;
                DisconnectedBluetoothDevice();
            }
        }

        private void DisconnectedBluetoothDevice()
        {
            StopDeviceProcessing();

            mBluetoothLEDevice = null;
            _TimerSearchFoldioStatus.Elapsed -= _TimerSearchFoldioStatus_Elapsed;
            _TimerSearchFoldioStatus.Stop();

            IsDeviceControlEnable = false;
            IsSelectedDevicePanel = true;
            IsDomeMode = false;
            IsNotDomeMode = true;
            IsFoldioFound = false;
            DeviceName = "";
            DeviceSlno = "";
            HaloEdge = 0;
            ColorTemparature = 2300;
        }

        private void _TimerSearchFoldioStatus_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_loading == true) { return; }
            _loading = true;
            selectDevice(mBluetoothLEDevice.Name, mBluetoothLEDevice.BluetoothAddress.ToString());
        }

        private readonly object _syncRoot = new object();
        private void Next_Value(string cmd)
        {
            ObservableCollection<ComboItemClass> ItemValues = null;
            ComboItemClass IValue = null;

            if (cmd == "Frame_Next")
            {
                ItemValues = FrameNumbers;
                IValue = SelectedFrame;
            }
            if (cmd == "Angle_Next")
            {
                ItemValues = Angles;
                IValue = SelectedAngleValue;
            }
            if (cmd == "Speed_Next")
            {
                ItemValues = Speeds;
                IValue = SelectedSpeed;
            }

            lock (_syncRoot)
            {
                if (ItemValues == null || ItemValues.Count == 0)
                    return;
                int ind = ItemValues.IndexOf(IValue);
                if (ind < 0)
                    return;
                ind++;
                if (ind < ItemValues.Count)
                    IValue = ItemValues[ind];
            }

            if (cmd == "Frame_Next")
            {
                SelectedFrame = IValue;
            }
            if (cmd == "Angle_Next")
            {
                SelectedAngleValue = IValue;
            }
            if (cmd == "Speed_Next")
            {
                SelectedSpeed = IValue;
            }
        }
        private void Prev_Value(string cmd)
        {
            ObservableCollection<ComboItemClass> ItemValues = null;
            ComboItemClass IValue = null;

            if (cmd == "Frame_Prev")
            {
                ItemValues = FrameNumbers;
                IValue = SelectedFrame;
            }
            if (cmd == "Angle_Prev")
            {
                ItemValues = Angles;
                IValue = SelectedAngleValue;
            }
            if (cmd == "Speed_Prev")
            {
                ItemValues = Speeds;
                IValue = SelectedSpeed;
            }

            lock (_syncRoot)
            {
                if (ItemValues == null || ItemValues.Count == 0)
                    return;
                int ind = ItemValues.IndexOf(IValue);
                ind--;
                if (ind < 0)
                    return;

                if (ind < ItemValues.Count)
                    IValue = ItemValues[ind];
            }

            if (cmd == "Frame_Prev")
            {
                SelectedFrame = IValue;
            }
            if (cmd == "Angle_Prev")
            {
                SelectedAngleValue = IValue;
            }
            if (cmd == "Speed_Prev")
            {
                SelectedSpeed = IValue;
            }
        }

        public bool __StartRotation = false;

        private void DeviceActivityPanelTextChange(bool _deviceState, string _message)
        {
            if (mBluetoothLEDevice == null) { return; }
            if (_deviceState == true)
            {
                //Device Start 
                IsFoldioFound = false;
                IsDeviceActivityMode = true;
            }
            else if (_deviceState == false)
            {
                //Device Stop
                IsFoldioFound = true;
                IsDeviceActivityMode = false;
                lbl_DeviceActivityHeader = "";
                btn_DeviceActivityCaption = "Cancel";
            }
            switch (_message)
            {
                case "Capture_360":
                    lbl_DeviceActivityHeader = "Processing... " + DSLR_Tool_PC.StaticClass.CapturedImageCount.ToString() + "/" + SelectedFrame.ItemName;
                    btn_DeviceActivityCaption = "Cancel";
                    break;

                case "Capture_Single":
                    break;

                case "Capture_Vedio":
                    break;

                case "Device_Rotate":
                    lbl_DeviceActivityHeader = "Moving...";
                    btn_DeviceActivityCaption = "Stop";
                    break;

                default:
                    lbl_DeviceActivityHeader = "";
                    btn_DeviceActivityCaption = "Cancel";
                    break;
            }
        }
        
        public int getframe()
        {
            return Convert.ToInt32(SelectedFrame.ItemName);
        }
        public void CapturePhoto_360_Recursive()
        {
            __Thread = new Thread(CapturePhoto_360_RecursiveNew);
            __Thread.Start();
        }
        private void CapturePhoto_360_RecursiveNew()
        {
            if (StaticClass.Is360CaptureProcess == false) { StopDeviceProcessing(); }
            IsDeviceControlEnable = false;
            IsFrameSettingsEnable = false;
            DeviceActivityPanelTextChange(true, "Capture_360");

            if (ServiceProvider.DeviceManager.SelectedCameraDevice != CameraDevice || __StartRotation == false)
            {
                DeviceActivityPanelTextChange(false, "");
                IsFoldioFound = true;
                IsDeviceActivityMode = false;
                IsDeviceControlEnable = true;
                IsFrameSettingsEnable = true;

                StaticClass.CapturedImageCount = 0;
                StaticClass.IsCapturedPhoto = false;
                StaticClass.Is360CaptureProcess = false;

                images.Clear();
                return;
            }

            int _FrameNum = Convert.ToInt32(SelectedFrame.ItemName);
            int _RotateAngle = 0;
            int _FrameSpeed = getSpeed();

            if (_FrameNum > 0)
            {
                _RotateAngle = Math.Abs(360 / _FrameNum);
                if (ExceedAngle == 0)
                {
                    ExceedAngle = _RotateAngle % 2;
                    _RotateAngle = _RotateAngle - ExceedAngle;
                }
                else
                {
                    _RotateAngle = _RotateAngle + ExceedAngle;
                    ExceedAngle = 0;
                }
                _RotateAngle = (_RotateAngle / BTCmd.ANGLE_DIVIDENT);
            }

            if (_FrameNum > 0 && _RotateAngle > 0)
            {
                if (_FrameNum == DSLR_Tool_PC.StaticClass.CapturedImageCount)
                {
                    DeviceActivityPanelTextChange(false, "");
                    IsFoldioFound = true;
                    IsDeviceActivityMode = false;
                    IsDeviceControlEnable = true;
                    IsFrameSettingsEnable = true;
                    RecentHistory.Last360 = ServiceProvider.Settings.DefaultSession.Folder;
                    StaticClass.CapturedImageCount = 0;
                    StaticClass.IsCapturedPhoto = false;
                    StaticClass.Is360CaptureProcess = false;

                    //_backgroundWorker_DoWork();
                    string OutPutFile = Path.GetDirectoryName(DSLR_Tool_PC.StaticClass.FileName_LastCapturedImage);
                    images.Clear();
                    MessageBox.Show("Saved Successfully at location. " + Environment.NewLine + OutPutFile, "360 Image", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            V2:
                int CaptureTimeCount = 0;
                StaticClass.FileName_LastCapturedImage = "";
                ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.Capture);
                Thread.Sleep(3000);
            V1:
                if (System.IO.File.Exists(DSLR_Tool_PC.StaticClass.FileName_LastCapturedImage) || DSLR_Tool_PC.StaticClass.IsCapturedPhoto)
                {
                    Thread.Sleep(500);
                    CollectImages(DSLR_Tool_PC.StaticClass.FileName_LastCapturedImage);
                    sendBluetoothMessage(BTCmd.STATE_ROTATING, BTCmd.rotateRight(_RotateAngle, getSpeed(), 1));
                    Thread.Sleep((int)((350.0 * _RotateAngle) / _FrameSpeed));
                    StaticClass.CapturedImageCount += 1;

                    Thread.Sleep(500);

                    CapturePhoto_360_RecursiveNew();
                }
                else
                {
                    if (CaptureTimeCount < 60)
                    {
                        CaptureTimeCount += 1;
                        Thread.Sleep(100);
                        goto V1;
                    }
                    else
                    {
                        goto V2;
                    }
                }
            }
        }

        private int ExceedAngle = 0;

        public ObservableCollection<ComboItemClass> _frameNumbers = new ObservableCollection<ComboItemClass>();
        public ObservableCollection<ComboItemClass> FrameNumbers
        {
            get { return _frameNumbers; }
            set
            {
                _frameNumbers = value;
                OnPropertyChanged();
            }
        }
        private ComboItemClass _selectedFrame = new ComboItemClass();
        public ComboItemClass SelectedFrame
        {
            get { return _selectedFrame; }
            set
            {
                _selectedFrame = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComboItemClass> _speeds = new ObservableCollection<ComboItemClass>();
        public ObservableCollection<ComboItemClass> Speeds
        {
            get { return _speeds; }
            set
            {
                _speeds = value;
                OnPropertyChanged();
            }
        }

        private ComboItemClass _selectedSpeed = new ComboItemClass();
        public ComboItemClass SelectedSpeed
        {
            get { return _selectedSpeed; }
            set
            {
                _selectedSpeed = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComboItemClass> _angles = new ObservableCollection<ComboItemClass>();
        public ObservableCollection<ComboItemClass> Angles
        {
            get { return _angles; }
            set
            {
                _angles = value;
                OnPropertyChanged();
            }
        }

        private ComboItemClass _selectedAngleValue = new ComboItemClass();
        public ComboItemClass SelectedAngleValue
        {
            get { return _selectedAngleValue; }
            set
            {
                _selectedAngleValue = value;
                OnPropertyChanged();
            }
        }


        private TodoItem p_SelectedItem;
        public TodoItem SelectedtemFoldioDevice
        {
            get { return p_SelectedItem; }

            set
            {
                p_SelectedItem = value;
                NotifyPropertyChanged("SelectedtemFoldioDevice");
            }
        }

        private bool _isSelectedDevicePanel = true;
        public bool IsSelectedDevicePanel
        {
            get { return _isSelectedDevicePanel; }
            set
            {
                _isSelectedDevicePanel = value;
                NotifyPropertyChanged("IsSelectedDevicePanel");
            }
        }
        private bool _isListOfDeviceEnable = false;
        public bool IsListOfDeviceEnable
        {
            get { return _isListOfDeviceEnable; }
            set
            {
                _isListOfDeviceEnable = value;
                NotifyPropertyChanged("IsListOfDeviceEnable");
            }
        }

        private bool _isPCBluetoothOFF = false;
        public bool IsPCBluetoothOFF
        {
            get { return _isPCBluetoothOFF; }
            set
            {
                _isPCBluetoothOFF = value;
                NotifyPropertyChanged("IsPCBluetoothOFF");
            }
        }

        private bool _isFoldioFound = false;
        public bool IsFoldioFound
        {
            get { return _isFoldioFound; }
            set
            {
                _isFoldioFound = value;
                NotifyPropertyChanged("IsFoldioFound");
            }
        }

        private bool _isDomeMode = false;
        public bool IsDomeMode
        {
            get { return _isDomeMode; }
            set
            {
                _isDomeMode = value;
                NotifyPropertyChanged("IsDomeMode");
                IsNotDomeMode = IsDomeMode;
            }
        }

        private bool _isNotDomeMode = true;
        public bool IsNotDomeMode
        {
            get { return _isNotDomeMode; }
            set
            {
                _isNotDomeMode = value;
                NotifyPropertyChanged("IsNotDomeMode");
            }
        }

        private bool _IsDeviceControlEnable = false;
        public bool IsDeviceControlEnable
        {
            get { return _IsDeviceControlEnable; }
            set
            {
                _IsDeviceControlEnable = value;
                NotifyPropertyChanged("IsDeviceControlEnable");
            }
        }
        private bool _IsFrameSettingsEnable = false;
        public bool IsFrameSettingsEnable
        {
            get { return _IsFrameSettingsEnable; }
            set
            {
                _IsFrameSettingsEnable = value;
                NotifyPropertyChanged("IsFrameSettingsEnable");
            }
        }
        private bool _isDeviceActivityMode = false;
        public bool IsDeviceActivityMode
        {
            get { return _isDeviceActivityMode; }
            set
            {
                _isDeviceActivityMode = value;
                NotifyPropertyChanged("IsDeviceActivityMode");
            }
        }

        private string _lbl_DeviceActivityHeader = "...";
        public string lbl_DeviceActivityHeader
        {
            get { return _lbl_DeviceActivityHeader; }
            set
            {
                _lbl_DeviceActivityHeader = value;
                NotifyPropertyChanged("lbl_DeviceActivityHeader");
            }
        }

        private string _btn_DeviceActivityCaption = "Cancel";
        public string btn_DeviceActivityCaption
        {
            get { return _btn_DeviceActivityCaption; }
            set
            {
                _btn_DeviceActivityCaption = value;
                NotifyPropertyChanged("btn_DeviceActivityCaption");
            }
        }

        private string _selectedAngleIndex;
        public string SelectedAngleIndex
        {
            get { return _selectedAngleIndex; }
            set
            {
                _selectedAngleIndex = value;
                NotifyPropertyChanged("SelectedAngleIndex");
            }
        }

        private int _selectedSpeedIndex = 0;
        public int SelectedSpeedIndex
        {
            get { return _selectedSpeedIndex; }
            set
            {
                _selectedSpeedIndex = value;
                NotifyPropertyChanged("SelectedSpeedIndex");
            }
        }

        private bool _moveLeftCheckedStatus = false;
        public bool MoveLeftCheckedStatus
        {
            get { return _moveLeftCheckedStatus; }
            set
            {
                if (RotationAngle_Text > 0)
                {
                    _moveLeftCheckedStatus = value;
                    NotifyPropertyChanged("MoveLeftCheckedStatus");
                    if (_moveLeftCheckedStatus == true) { MoveLeft_with_Angle(); }
                }
            }
        }

        private bool _moveRightCheckedStatus = false;
        public bool MoveRightCheckedStatus
        {
            get { return _moveRightCheckedStatus; }
            set
            {
                _moveRightCheckedStatus = value;
                NotifyPropertyChanged("MoveRightCheckedStatus");
                if (_moveRightCheckedStatus == true) { MoveRight_with_Angle(); }
            }
        }

        private bool _leftCheckedStatus = false;
        public bool LeftCheckedStatus
        {
            get { return _leftCheckedStatus; }
            set
            {
                _leftCheckedStatus = value;
                NotifyPropertyChanged("LeftCheckedStatus");
                if (_leftCheckedStatus == true) { MoveLeft(); }
            }
        }

        private bool _rightCheckedStatus = false;
        public bool RightCheckedStatus
        {
            get { return _rightCheckedStatus; }
            set
            {
                _rightCheckedStatus = value;
                NotifyPropertyChanged("RightCheckedStatus");
                if (_rightCheckedStatus == true) { MoveRight(); }
            }
        }

        public int getSpeed()
        {
            return BTCmd.mArrSpeed[SelectedSpeedIndex];
        }

        private int getRepeat()
        {
            return BTCmd.REPEAT_INFINITE;
        }

        private bool isBusy()
        {
            return mBTState != BTCmd.STATE_IDLE;
        }

        private void WaitingForDeviceActivity(double _Angle)
        {
            var WaitingTime = ((BTCmd.DeviceRotateTimePerAngle * _Angle) / getSpeed());
            Thread.Sleep((int)WaitingTime);
        }

        bool __DeviceLock = false;
        int _rotate_angl = 0;
        string _rotate_Direction = "";
        private void StartDeviceProcessing()
        {
            if (__DeviceLock == false && mBluetoothLEDevice != null)
            {
                int _repeate = BTCmd.REPEAT_ONCE;
                IsDeviceControlEnable = false;
                IsFrameSettingsEnable = false;
                switch (_rotate_Direction)
                {
                    case "MoveLeft_90":
                        _rotate_angl = (90 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateLeft(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveRight_90":
                        _rotate_angl = (90 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveLeft_360":
                        //if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == false && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; }
                        _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateLeft(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveRight_360":
                        //if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == false && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; }
                        _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveLeft_with_Angle":
                        if (RotationAngle_Text > 0)
                        {
                            _rotate_angl = (RotationAngle_Text / BTCmd.ANGLE_DIVIDENT);
                            if (_rotate_angl == 0 && RotationAngle_Text > 0) { _rotate_angl = RotationAngle_Text; }
                            rotateInControlMode(BTCmd.ROTATATION_DIRECTION_LEFT, _rotate_angl, getSpeed(), _repeate, true);
                        }
                        //
                        break;
                    case "MoveRight_with_Angle":
                        if (RotationAngle_Text > 0)
                        {
                            _rotate_angl = (RotationAngle_Text / BTCmd.ANGLE_DIVIDENT);
                            if (_rotate_angl == 0 && RotationAngle_Text > 0) { _rotate_angl = RotationAngle_Text; }
                            rotateInControlMode(BTCmd.ROTATATION_DIRECTION_RIGHT, _rotate_angl, getSpeed(), _repeate, true);
                        }
                        //
                        break;
                    case "MoveLeft":
                        rotateInControlMode(BTCmd.ROTATATION_DIRECTION_LEFT, 360, getSpeed(), BTCmd.REPEAT_INFINITE, false);
                        //sendBluetoothMessage(BTCmd.STATE_IDLE, BTCmd.cancel(false));
                        break;
                    case "MoveRight":
                        rotateInControlMode(BTCmd.ROTATATION_DIRECTION_RIGHT, 360, getSpeed(), BTCmd.REPEAT_INFINITE, false);
                        //sendBluetoothMessage(BTCmd.STATE_IDLE, BTCmd.cancel(false));
                        break;
                    case "VideoRight":
                        if (IsDeviceAndVedioMode == true && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; _rotate_angl = 0; }
                        if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == true) { _repeate = BTCmd.REPEAT_ONCE; _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT); }
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                }

                __StartRotation = true;
                WaitingForDeviceActivity(_rotate_angl);
                if ( _rotate_Direction == "VideoRight"  && _rotate_angl == 0) { return; }
                if (_rotate_Direction == "MoveLeft" || _rotate_Direction == "MoveRight" ) { return; }
                _rotate_angl = 0;
                _rotate_Direction = "";
                IsDeviceControlEnable = true;
                IsFrameSettingsEnable = true;
                __StartRotation = false;
                DeviceActivityPanelTextChange(false, "");
                MoveRightCheckedStatus = false;
                MoveLeftCheckedStatus = false;
            }
        }
        private void StartDeviceTurnProcessing()
        {
            if (__DeviceLock == false && mBluetoothLEDevice != null)
            {
                int _repeate = BTCmd.REPEAT_ONCE;
                //IsDeviceControlEnable = false;
                switch (_rotate_Direction)
                {
                    case "MoveLeft_90":
                        _rotate_angl = (90 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateLeft(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveRight_90":
                        _rotate_angl = (90 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveLeft_360":
                        //if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == false && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; }
                        _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateLeft(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveRight_360":
                        //if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == false && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; }
                        _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT);
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                    case "MoveLeft_with_Angle":
                        if (RotationAngle_Text > 0)
                        {
                            _rotate_angl = (RotationAngle_Text / BTCmd.ANGLE_DIVIDENT);
                            if (_rotate_angl == 0 && RotationAngle_Text > 0) { _rotate_angl = RotationAngle_Text; }
                            rotateInControlMode(BTCmd.ROTATATION_DIRECTION_LEFT, _rotate_angl, getSpeed(), _repeate, true);
                        }
                        //
                        break;
                    case "MoveRight_with_Angle":
                        if (RotationAngle_Text > 0)
                        {
                            _rotate_angl = (RotationAngle_Text / BTCmd.ANGLE_DIVIDENT);
                            if (_rotate_angl == 0 && RotationAngle_Text > 0) { _rotate_angl = RotationAngle_Text; }
                            rotateInControlMode(BTCmd.ROTATATION_DIRECTION_RIGHT, _rotate_angl, getSpeed(), _repeate, true);
                        }
                        //
                        break;
                    case "MoveLeft":
                        rotateInControlMode(BTCmd.ROTATATION_DIRECTION_LEFT, 360, getSpeed(), BTCmd.REPEAT_INFINITE, false);
                        //sendBluetoothMessage(BTCmd.STATE_IDLE, BTCmd.cancel(false));
                        break;
                    case "MoveRight":
                        rotateInControlMode(BTCmd.ROTATATION_DIRECTION_RIGHT, 360, getSpeed(), BTCmd.REPEAT_INFINITE, false);
                        //sendBluetoothMessage(BTCmd.STATE_IDLE, BTCmd.cancel(false));
                        break;
                    case "VideoRight":
                        if (IsDeviceAndVedioMode == true && IsCheckedContinueTurn == true) { _repeate = BTCmd.REPEAT_INFINITE; _rotate_angl = 0; }
                        if (IsDeviceAndVedioMode == true && IsCheckedSingleTurn == true) { _repeate = BTCmd.REPEAT_ONCE; _rotate_angl = (360 / BTCmd.ANGLE_DIVIDENT); }
                        sendBluetoothMessage(BTCmd.STATE_ROTATING_90_DEGREE, BTCmd.rotateRight(_rotate_angl, getSpeed(), _repeate));
                        break;
                }

                __StartRotation = true;
                WaitingForDeviceActivity(_rotate_angl);
                if (_rotate_Direction == "VideoRight" && _rotate_angl == 0) { return; }
                if (_rotate_Direction == "MoveLeft" || _rotate_Direction == "MoveRight") { return; }
                _rotate_angl = 0;
                _rotate_Direction = "";
                IsDeviceControlEnable = true;
                __StartRotation = false;
                DeviceActivityPanelTextChange(false, "");
                MoveRightCheckedStatus = false;
                MoveLeftCheckedStatus = false;
            }
        }
        public void StopDeviceProcessing()
        {
            if (__Thread != null) { __Thread.Abort(); }
            //if (__Thread.IsAlive == true || __StartRotation == true)
            if (__StartRotation == true)
            {
                sendBluetoothMessage(BTCmd.STATE_IDLE, BTCmd.cancel(false));
                sendBluetoothMessage(BTCmd.STATE_GET_STATUS, BTCmd.GET_STATUS);
                _rotate_angl = 0;
                _rotate_Direction = "";
                DeviceActivityPanelTextChange(false, "");

                __StartRotation = false;
                lbl_DeviceActivityHeader = "";
                IsDeviceActivityMode = false;

                IsFoldioFound = true;
                IsDeviceActivityMode = false;
                IsDeviceControlEnable = true;
                IsFrameSettingsEnable = true;
                StaticClass.CapturedImageCount = 0;
                StaticClass.IsCapturedPhoto = false;
                StaticClass.Is360CaptureProcess = false;

                images.Clear();

                RightCheckedStatus = false;
                LeftCheckedStatus = false;
                MoveLeftCheckedStatus = false;
                MoveRightCheckedStatus = false;

                IsRightCheckedEvent = false;
                IsLeftCheckedEvent = false;
            }
        }

        private void MoveLeft_90()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveLeft_90";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        private void MoveRight_90()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveRight_90";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        private void MoveLeft_360()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveLeft_360";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        private void MoveRight_360()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveRight_360";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        public void MoveLeft_with_Angle()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveLeft_with_Angle";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        private void MoveRight_with_Angle()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveRight_with_Angle";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        public void MoveLeft()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveLeft";
            __Thread = new Thread(StartDeviceTurnProcessing);
            __Thread.Start();
        }

        public void MoveRight()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "MoveRight";
            __Thread = new Thread(StartDeviceTurnProcessing);
            __Thread.Start();
        }
        public void VideoMode_Right()
        {
            DeviceActivityPanelTextChange(true, "Device_Rotate");
            _rotate_Direction = "VideoRight";
            __Thread = new Thread(StartDeviceProcessing);
            __Thread.Start();
        }

        private void rotateInControlMode(int direction, int angle, int speed, int repeat, bool withTopAnim)
        {
            if (repeat == BTCmd.REPEAT_INFINITE)
            {
                angle = BTCmd.ROTATE_ANGLE_INFINITE;
            }
            if (direction == BTCmd.ROTATATION_DIRECTION_LEFT)
            {
                if (withTopAnim)
                {

                }
                //_rotate_Direction = "L";
                sendBluetoothMessage(BTCmd.STATE_ROTATING, BTCmd.rotateLeft(angle, speed, repeat));
            }
            else
            {
                if (withTopAnim)
                {

                }
                //_rotate_Direction = "R";
                sendBluetoothMessage(BTCmd.STATE_ROTATING, BTCmd.rotateRight(angle, speed, repeat));
            }
        }

        private int _rotationAngle_Text = 0;
        public int RotationAngle_Text
        {
            get { return _rotationAngle_Text; }
            set
            {
                _rotationAngle_Text = value;
                NotifyPropertyChanged("RotationAngle_Text");
            }
        }

        private int _haloEdge = 0;
        public int HaloEdge
        {
            get { return _haloEdge; }
            set
            {
                _haloEdge = value;
                NotifyPropertyChanged("HaloEdge");

                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 20))
                        {
                            if (mBluetoothLEDevice != null) { haloEdgeStatusChanges(_haloEdge); }
                        }
                });
            }
        }

        private int _colorTemparature = 0;
        public int ColorTemparature
        {
            get { return _colorTemparature; }
            set
            {
                _colorTemparature = value;
                NotifyPropertyChanged("ColorTemparature");

                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 20))
                        {
                            if (mBluetoothLEDevice != null) { ColorTemparatureStatusChanges(_colorTemparature); }
                        }
                });
            }
        }

        private string _deviceName = "Foldio360";
        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                _deviceName = value;
                NotifyPropertyChanged("DeviceName");
            }
        }

        private string _deviceVersion = "1.0.0";
        public string DeviceVersion
        {
            get { return _deviceVersion; }
            set
            {
                _deviceVersion = value;
                NotifyPropertyChanged("DeviceVersion");
            }
        }

        private string _deviceSlno = "XXXXXXXX";
        public string DeviceSlno
        {
            get { return _deviceSlno; }
            set
            {
                _deviceSlno = value;
                NotifyPropertyChanged("DeviceSlno");
            }
        }

        public bool haloEdgeStatusChanges(int tempheloValue)
        {
            string message = string.Format("set_bright({0})", tempheloValue);
            sendBluetoothMessage(BTCmd.STATE_NONE, message);

            return true;
        }

        public bool ColorTemparatureStatusChanges(int ColortemphValue)
        {
            //int TempValue = BTCmd.temperatureMin + (100 * ColortemphValue);
            string message = string.Format("set_temp({0})", ColortemphValue);
            sendBluetoothMessage(BTCmd.STATE_NONE, message);

            return true;
        }

        public void sendBluetoothMessage(int state, string message)
        {
            byte[] value;
            try
            {
                __FoldioWriteReturnStatus = false;
                //send data to service
                value = Encoding.UTF8.GetBytes(message);
                writeRXCharacteristic(value);

                if (state != BTCmd.STATE_NONE)
                {
                    mBTState = state;
                }
            }
            catch (Exception e) { mBTState = BTCmd.STATE_IDLE; }
        }


        private async void writeRXCharacteristic(byte[] value)
        {
            try
            {
                __FoldioWriteReturnStatus = false;
                if (mBluetoothLEDevice == null)
                {
                    return;
                }
                GattDeviceService RxService = mBluetoothLEDevice.GetGattService(BTCmd.RX_SERVICE_UUID);
                if (RxService == null)
                {
                    return;
                }
                GattCharacteristic RxChar = RxService.GetCharacteristics(BTCmd.RX_CHAR_UUID)[0];
                if (RxChar == null)
                {
                    return;
                }

                RxChar.ValueChanged += Characteristic_ValueChanged;
                var result = write(RxChar, value);
                //SendMessage(mBluetoothLEDevice, RxChar, value.AsBuffer());

                //var result = RxChar.WriteValueAsync( value.AsBuffer(), GattWriteOption.WriteWithResponse);
                //if (result == GattCommunicationStatus.Success) { __FoldioWriteReturnStatus = true; }
                //readCharacteristic();
            }
            catch (Exception ex) { Log.Debug("writeRXCharacteristic", ex); }
        }

        void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            // Parse the data however required.
        }

        /// <summary>  
        /// Write a byte[] into characteristic.  
        /// </summary>  
        /// <param name="characteristic"></param>  
        /// <param name="data"></param>  
        /// <returns>communication status</returns>  
        public async Task<GattCommunicationStatus> write(GattCharacteristic characteristic, byte[] data)
        {
            return await characteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithResponse);
        }
        private async Task SendMessage(BluetoothLEDevice device, GattCharacteristic characteristic, IBuffer message)
        {
            if (characteristic != null && device.ConnectionStatus.Equals(BluetoothConnectionStatus.Connected) && message != null)
            {
                Console.WriteLine($"[{device.Name}] Sending message...");
                GattCommunicationStatus result = await characteristic.WriteValueAsync(message);
                Console.WriteLine($"[{device.Name}] Result: {result}");
            }
        }

        /// <summary>  
        /// Read value from characteristic. Reverse the result using Array.Reverse.  
        /// </summary>  
        /// <returns>read response</returns>  
        public async Task<byte[]> read(GattCharacteristic selectedCharacteristic)
        {
            string r = (await selectedCharacteristic.ReadValueAsync()).Value.ToString();
            byte[] response = (await selectedCharacteristic.ReadValueAsync()).Value.ToArray();
            Array.Reverse(response, 0, response.Length);
            return response;
        }

        [Obsolete]
        public void readCharacteristic()
        {
            try
            {
                if (mBluetoothLEDevice == null)
                {
                    DisconnectedBluetoothDevice();
                    _TimerSearchFoldioStatus.Stop();
                    return;
                }
                GattDeviceService RxService = mBluetoothLEDevice.GetGattService(BTCmd.RX_SERVICE_UUID);
                if (RxService == null)
                {
                    return;
                }
                GattCharacteristic RxChar = RxService.GetCharacteristics(BTCmd.TX_CHAR_UUID)[0];
                if (RxChar == null)
                {
                    return;
                }
                var result = read(RxChar);
                //var result = RxChar.ReadValueAsync();
            }
            catch (Exception ex) { Log.Debug("readCharacteristic", ex); }
        }


        public void enableTXNotification()
        {
            if (mBluetoothLEDevice == null)
            {
                //showMessage("mBluetoothGatt null" + mBluetoothGatt);
                //broadcastUpdate(DEVICE_DOES_NOT_SUPPORT_UART);
                return;
            }

            GattDeviceService RxService = mBluetoothLEDevice.GetGattService(BTCmd.RX_SERVICE_UUID);
            if (RxService == null)
            {
                //showMessage("Rx service not found!");
                //broadcastUpdate(DEVICE_DOES_NOT_SUPPORT_UART);
                return;
            }
            GattCharacteristic TxChar = RxService.GetCharacteristics(BTCmd.TX_CHAR_UUID)[0];
            if (TxChar == null)
            {
                //showMessage("Tx charateristic not found!");
                //broadcastUpdate(DEVICE_DOES_NOT_SUPPORT_UART);
                return;
            }
            //mBluetoothLEDevice.setCharacteristicNotification(TxChar, true);

            //GattDescriptor descriptor = TxChar.GetDescriptors(BTCmd.CCCD);
            //descriptor.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);
            //mBluetoothLEDevice.writeDescriptor(descriptor);

        }

        private string _displayedDeviceImagePath;
        public string DisplayedDeviceImagePath
        {
            get { return _displayedDeviceImagePath; }
            set
            {
                _displayedDeviceImagePath = value;
                NotifyPropertyChanged("DisplayedDeviceImagePath");
            }
        }

        public string ShortPathGIF { get; set; }

        public System.IO.DirectoryInfo FolderNameGIF { get; set; }

        public string TargetFolder { get; set; }

        public string[] FilesGIF { get; set; }

        public double PlayTime { get; set; }

        int __PlayImagePosition = 0;
        private void CollectImages(string _ImageFilePath)
        {
            if (_ImageFilePath == "") { return; }

            var f = _ImageFilePath;
            ImageDetails id = new ImageDetails()
            {
                Path = f,
                FileName = System.IO.Path.GetFileName(f),
                Extension = System.IO.Path.GetExtension(f),
                DateModified = System.IO.File.GetCreationTime(f).ToString("yyyy-MM-dd"),
                IsSelected = false
            };
            id.Width = (int)ExportWidth;
            id.Height = (int)ExportHeight;

            images.Add(id);
            if (id.IsSelected) { __PlayImagePosition = images.Count - 1; }
            PlayTime = (int)images.Count * 1000 / 1000;

        }

        public List<ImageDetails> images = new List<ImageDetails>();
        private void _backgroundWorker_DoWork()
        {
            StaticHelper.Instance.SystemMessage = "Please Wait Collecting Images...";
            string tempFolder = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName());

            try
            {
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);
            }
            catch (Exception) { return; }

            Thread.Sleep(500);
            int counter = 1;

            for (int i = 0; i < images.Count; i++)
            {
                try
                {
                    string fileName = images[i].Path;
                    string outfile = Path.Combine(tempFolder, "img" + counter.ToString("000000") + ".jpg");

                    CopyFile(fileName, outfile);
                    counter++;

                    StaticHelper.Instance.SystemMessage = "" + i + "/" + images.Count + " Images Collected...";
                }
                catch (Exception ex)
                {
                }
            }

            GenerateMp4(tempFolder);

            DeleteTempFolder(tempFolder);
        }

        private void GenerateMp4(string tempFolder)
        {
            if (images.Count <= 0) { return; }
            string _ImagPath = images[0].Path;
            StaticHelper.Instance.SystemMessage = "Video File generation Started...";
            int _FrameNum = Convert.ToInt32(SelectedFrame.ItemName);
            string _FOlder = Path.GetDirectoryName(DSLR_Tool_PC.StaticClass.FileName_LastCapturedImage);
            string OutPutFile = Path.Combine(_FOlder, "mp_" + DateTime.Now.Ticks.ToString() + ".mp4");
            try
            {
                string ffmpegPath = Path.Combine(Settings.ApplicationFolder, "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                {
                    MessageBox.Show("ffmpeg not found! Please reinstall the application.");
                    return;
                }
                string _outLastfileName = Path.Combine(tempFolder, "img%06d.jpg");
                float fr = Convert.ToSingle(_FrameNum / _FrameNum); //(1f / (Convert.ToSingle(PlayTime) * (1f / 3f)));

                //string parameters = @"-r {0} -i {1}\img00%04d.jpg -c:v libx264 -vf fps=25 -pix_fmt yuv420p";
                string parameters = @" -framerate {0} -i {1} -c:v libx264 -vf fps=25 -pix_fmt yuv420p -vf scale={3}:{4}  {2}";

                //new VideoType("4K 16:9", 3840, 2160, ".mp4"),
                //new VideoType("HD 1080 16:9", 1920, 1080, ".mp4"),
                //new VideoType("UXGA 4:3", 1600, 1200, ".mp4"),
                //new VideoType("HD 720 16:9", 1280, 720, ".mp4"),
                //new VideoType("Super VGA 4:3", 800, 600, ".mp4"),

                StaticHelper.Instance.SystemMessage = "Vedio generation process Started...";
                Stopwatch stopWatch = new Stopwatch();
                // transfer file from camera to temporary folder 
                // in this way if the session folder is used as hot folder will prevent write errors
                stopWatch.Start();
                _timer.Interval = 1000;
                _timer.Start();

                Process newprocess = new Process();
                newprocess.StartInfo = new ProcessStartInfo()
                {
                    FileName = ffmpegPath,
                    Arguments = string.Format(parameters, fr, _outLastfileName, OutPutFile, ExportWidth, ExportHeight),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                newprocess.Start();
                newprocess.OutputDataReceived += newprocess_OutputDataReceived;
                newprocess.ErrorDataReceived += newprocess_OutputDataReceived;
                newprocess.BeginOutputReadLine();
                newprocess.BeginErrorReadLine();
                newprocess.WaitForExit();

                _timer.Stop();
                stopWatch.Stop();
                string strTransfer = "Transfer time : " + stopWatch.Elapsed.TotalSeconds.ToString("##.###") + " Speed :" +
                                     Math.Round(
                                         new System.IO.FileInfo(OutPutFile).Length / 1024.0 / 1024 /
                                         stopWatch.Elapsed.TotalSeconds, 2) + " Mb/s";
                Log.Debug(strTransfer);
                StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferDone + " " + strTransfer;

                if (ServiceProvider.Settings.PlaySound)
                {
                    PhotoUtils.PlayCaptureSound();
                }
                Log.Debug("Photo transfer done.");
            }
            catch (Exception ex)
            {
            }
            if (!File.Exists(OutPutFile))
            {
                MessageBox.Show("Video file not found !");
            }
            else
            {
                MessageBox.Show("Saved Successfully at location. " + Environment.NewLine + OutPutFile, "360 Image", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            GC.Collect();
        }

        void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StaticHelper.Instance.SystemMessage = "Please Wait Convertion is under process..";
        }

        private void DeleteTempFolder(string tempFolder)
        {
            try
            {
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
            }
        }


        public int Fps { get; private set; }
        //public int Progress { get; private set; }
        public bool FillImage { get; private set; }

        private void CopyFile(string filename, string destFile)
        {

            using (MagickImage image = new MagickImage(filename))
            {
                double zw = image.Width;
                double zh = image.Height;

                double za = FillImage ? ((zw <= zh) ? zw : zh) : ((zw >= zh) ? zw : zh);

                MagickGeometry geometry = new MagickGeometry(ExportWidth, ExportHeight)
                {
                    IgnoreAspectRatio = true,
                    FillArea = false
                };

                image.FilterType = FilterType.Point;
                image.Resize(geometry);
                image.Quality = 90;
                image.Format = MagickFormat.Jpeg;
                image.Write(destFile);
            }
        }

        public int Progress { get; private set; }
        private object _locker = new object();
        private void newprocess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                lock (_locker)
                {
                    //frame=   20 fps= 12 q=0.0 size=       0kB time=00:00:00.00 bitrate=N/A    
                    if (e.Data != null && e.Data.StartsWith("frame="))
                    {
                        var s = e.Data.Substring(7, 5).Trim();
                        int i = 0;
                        if (int.TryParse(e.Data.Substring(7, 5).Trim(), out i))
                            Progress = i;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private bool _frameNumberEnable = true;
        public bool FrameNumberEnable
        {
            get { return _frameNumberEnable; }
            set
            {
                _frameNumberEnable = value;
                NotifyPropertyChanged("FrameNumberEnable");
            }
        }

        public void LoadSettingsDefaultValues(bool _VedioMode)
        {
            try
            {
                if (StaticClass.TypeOfCapture_SelectedTabIndex != 0)
                {
                    _frameNumbers.Clear();
                    _frameNumbers.Add(new ComboItemClass() { ItemName = "1", ItemId = 4 });
                    FrameNumberEnable = false;

                    //ComboItemClass IValue = null;
                    //IValue = FrameNumbers[2];
                    //SelectedFrame = IValue;
                }
                else
                {
                    _frameNumbers.Clear();
                    _frameNumbers.Add(new ComboItemClass() { ItemName = "24", ItemId = 1 });
                    _frameNumbers.Add(new ComboItemClass() { ItemName = "36", ItemId = 2 });
                    _frameNumbers.Add(new ComboItemClass() { ItemName = "48", ItemId = 3 });
                    FrameNumberEnable = true;

                    //ComboItemClass IValue = null;
                    //IValue = FrameNumbers[0];
                    //SelectedFrame = IValue;
                }
                ComboItemClass IValue = null;
                IValue = FrameNumbers[0];
                SelectedFrame = IValue;

                IsDeviceAndVedioMode = false;
                if (_VedioMode == true)
                {
                    if (IsDomeMode == true) { IsDeviceAndVedioMode = true; }
                }
            }
            catch (Exception ex) { }
        }

        bool _loading = false;
        public async void selectDevice(string deviceName, string deviceAddress)
        {
            var bdevice = await BluetoothLEDevice.FromBluetoothAddressAsync(mBluetoothLEDevice.BluetoothAddress);
            if (bdevice != null)
            {
                if (bdevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                {
                    DisconnectedBluetoothDevice();
                    _TimerSearchFoldioStatus.Stop();
                }
            }

            try
            {
                if (mBluetoothLEDevice != null)
                {
                    var device = await BluetoothLEDevice.FromIdAsync(mBluetoothLEDevice.BluetoothDeviceId.Id);
                    if (device != null)
                    {
                        if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                        {
                            DisconnectedBluetoothDevice();
                            _TimerSearchFoldioStatus.Stop();
                        }
                        var availableServices = await device.GetGattServicesAsync();
                        if (availableServices.Status != GattCommunicationStatus.Success)
                        {
                            DisconnectedBluetoothDevice();
                            _TimerSearchFoldioStatus.Stop();
                        }
                    }
                    else
                    {
                        DisconnectedBluetoothDevice();
                        _TimerSearchFoldioStatus.Stop();
                    }
                }
            }
            catch (Exception ex) { Log.Debug("", ex); }

            readCharacteristic();
            _loading = false;
        }

        private void CheckBluetoothDeviceLE()
        {
            var tcs = new TaskCompletionSource<bool>();

            Task.Run(async () =>
            {
                try
                {
                    // New watcher
                    var watcher = new DnaBluetoothLEAdvertisementWatcher();

                    // Hook into events
                    watcher.StartedListening += () =>
                    {
                        //Console.ForegroundColor = ConsoleColor.DarkYellow;
                        //Console.WriteLine("Started listening");
                    };

                    watcher.StoppedListening += () =>
                    {
                        //Console.ForegroundColor = ConsoleColor.Gray;
                        //Console.WriteLine("Stopped listening");
                    };

                    watcher.NewDeviceDiscovered += (device) =>
                    {
                        //Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine($"New device: {device}");
                    };

                    watcher.DeviceNameChanged += (device) =>
                    {
                        //Console.ForegroundColor = ConsoleColor.Yellow;
                        //Console.WriteLine($"Device name changed: {device}");
                    };

                    watcher.DeviceTimeout += (device) =>
                    {
                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine($"Device timeout: {device}");
                    };

                    // Start listening
                    watcher.StartListening();

                    while (true)
                    {
                        // Pause until we press enter
                        var command = ""; //Console.ReadLine()?.ToLower().Trim();

                        if (string.IsNullOrEmpty(command))
                        {
                            // Get discovered devices
                            var devices = watcher.DiscoveredDevices;

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{devices.Count} devices......");

                            foreach (var device in devices)
                                Console.WriteLine(device);
                        }
                        else if (command == "c")
                        {
                            // Attempt to find contour device
                            var contourDevice = watcher.DiscoveredDevices.FirstOrDefault(
                                f => f.Name.ToLower().Contains("Foldio"));

                            // If we don't find it...
                            if (contourDevice == null)
                            {
                                // Let the user know
                                Console.WriteLine("No Contour device found for connecting");
                                continue;
                            }

                            // Try and connect
                            Console.WriteLine("Connecting to Contour Device...");

                            try
                            {
                                // Try and connect
                                await watcher.PairToDeviceAsync(contourDevice.DeviceId);
                            }
                            catch (Exception ex)
                            {
                                // Log it out
                                Console.WriteLine("Failed to pair to Contour device.");
                                Console.WriteLine(ex);
                            }
                        }
                        // Q to quit
                        else if (command == "q")
                        {
                            break;
                        }
                    }

                    // Finish console application
                    tcs.TrySetResult(true);
                }
                finally
                {
                    // If anything goes wrong, exit out
                    tcs.TrySetResult(false);
                }
            });

            tcs.Task.Wait();
        }

        private bool _isDeviceAndVedioMode;
        public bool IsDeviceAndVedioMode
        {
            get { return _isDeviceAndVedioMode; }
            set
            {
                if (_isDeviceAndVedioMode != value)
                {
                    _isDeviceAndVedioMode = value;
                    NotifyPropertyChanged("IsDeviceAndVedioMode");
                }
            }
        }

        private bool _IsCheckedSingleTurn = true;
        public bool IsCheckedSingleTurn
        {
            get { return _IsCheckedSingleTurn; }
            set
            {
                _IsCheckedSingleTurn = value;
                NotifyPropertyChanged("IsCheckedSingleTurn");
            }
        }

        private bool _IsCheckedContinueTurn = false;
        public bool IsCheckedContinueTurn
        {
            get { return _IsCheckedContinueTurn; }
            set
            {
                _IsCheckedContinueTurn = value;
                NotifyPropertyChanged("IsCheckedContinueTurn");
            }
        }

        private bool _IsRightCheckedEvent = false;
        public bool IsRightCheckedEvent
        {
            get { return _IsRightCheckedEvent; }
            set { _IsRightCheckedEvent = value; NotifyPropertyChanged("IsRightCheckedEvent"); }
        }


        private bool _IsLeftCheckedEvent = false;
        public bool IsLeftCheckedEvent
        {
            get { return _IsLeftCheckedEvent; }
            set { _IsLeftCheckedEvent = value; NotifyPropertyChanged("IsLeftCheckedEvent"); }
        }
    }

    #region "ComboItemClass"
    public class ComboItemClass : BaseFieldClass
    {
        private string _itemName;
        public string ItemName
        {
            get { return _itemName; }
            set
            {
                _itemName = value;
                this.OnPropertyChanged();
            }
        }

        private int _itemId = 0;
        public int ItemId
        {
            get { return _itemId; }
            set
            {
                _itemId = value;
                this.OnPropertyChanged();
            }
        }
    }
    #endregion 
    #region "Child TodoItems"
    public class TodoItem
    {
        public string Title { get; set; }
        public string DeviceAddress { get; set; }
        //public int Completion { get; set; }
    }
    #endregion 
}
