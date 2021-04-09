#region Licence

// Distributed under MIT License
// ===========================================================
// 
// digiCamControl - DSLR camera remote control open source software
// Copyright (C) 2014 Duka Istvan
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY,FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

#region

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Classes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using DSLR_Tool_PC;
using DSLR_Tool_PC.ViewModels;

#endregion

namespace CameraControl.Controls
{
    /// <summary>
    /// Interaction logic for Controler.xaml
    /// </summary>
    public partial class Controler : UserControl
    {
        LVViewModel __lVViewModel = LVViewModel.lvInstance();
        private bool _loading = false;

        public Controler()
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

            property.NoDownload = false;
            if (ServiceProvider.Settings.CaptureInSdRamFolder != null && ServiceProvider.Settings.CaptureInSdRamFolder != "")
            {
                if (ServiceProvider.Settings.DefaultSession.Folder != ServiceProvider.Settings.CaptureInSdRamFolder)
                {
                    ServiceProvider.Settings.DefaultSession.Folder = System.IO.Path.Combine(ServiceProvider.Settings.CaptureInSdRamFolder, ServiceProvider.Settings.DefaultSession.Name);
                }
            }
            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem1 && StaticClass.__CaptureInSdRam != true)
            {
                StaticClass.__CaptureInSdRam = true;
            }

            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem2)
            {
                property.NoDownload = true;
                StaticClass.__CaptureInSdRam = false;
            }
            if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem3)
            {
                property.NoDownload = false;
                StaticClass.__CaptureInSdRam = false;
            }
            property.CaptureInSdRam = StaticClass.__CaptureInSdRam;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy) { return; }

                string cmd = ((Button)sender).Name.ToString();

                StaticClass.CameraParametersChanging(cmd);
            }
            catch (Exception)
            {
            }
        }

        private void cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy) { return; }

            string cmd = Convert.ToString(((ComboBox)sender).SelectedValue);
            if (cmd == "") { return; }
            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    if (cmd == "M")
                    {
                        __lVViewModel.StopLiveView();
                        __lVViewModel.StartLiveView();
                    }
                    __lVViewModel.StopStartLievView();
                }
                catch (Exception ex)
                {
                    Log.Debug("cmb_SelectionChanged", ex);
                }
            }));
        }

        private void TransferNext_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_transfer.Items.Count > 0)
            {
                int ind = cmb_transfer.SelectedIndex;
                if (ind < 0)
                    return;
                ind--;
                if (ind < cmb_transfer.Items.Count && ind >= 0)
                    cmb_transfer.SelectedIndex = ind;
            }
        }

        private void TransferPrev_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_transfer.Items.Count > 0)
            {
                int ind = cmb_transfer.SelectedIndex;
                if (ind < 0)
                    return;
                ind++;
                if (ind < cmb_transfer.Items.Count)
                    cmb_transfer.SelectedIndex = ind;
            }
        } 
        private void cmb_transfer_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                if (_loading)
                    return;
                if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
                    return;
                if (cmb_transfer.SelectedIndex == 0)
                {
                    System.Windows.Forms.FolderBrowserDialog _saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
                    _saveFileDialog.ShowDialog();
                    if (_saveFileDialog.SelectedPath == "") { return; }

                    ServiceProvider.Settings.CaptureInSdRamFolder = _saveFileDialog.SelectedPath.ToString();
                    ServiceProvider.Settings.DefaultSession.Folder = System.IO.Path.Combine(_saveFileDialog.SelectedPath, ServiceProvider.Settings.DefaultSession.Name);
                }
            }
            catch (Exception) { }
        }
    }
}