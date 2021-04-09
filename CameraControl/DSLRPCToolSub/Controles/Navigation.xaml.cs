using CameraControl;
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
        public Navigation()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // txtbyFrame.Text = "/0";

        }


        private void TxtFrame_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {

                if (e.Key == Key.Return)
                {
                    count = 0;
                    int txtFrameValue = Convert.ToInt32(TxtFrame.Text);
                    count = txtFrameValue;
                    if (txtFrameValue != -1 && txtFrameValue != 0)
                    {
                        foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                        {
                            window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(txtFrameValue - 1);
                            txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                            txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count);

                        }
                        TxtDegree.Text = string.Format(Convert.ToString(txtFrameValue * 10));

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
            try
            {
                foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                {
                    if (count >= 1)
                    {
                        //var ct = txtFrameValue - 1;
                        window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(count - 1);
                        txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                        // txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count);
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
            try
            {
                foreach (MainWindowAdvanced window in Application.Current.Windows.OfType<MainWindowAdvanced>())
                {
                    if (count >= 0 && count < window.ListBoxSnapshots.Items.Count)
                    {
                        //var ct = txtFrameValue - 1;
                        window.ListBoxSnapshots.SelectedItem = window.ListBoxSnapshots.Items.GetItemAt(count);
                        txtbyFrame.Text = "/" + window.ListBoxSnapshots.Items.Count;
                        // txtFramedistance.Text = Convert.ToString(360 / window.ListBoxSnapshots.Items.Count);
                        count++;
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
