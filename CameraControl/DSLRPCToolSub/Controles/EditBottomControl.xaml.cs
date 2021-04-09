using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using DSLR_Tool_PC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for EditBottomControl.xaml
    /// </summary>
    public partial class EditBottomControl : UserControl
    {
        EditBottomViewModel _editBottomViewModel = EditBottomViewModel.eb_Instance();
        public PathUpdate __Pathupdate { get; set; }
        private ImageDetails _selectedItem = null;

        private static EditBottomControl uni_class_inst = null;
        public static EditBottomControl getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new EditBottomControl();

            return uni_class_inst;
        }

        public EditBottomControl()
        {
            this.DataContext = _editBottomViewModel;
            InitializeComponent();

            __timer.Elapsed += __timerElapsed;
            ServiceProvider.WindowsManager.Event += Trigger_Event;
            ListBoxSnapshots.SelectionChanged += ListBoxSnapshots_SelectionChanged;
        }

        private void __timerElapsed(object sender, ElapsedEventArgs e)
        {
         
        }

        private void ListBoxSnapshots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    _selectedItem = e.AddedItems[0] as ImageDetails;
                    ImageDetails item = e.AddedItems[0] as ImageDetails;
                    if (item != null)
                    {
                        ListBoxSnapshots.ScrollIntoView(item);
                        ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.Path, BitmapLoader.LargeThumbSize, 0);
                        StaticClass.ImageListBoxSelectedItem = item;
                        
                        //EditLeftControl.getInstance().Trigger_Event("Fixed",null);
                    }
                }
            }
            catch (Exception)
            { 
            }
        }

        private void Trigger_Event(string cmd, object o)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => TriggerEvent(cmd, o)));
            }
            catch (Exception)
            {

            }
        }

        private void TriggerEvent(string cmd, object o)
        {
            try
            {
                __Pathupdate = PathUpdate.getInstance();
                switch (cmd)
                {

                    case WindowsCmdConsts.Next_Image:
                        if (ListBoxSnapshots.SelectedIndex < ListBoxSnapshots.Items.Count - 1)
                        {
                            ImageDetails item = ListBoxSnapshots.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                int ind = ListBoxSnapshots.Items.IndexOf(item);
                                ListBoxSnapshots.SelectedIndex = ind + 1;
                                StaticClass.ImageListBoxSelectedIndex = ind;
                            }
                            
                            item = ListBoxSnapshots.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                ListBoxSnapshots.ScrollIntoView(item);
                                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.Path, BitmapLoader.LargeThumbSize, 0);
                                __Pathupdate.PathImg = item.Path_Orginal;
                                StaticClass.ImageListBoxSelectedItem = ListBoxSnapshots.SelectedItem as ImageDetails;
                            }

                        }
                        break;
                    case WindowsCmdConsts.Prev_Image:
                        if (ListBoxSnapshots.SelectedIndex > 0)
                        {
                            ImageDetails item = ListBoxSnapshots.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                int ind = ListBoxSnapshots.Items.IndexOf(item);
                                ListBoxSnapshots.SelectedIndex = ind - 1;
                            }
                            StaticClass.ImageListBoxSelectedItem = ListBoxSnapshots.SelectedItem as ImageDetails;
                            item = ListBoxSnapshots.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                ListBoxSnapshots.ScrollIntoView(item);
                                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.Path, BitmapLoader.LargeThumbSize, 0);
                                __Pathupdate.PathImg = item.Path_Orginal; //clickedOnItem.Source.ToString();
                            }
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to process event ", exception);
            }

        }
        private DependencyObject GetParentDependencyObjectFromVisualTree(DependencyObject startObject, Type type)
        {
            //Walk the visual tree to get the parent of this control
            DependencyObject parent = startObject;
            while (parent != null)
            {
                if (type.IsInstanceOfType(parent))
                    break;
                else
                    parent = VisualTreeHelper.GetParent(parent);
            }

            return parent;
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //SelectedImage.Imagepath = ImageLIstBox.
            Image clickedOnItem = (Image)GetParentDependencyObjectFromVisualTree((DependencyObject)e.MouseDevice.DirectlyOver, typeof(Image));

            if (clickedOnItem != null)
            {
                __Pathupdate = PathUpdate.getInstance();
                __Pathupdate.PathImg = ((ImageDetails)clickedOnItem.DataContext).Path_Orginal; //clickedOnItem.Source.ToString();
                ServiceProvider.Settings.EditImageByte = DSLR_Tool_PC.StaticClass.ConvertImageToByteArray(__Pathupdate.PathImg);//.Substring(8)) ;// ; //converterDemo(clickedOnItem);
                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(__Pathupdate.PathImg, BitmapLoader.LargeThumbSize, 0);

                //PhotoEditModel.GetInstance().ImagePath = Pathupdate.PathImg.Substring(8);
                PhotoEditModel.GetInstance().ImageData = ServiceProvider.Settings.EditImageByte;
            } 
        }
        Timer __timer = new Timer(1000);
        public void CallTimer(bool _startTimer) { if (_startTimer) { __timer.Start(); } else { __timer.Stop(); } }
        public void CallMouseClick(object sender, MouseButtonEventArgs e)
        {
            //Image_MouseDown(sender, e);
            ImageDetails item = StaticClass.ImageListBoxSelectedItem;
            if (item != null)
            {
                int ind = ListBoxSnapshots.Items.IndexOf(item);
                ListBoxSnapshots.SelectedIndex = ind;
                ListBoxSnapshots.ScrollIntoView(item);
                ListBoxSnapshots.Focus();
                StaticClass.ImageListBoxSelectedIndex = ind;
            }
        }
    }
}
