using DSLR_Tool_PC.ViewModels;
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
    /// Interaction logic for ExportZIP.xaml
    /// </summary>
    public partial class ExportZIP : UserControl
    {
        
        public ExportZipModel exportZipModel = ExportZipModel.getInstance();
        public ExportZIP()
        {
            this.DataContext = exportZipModel;

            InitializeComponent();
            cmbFileExtensions.SelectedIndex = 0;
        }

        private void ExportZipButton_Click(object sender, RoutedEventArgs e)
        {
            exportZipModel.ProduceZIP();
        }

        private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // exportZipModel.SelectedImageZip_Preview=
            //var selected_item = ImageList.SelectedItem;
            //if (selected_item != null)
            //{
            //    exportZipModel.SelectedImageZip_Preview = ((ImageDetails)selected_item);
            //}
        }
       
        private void SelectedZipCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {            
            exportZipModel.CountSelectedImages();
            exportZipModel.SelectedImageZip_Preview = null;
        }

        private void SelectedZipCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            exportZipModel.CountSelectedImages();
            var chkbox = (CheckBox)sender; 
            if (chkbox.IsChecked == true)
            {
                var selected_item = chkbox.DataContext;
                if (selected_item != null)
                {
                    exportZipModel.SelectedImageZip_Preview = ((ImageDetails)selected_item);
                }
            }
        }

        private void ImageFilm_Checked(object sender, RoutedEventArgs e)
        {
            exportZipModel.ImageFilm = true;
        }

        private void ImageFilm_Unchecked(object sender, RoutedEventArgs e)
        {
            exportZipModel.ImageFilm = false;
        }

        private void cmbFileExtensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exportZipModel.SelectedFileExtension = exportZipModel._selectedFileExtension = ((System.Windows.Controls.ContentControl)cmbFileExtensions.SelectedItem).Content.ToString();
        }
    }
}
