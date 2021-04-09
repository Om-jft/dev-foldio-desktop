using DSLR_Tool_PC.ViewModels;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using CameraControl.Devices;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for TurnTableControl.xaml
    /// </summary>
    public partial class TurnTableControl : UserControl
    {
        TurnTableViewModel _turntablemodel = TurnTableViewModel.GetInstance();

        public TurnTableControl()
        {
            this.DataContext = _turntablemodel;
            InitializeComponent();
        }

        private void lbTodoList_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.lbTodoList.DataContext = _turntablemodel.FoldioItems;
        }

        private void TB_MoveLeft_Checked(object sender, RoutedEventArgs e)
        {
            //_turntablemodel.MoveLeft_with_Angle();
            //TB_MoveLeft.IsChecked = false;
        }

        private void Left_Rotation_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/click/Group 15.png"));
                //Left_Rotation.Background = brush;

                _turntablemodel.MoveLeft();
            }

        }

        private void Left_Rotation_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                var brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/none/Group 15.png"));
                //Left_Rotation.Background = brush;

                _turntablemodel.StopDeviceProcessing();
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }

        private void Left_Rotation_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) { }
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/none/Group 15.png"));
            //Left_Rotation.Background = brush;

            _turntablemodel.StopDeviceProcessing();
        }

        private void TB_Left_Checked(object sender, RoutedEventArgs e)
        {
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/click/Group 15.png"));
            //Left_Rotation.Background = brush;

            _turntablemodel.MoveLeft();
        }

        private void TB_Left_Unchecked(object sender, RoutedEventArgs e)
        {
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/none/Group 15.png"));
            //Left_Rotation.Background = brush;

            _turntablemodel.StopDeviceProcessing();
        }

        private void TB_Right_Checked(object sender, RoutedEventArgs e)
        {
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/click/Group 19 Copy 2.png"));
            //Left_Rotation.Background = brush;

            _turntablemodel.MoveRight();
        }

        private void TB_Right_Unchecked(object sender, RoutedEventArgs e)
        {
            var brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/DSLRPCToolSub/Assets/Images/none/Group 19.png"));
            //Left_Rotation.Background = brush;

            _turntablemodel.StopDeviceProcessing();
        }
    }
}
