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

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for WatermarkImage.xaml
    /// </summary>
    public partial class WatermarkImage : UserControl
    {
        Watermark wtrMrkImg = Watermark.GetInstance();

        public WatermarkImage()
        {
            this.DataContext = wtrMrkImg;
            InitializeComponent();
        }
    }
}
