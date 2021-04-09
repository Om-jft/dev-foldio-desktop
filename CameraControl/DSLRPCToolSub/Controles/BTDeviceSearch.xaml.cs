using CameraControl.DSLRPCToolSub.Classes;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;


namespace CameraControl.DSLRPCToolSub.Controles
{
    /// <summary>
    /// Interaction logic for BTDeviceSearch.xaml
    /// </summary>
    public partial class BTDeviceSearch : Window
    {
        private static BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active 
        };
        public BTDeviceSearch()
        {
            InitializeComponent();
            GetDiscoverableDevices();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        DispatcherTimer __Tmr_SearchingFoldio = new DispatcherTimer();
        public void GetDiscoverableDevices()
        {
            try
            {
                ScannedDevicesList.Clear();

                __Tmr_SearchingFoldio.Tick += new EventHandler(dispatcherTimer_Tick);
                __Tmr_SearchingFoldio.Interval = new TimeSpan(0, 0, 1);
                __Tmr_SearchingFoldio.Start();

                watcher.Received += bluetoothFoundAsync;
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.Start();
                //connectToAddress();
            }
            catch (Exception){}

        }

        List<TodoItem> FoldioItems = new List<TodoItem>();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (ScannedDevicesList.Count > 0)
            {
                foreach (var itm in ScannedDevicesList)
                {
                    string deviceName = itm.Name.ToString();
                    string deviceAddress = itm.BluetoothAddress.ToString();

                    TodoItem result = FoldioItems.Find(x => x.DeviceAddress == deviceAddress);
                    if (result == null)
                    {
                        List<TodoItem> _Item = new List<TodoItem>();
                        _Item.Add(new TodoItem() { Title = deviceName, DeviceAddress = deviceAddress });
                        lb_BTDevicesSearch.Items.Add(_Item);

                        FoldioItems.Add(new TodoItem() { Title = deviceName, DeviceAddress = deviceAddress });
                        break;
                    }
                }
            }
        }

        public static List<BluetoothLEDevice> ScannedDevicesList = new List<BluetoothLEDevice>();
        private static async void bluetoothFoundAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string deviceName = args.Advertisement.LocalName;
            string deviceAddress = args.BluetoothAddress.ToString();

            if (deviceName.Contains("Foldio"))
            {
                var bdevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                BluetoothLEDevice result = ScannedDevicesList.Find(x => x.BluetoothAddress.ToString() == deviceAddress);
                if (result == null )
                {
                    if (bdevice.DeviceInformation.Pairing.IsPaired == true)
                    {
                        ScannedDevicesList.Add(bdevice);
                    }
                }
            }
            else
            {

            }
        }

        private void lb_BTDevicesSearch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int SelectedIndex = lb_BTDevicesSearch.SelectedIndex;
            if (SelectedIndex < 0) { return; }
            ulong devAddress = Convert.ToUInt64(FoldioItems[SelectedIndex].DeviceAddress);
            string devName = FoldioItems[SelectedIndex].Title.ToString();
            FindBluetoothDevice(devAddress);
        }

        private async void FindBluetoothDevice(ulong deviceAddress)
        {
            CurrentSelectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
            if (CurrentSelectedDevice != null) { this.Close(); }
        }

        public BluetoothLEDevice CurrentSelectedDevice { get; set; }
        public async void selectDevice(string deviceName, string deviceAddress)
        {
            //This works only if your device is already paired!
            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);
                // Check if the name of the device founded is EMU Bridge.  
                if (bleDevice.Name == deviceName && bleDevice.BluetoothAddress.ToString() == deviceAddress)
                {
                    // Save detected device in the current device variable.  
                    CurrentSelectedDevice = bleDevice;
                    break;
                }
            }
        }
    }
    public class TodoItem
    {
        public string Title { get; set; }
        public string DeviceAddress { get; set; }
        //public int Completion { get; set; }
    }
}
