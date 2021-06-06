using CameraControl;
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
    /// Interaction logic for Navigation.xaml
    /// </summary>
    public partial class Navigation : UserControl
    {
        static int count = 0;
        private static Navigation _Navigation_inst = null;
        public Navigation()
        {
            InitializeComponent();
            _Navigation_inst = this;
            TxtDegree.Text = "0°";
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // txtbyFrame.Text = "/0";

        }

        
        public static Navigation GetInstance()
        {
            if (_Navigation_inst == null)
            {
                _Navigation_inst = new Navigation();

            }
            return _Navigation_inst;
        }
        private void TxtFrame_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {

                if (e.Key == Key.Return)
                {
                    //count = 0;
                    int total = 0;
                    int txtFrameValue = Convert.ToInt32(TxtFrame.Text);
                    //count = txtFrameValue;
                    if (txtFrameValue != -1 && txtFrameValue != 0)
                    {
                        foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                        {
                            if (txtFrameValue > window.ListBoxSnapshots.Items.Count) { MessageBox.Show("Frame not found", "360 PC Tool", MessageBoxButton.OK, MessageBoxImage.Exclamation); }
                            window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(txtFrameValue - 1);
                            txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                            txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count) +"°";
                            total = 360 / window.ListBoxSnapshots.Items.Count;

                        }
                        TxtDegree.Text = string.Format(Convert.ToString((txtFrameValue-1) * total))+ "°";

                    }

                }
            }
            catch (Exception)
            {

                throw;
            }


        }

        private void SkipPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            //count = Convert.ToInt32(TxtFrame.Text);
            int txtframevalue = Convert.ToInt32(TxtFrame.Text);
            if (count == 0) { count=txtframevalue - 1; }
            try
            {
                foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                {
                    if (txtframevalue>1)
                    {
                        if (count==window.ListBoxSnapshots.Items.Count) { count--; }
                        //var ct = txtFrameValue - 1;
                        window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(txtframevalue-2);
                        txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                        // txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count);
                        //if (count == 23) { count = txtframevalue; }
                        int factor = 360 / window.ListBoxSnapshots.Items.Count;
                        TxtFrame.Text = Convert.ToString(txtframevalue-1);
                        TxtDegree.Text = Convert.ToString((txtframevalue - 2) * factor)+"°";
                        count--;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        private void SkipNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //count = Convert.ToInt32(TxtFrame.Text);
            int txtframevalue = Convert.ToInt32(TxtFrame.Text);
            try
            {
                foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                {
                    if (txtframevalue < window.ListBoxSnapshots.Items.Count)
                    {
                        //var ct = txtFrameValue - 1;
                        window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(txtframevalue);
                        txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                        // txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count);
                        int factor = 360 / window.ListBoxSnapshots.Items.Count;
                        TxtFrame.Text = Convert.ToString(txtframevalue + 1);
                        TxtDegree.Text = Convert.ToString(txtframevalue  * factor)+"°";
                        
                    }


                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
