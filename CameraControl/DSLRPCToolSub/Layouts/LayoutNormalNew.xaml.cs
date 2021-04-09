#region

using CameraControl.ViewModel;

#endregion

namespace DSLR_Tool_PC.Layouts
{
    /// <summary>
    /// Interaction logic for LayoutNormal.xaml
    /// </summary>
    public partial class LayoutNormalNew
    {
        public LayoutNormalNew()
        {
            InitializeComponent();
            ImageLIst = ImageLIstBox;
            ZoomAndPanControl = zoomAndPanControl;
            LayoutViewModel = (LayoutViewModel)ZoomAndPanControl.DataContext;
            MediaElement = VideoControl;
            content = Image;
            InitServices();
            //zoombox.RelativeZoomModifiers.Clear();
            //zoombox.RelativeZoomModifiers.Add(KeyModifier.None);
            //zoombox.DragModifiers.Clear();
            //zoombox.DragModifiers.Add(KeyModifier.None);
            //zoombox.KeepContentInBounds = true;
        }
    }
}