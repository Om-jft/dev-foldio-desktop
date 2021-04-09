using DSLR_Tool_PC.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for ExportGIF.xaml
    /// </summary>


    public partial class ExportGIF : UserControl
    {
        public ExportGIFModel exportGIFmodel = ExportGIFModel.getInstance();
        public ExportGIF()
        {
            this.DataContext = exportGIFmodel;
            InitializeComponent();
        }
        private void ExportGIF_Click(object sender, RoutedEventArgs e)
        {
            exportGIFmodel.ProduceGIF();
        }
        private void GifPlayTime_LostFocus(object sender, RoutedEventArgs e)
        {
            exportGIFmodel.FrameDelayTimer();
        }
    }
}

