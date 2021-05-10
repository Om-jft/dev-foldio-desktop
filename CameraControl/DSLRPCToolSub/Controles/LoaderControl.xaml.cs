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

namespace CameraControl.DSLRPCToolSub.Controles
{
    /// <summary>
    /// Interaction logic for LoaderControl.xaml
    /// </summary>
    public partial class LoaderControl : UserControl
    {
        public LoaderControl()
        {
            InitializeComponent();
        }
        public void showLoader()
        {
            this.Visibility = Visibility.Visible;
        }
        public void HideLoader()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
