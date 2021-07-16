
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace DSLR_Tool_PC.Classes
{
    public class BTCmd
    {
        public static int DeviceRotateTimePerAngle = 248; // device 1 angle Rotate time is 0.25 sec
        public static int NO_INDICATOR = 0;
        public static int WITH_INDICATOR = 1;
      
        public static int ANGLE_DIVIDENT = 2;

        public static string ROTATE_LEFT = "CW";
        public static string ROTATE_RIGHT = "CCW";
        public static int ROTATE_ANGLE_INFINITE = 0;
        public static int ROTATE_REPEAT = 0;

        public static string ROTATE = "rotate({0},{1},{2},{3})";  // direction, angle, speed, repeat
        public static string CANCEL = "cancel({0})";
        public static string GET_BRIGHT = "get_bright";
        public static string GET_STATUS = "get_status";
        public static string Botled = "bot_led({0})";

        // BT Status
        public static int STATE_NONE = -1;
        public static int STATE_IDLE = 0;
        public static int STATE_ROTATING = 1;
        public static int STATE_DSLR_TAKING_PICTURE = 2;
        public static int STATE_DSLR_TAKING_PICTURE_DELAY = 3;
        public static int STATE_DSLR_TEST = 4;
        public static int STATE_GET_STATUS = 5;
        public static int STATE_GET_BRIGHT = 6;
        public static int STATE_PHONE_TAKING_PICTURE = 7;
        public static int STATE_SHOOTING = 8;
        public static int STATE_ROTATING_BY_TOUCH = 9;
        public static int STATE_ROTATING_90_DEGREE = 10;
        public static int STATE_DSLR_GET_AUDIO_STATUS = 11;

        public static int STATE_DISCONNECTED = 0;
        public static int STATE_CONNECTING = 1;
        public static int STATE_CONNECTED = 2;

        public static string ACTION_GATT_CONNECTED = "com.nordicsemi.nrfUART.ACTION_GATT_CONNECTED";
        public static string ACTION_GATT_DISCONNECTED = "com.nordicsemi.nrfUART.ACTION_GATT_DISCONNECTED";
        public static string ACTION_GATT_SERVICES_DISCOVERED = "com.nordicsemi.nrfUART.ACTION_GATT_SERVICES_DISCOVERED";
        public static string ACTION_DATA_AVAILABLE = "com.nordicsemi.nrfUART.ACTION_DATA_AVAILABLE";
        public static string EXTRA_DATA = "com.nordicsemi.nrfUART.EXTRA_DATA";
        public static string DEVICE_DOES_NOT_SUPPORT_UART = "com.nordicsemi.nrfUART.DEVICE_DOES_NOT_SUPPORT_UART";

        public static Guid TX_POWER_UUID = new Guid("00001804-0000-1000-8000-00805f9b34fb");
        public static Guid TX_POWER_LEVEL_UUId = new Guid("00002a07-0000-1000-8000-00805f9b34fb");
        public static Guid CCCD = new Guid("00002902-0000-1000-8000-00805f9b34fb");
        public static Guid FIRMWARE_REVISON_UUID = new Guid("00002a26-0000-1000-8000-00805f9b34fb");
        public static Guid DIS_UUID = new Guid("0000180a-0000-1000-8000-00805f9b34fb");
        public static Guid RX_SERVICE_UUID = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        public static Guid RX_CHAR_UUID = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        public static Guid TX_CHAR_UUID = new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e");

        public static int temperatureMin = 2300;
        public static int temperatureMax = 5600;
        public static int temperatureMaxProgress = 5600 - 2300;


        // Rotation
        public static int ORIENTATION_HYSTERESIS = 5;
        public static int ROTATATION_DIRECTION_RIGHT = 0;
        public static int ROTATATION_DIRECTION_LEFT = 1;

        public static int REPEAT_INFINITE = 0;
        public static int REPEAT_ONCE = 1;

        public static int[] mArrSpeed = { 1, 2, 3 };
        public static int[] mArrFrame = { 24, 36, 48 };

        // Flags
        public static int mSpeedIndex = 1;
        public static int mFrameIndex = 0;


        public static string rotateLeft(int angle, int speed, int repeat)
        {
            return string.Format(ROTATE, ROTATE_LEFT, angle, speed, repeat);
        }

        public static string rotateRight(int angle, int speed, int repeat)
        {
            return string.Format(ROTATE, ROTATE_RIGHT, angle, speed, repeat);
        }

        public static string cancel(bool withIndicator)
        {
            int indicator = withIndicator ? WITH_INDICATOR : NO_INDICATOR;
            return string.Format(CANCEL, indicator);
        }

        public static string indicator(bool withIndicator)
        {
            int indicator = withIndicator ? WITH_INDICATOR : NO_INDICATOR;
            return string.Format(Botled, indicator);
        }

        public static List<BluetoothLEDevice> LEDevicesList = new List<BluetoothLEDevice>();
        public async static Task<List<BluetoothLEDevice>> getLEDevices()
        {
            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);
                // Add the dvice name into the list.  
                LEDevicesList.Add(bleDevice);
            }
            return LEDevicesList;
        }

        public async static Task<bool> GetBluetoothIsEnabledAsync()
        {
            var radios = await Radio.GetRadiosAsync();
            var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
            return bluetoothRadio != null && bluetoothRadio.State == RadioState.On;
        }

        private static BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };
        public static void GetDiscoverableDevices()
        {
            watcher.Received += bluetoothFoundAsync;
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Start();
            //connectToAddress();
        }

        public static List<BluetoothLEDevice> ScannedDevicesList = new List<BluetoothLEDevice>();
        private static async void bluetoothFoundAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string deviceName = args.Advertisement.LocalName;
            string deviceAddress = args.BluetoothAddress.ToString();

            if (deviceName.Contains("Foldio"))
            {
                var bdevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                ScannedDevicesList.Add(bdevice);
                watcher.Stop();
            }
            else
            {

            }
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                                  new Action(delegate { }));
        }
    }
}

