using DSLR_Tool_PC.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for ExportMP4.xaml
    /// </summary>
    public partial class ExportMP4 : UserControl
    {
        public ExportMP4ViewModel _EMP4Model = ExportMP4ViewModel.getInstance();
        public ExportMP4()
        {
            this.DataContext = _EMP4Model;
            InitializeComponent();
        }
         
    }
}
