using BumpKit;
using DSLR_Tool_PC.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using System.Drawing;
using System.IO.Compression;
using System.Collections.Concurrent;


namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for ExportPopup.xaml
    /// </summary>
   
    public partial class ExportPopup : Window
    { 
        public ExportPopup()
        {
            InitializeComponent();
        }

        private void TabExport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ExportGIFModel.getInstance().URIPathImgGIF = "";
            ExportZipModel.getInstance().InitialImagePath = "";
            ExportMP4ViewModel.getInstance().URIPathImgGIF = "";
            if ((e.OriginalSource == TabExport))
            {
                if (TabExport.SelectedIndex == 0)
                {
                    if (PathUpdate.getInstance().PathImg != null)
                    {
                        ExportGIFModel.getInstance().URIPathImgGIF = PathUpdate.getInstance().PathImg;
                        ExportGIFModel.getInstance().__Parent_window = this;
                    }
                }
                else if (TabExport.SelectedIndex == 1)
                {
                    if (PathUpdate.getInstance().PathImg != null)
                    {
                        ExportZipModel.getInstance().InitialImagePath = PathUpdate.getInstance().PathImg;
                        ExportZipModel.getInstance().__Parent_window = this;
                    }
                }
                else if (TabExport.SelectedIndex == 2)
                {
                    if (PathUpdate.getInstance().PathImg != null)
                    {
                        ExportMP4ViewModel.getInstance().URIPathImgGIF = PathUpdate.getInstance().PathImg;
                        ExportMP4ViewModel.getInstance().__Parent_window = this;
                    }
                }
            }
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}

