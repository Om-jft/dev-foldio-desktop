using System.Windows.Controls;
using DSLR_Tool_PC.ViewModels;
using System.Text.RegularExpressions;
using CameraControl;
using System.Windows;
using System.Linq;
using System.Drawing;
using System.IO;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for PhotoEdit.xaml
    /// </summary>
    public partial class PhotoEdit : UserControl
    {
        PhotoEditModel _photoeditmodel = PhotoEditModel.GetInstance();

        public PhotoEdit()
        {
            this.DataContext = _photoeditmodel;
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void sldBackground_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var slidervalue = sldBackground.Value;
            //ImageLIstBox_Folder

            //foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
            //{
            //    window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(txtFrameValue - 1);
            //    Image img = window.EditFramePicEdit.i;
            //    Background _bg = new Background();
            //    BlurBitmapEffect myBlurEffect = new BlurBitmapEffect();
            //    myBlurEffect.Radius = sldBackground.Value;
            //    myBlurEffect.KernelType = KernelType.Box;
            //    Bitmap capcha = new Bitmap(window.EditFramePicEdit.Source.ToString());
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
