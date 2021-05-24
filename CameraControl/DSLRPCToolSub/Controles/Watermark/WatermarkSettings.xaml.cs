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
using DSLR_Tool_PC.ViewModels;
using CameraControl.Devices;
using System.IO;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for WatermarkSettings.xaml
    /// </summary>
    public partial class WatermarkSettings : UserControl
    {
        Watermark wtrMrkImg = Watermark.GetInstance();

        public WatermarkSettings()
        {
            this.DataContext = wtrMrkImg;
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    dialog.Filter = "Image files (*.jpg,*.jpeg,*.jpe,*.jfif,*.png,*.bmp,*.tiff )|*.jpg;*.jpeg;*.jpe;*.jfif;*.png;*.bmp;*.tiff";
                    dialog.Title = "Watermark Image";
                    //dialog.DefaultExt = ".jpeg";
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        wtrMrkImg.ImagePath = dialog.FileName;
                        wtrMrkImg.ImageName = System.IO.Path.GetFileName(dialog.FileName);

                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
                        img.EndInit();

                        wtrMrkImg.ImageWidth = 100;
                        wtrMrkImg.ImageHeight = 100;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error set folder ", ex);
            }
        }

        private void btnCancleWatermark_Click(object sender, RoutedEventArgs e)
        {
            wtrMrkImg.Reset();
        }
    }
}
