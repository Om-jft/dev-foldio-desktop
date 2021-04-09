using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices.Classes;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for CameraControler.xaml
    /// </summary>
    public partial class CameraControler : UserControl
    {
        private bool _loading = false;
        public CameraControler()
        {
            InitializeComponent();
            if (ServiceProvider.DeviceManager != null)
                ServiceProvider.DeviceManager.PropertyChanged += DeviceManager_PropertyChanged;
            RefreshItems();
        }
        private void DeviceManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ServiceProvider.DeviceManager == null || ServiceProvider.DeviceManager.SelectedCameraDevice == null)
                return;
            if (e.PropertyName == "SelectedCameraDevice")
            {
                Dispatcher.Invoke(new Action(RefreshItems));
                var device = ServiceProvider.DeviceManager.SelectedCameraDevice as BaseCameraDevice;
                if (device != null) device.PropertyChanged += device_PropertyChanged;
            }
        }

        private void device_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_loading)
                return;
            if (sender == ServiceProvider.DeviceManager.SelectedCameraDevice && e.PropertyName == "CaptureInSdRam")
            {
                Dispatcher.BeginInvoke(new Action(RefreshItems));
            }
        }
        private void RefreshItems()
        {
            _loading = true;
            try
            {
                if (ServiceProvider.Settings == null)
                    return;
                if (ServiceProvider.DeviceManager.SelectedCameraDevice == null)
                    return;
                CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();

                cmb_transfer.Items.Clear();
                if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.CaptureInRam))
                {
                    cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem1);
                    cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem2);
                    cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem3);
                    if (ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam)
                        cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem1;
                    else if (!ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam && property.NoDownload)
                        cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem2;
                    else
                        cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem3;
                }
                else
                {
                    cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem2);
                    cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem3);
                    cmb_transfer.SelectedItem = property.NoDownload
                                                    ? TranslationStrings.LabelTransferItem2
                                                    : TranslationStrings.LabelTransferItem3;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error relod list ", e);
            }
            _loading = false;
        }
        private void ComBoxo1_Loaded(object sender, RoutedEventArgs e)
        {
            // ... A List.
            List<int> data = new List<int>();
            data.AddRange(Enumerable.Range(1, 20).ToList());

            // ... Get the ComboBox reference.
            var comboBox = sender as ComboBox;

            // ... Assign the ItemsSource to the List.
            comboBox.ItemsSource = data;

            // ... Make the first item selected.
            comboBox.SelectedIndex = 0;

        }

        private void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ... Get the ComboBox.
            var comboBox = sender as ComboBox;

            // ... Set SelectedItem as Window Title.
            string value = comboBox.SelectedItem as string;
            //this.Title = "Selected: " + value;
        }

        private void cmb_shutter_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            if (cmb != null && cmb.IsFocused)
            {
                //cmb.IsDropDownOpen = !cmb.IsDropDownOpen;
            }
        }
        private void cmb_transfer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading)
                return;
            if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
                return;
            CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();

            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem1 &&
                ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam != true)
                ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = true;

            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem2)
            {
                property.NoDownload = true;
                ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
            }
            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem3)
            {
                property.NoDownload = false;
                ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
            }
            property.CaptureInSdRam = ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam;
        }
    }
}
