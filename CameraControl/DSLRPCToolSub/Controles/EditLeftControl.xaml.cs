using CameraControl.Core.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Devices;
using System.IO;
using DSLR_Tool_PC.ViewModels;
using CameraControl.Core;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Win32;

namespace DSLR_Tool_PC.Controles
{
    /// <summary>
    /// Interaction logic for EditLeftControl.xaml
    /// </summary>
    public partial class EditLeftControl : UserControl
    {

        List<ImageDetails> images_History = new List<ImageDetails>();
        List<ImageDetails> images_Folder = new List<ImageDetails>();
        public PathUpdate __Pathupdate { get; set; }

        public PhotoEditModel __photoEditModel { get; set; }

        public EditLeftControl()
        {
            ServiceProvider.WindowsManager.Event += Trigger_Event;
            //foreach (string myFile in Directory.GetFiles(@"E:\Orange Monkey\History"))
            //{
            //    System.Windows.Controls.Image myLocalImage = new System.Windows.Controls.Image();
            //    myLocalImage.Height = 120.8;
            //    myLocalImage.Width = 214.8;
            //    myLocalImage.Margin = new Thickness(5, 0, 5, 0);


            //    BitmapImage myImageSource = new BitmapImage();
            //    myImageSource.BeginInit();
            //    myImageSource.UriSource = new Uri(@"file:///" + myFile);

            //    myImageSource.EndInit();
            //    myLocalImage.Source = myImageSource;

            //    //filePath.Add(myFile);
            //    ImageLIstBox.Items.Add(myLocalImage);
            //    //ImageLog.Items.Add(myLocalImage);
            //}
            DataContext = this;
            InitializeComponent();

            ServiceProvider.Settings.EditImageByte = null;
            string root = "";
            try
            {
                root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "OrangeMonkie", "History");
                if (!Directory.Exists(root)) { Directory.CreateDirectory(root); }
            }
            catch (Exception exception)
            {
                Log.Error("Error set My pictures folder", exception);
                root = "C:\\";
            }
            //string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
            {
                root = System.IO.Path.GetDirectoryName(@"D:/Pics");
            }

            string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };
            var files = Directory.GetFiles(root, "*.*").Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));
            foreach (var file in files)
            {
                ImageDetails id = new ImageDetails()
                {
                    Path = file,
                    FileName = System.IO.Path.GetFileName(file),
                    Extension = System.IO.Path.GetExtension(file),
                    DateModified = System.IO.File.GetCreationTime(file).ToString("yyyy-MM-dd"),
                    CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
                    TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
                };

                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(file, UriKind.Absolute);

                img.EndInit();

                id.Width = img.PixelWidth;
                id.Height = img.PixelHeight;

                // I couldn't find file size in BitmapImage
                System.IO.FileInfo fi = new System.IO.FileInfo(file);
                id.Size = fi.Length;
                images_History.Add(id);
                Thread.Sleep(100);
            }

            images_History.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));
            EditBottomViewModel.eb_Instance().SelectedFolderFiles = images_History;

            foreach (var img1 in images_History)
            {
                ImageLIstBox.Items.Add(img1);
                ImageListBox.Items.Add(img1);
            }

            IntiService();
        }
        private void IntiService()
        {
            __photoEditModel = PhotoEditModel.GetInstance();
            __photoEditModel.ExecuteInti(this);
        }

        private void btn_Folderbrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                //dialog.SelectedPath = Session.Folder;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbFolderName.Text = dialog.SelectedPath;
                    tbFolderName_thmb.Text = dialog.SelectedPath;
                    BrowseFolderImages();
                }
            }
            catch (Exception ex)
            {
                //this.ShowMessageAsync(TranslationStrings.LabelError, TranslationStrings.LabelErrorSetFolder);
                Log.Error("Error set folder ", ex);
            }



        }

        public void BrowseFolderImages()
        {
            try
            {
                images_Folder = new List<ImageDetails>();

                string root = System.IO.Path.GetDirectoryName(tbFolderName.Text);//System.Reflection.Assembly.GetExecutingAssembly().Location);
                string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };

                var files = Directory.GetFiles(tbFolderName.Text).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

                string tempfolder = "";
                try
                {
                    tempfolder = Path.Combine(Settings.ApplicationTempFolder, "ECL_" + DateTime.Now.ToString("ddMMyyyy"));
                    if (Directory.Exists(tempfolder))
                        Directory.Delete(tempfolder, true);
                }
                catch (Exception)
                {
                    tempfolder = Path.Combine(Settings.ApplicationTempFolder, "ECL_" + DateTime.Now.ToString("yyyyMMdd"));
                    if (Directory.Exists(tempfolder))
                        Directory.Delete(tempfolder, true);
                }
                Directory.CreateDirectory(tempfolder);

                foreach (var f in files)
                {
                    var file = Path.Combine(tempfolder, Path.GetFileName(f));
                    StaticClass.GenerateSmallThumb(f, file);

                    ImageDetails id = new ImageDetails()
                    {
                        Path_Orginal = f,
                        Path = file,
                        FileName = System.IO.Path.GetFileName(file),
                        Extension = System.IO.Path.GetExtension(file),
                        DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd"),
                        CreationDateTime = System.IO.File.GetCreationTimeUtc(file),
                        TimeModified = System.IO.File.GetCreationTime(file).ToString("HH:mm:ss:ffffff")
                    };
                    images_Folder.Add(id);
                    Thread.Sleep(10);
                }

                images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime)));
                EditBottomViewModel.eb_Instance().SelectedFolderFiles = images_Folder;

                ImageListBox_Folder.Items.Clear();
                ImageLIstBox_Folder.Items.Clear();
                foreach (var img in images_Folder)
                {
                    ImageLIstBox_Folder.Items.Add(img);
                    ImageListBox_Folder.Items.Add(img);
                }
            }
            catch (Exception ex) { Log.Debug("BrowseFolderImages", ex); }
        }
        private bool ThumbnailCallback()
        {
            return false;
        }

        private void ImgViewGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //SelectedImage.Imagepath = ImageLIstBox.
            Image clickedOnItem = (Image)GetParentDependencyObjectFromVisualTree((DependencyObject)e.MouseDevice.DirectlyOver, typeof(Image));

            if (clickedOnItem != null)
            {
                __Pathupdate = PathUpdate.getInstance();
                __Pathupdate.PathImg = ((ImageDetails)clickedOnItem.DataContext).Path_Orginal; //clickedOnItem.Source.ToString();
                ServiceProvider.Settings.EditImageByte = DSLR_Tool_PC.StaticClass.ConvertImageToByteArray(__Pathupdate.PathImg);//.Substring(8)) ;// ; //converterDemo(clickedOnItem);
                PhotoUtils.WaitForFile(__Pathupdate.PathImg);
                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(__Pathupdate.PathImg, BitmapLoader.LargeThumbSize, 0);

                //PhotoEditModel.GetInstance().ImagePath = Pathupdate.PathImg.Substring(8);
                PhotoEditModel.GetInstance().ImageData = ServiceProvider.Settings.EditImageByte;
                StaticClass.ImageListBoxSelectedItem = (ImageDetails)clickedOnItem.DataContext;

                EditBottomControl.getInstance().CallMouseClick(sender, e);
            }
        }

        public byte[] converterDemo(Image x)
        {
            System.Drawing.ImageConverter _imageConverter = new System.Drawing.ImageConverter();
            byte[] xByte = (byte[])_imageConverter.ConvertTo(x, typeof(byte[]));
            return xByte;
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

        private static EditLeftControl uni_class_inst = null;
        public static EditLeftControl getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new EditLeftControl();

            return uni_class_inst;
        }


        public void Trigger_Event(string cmd, object o)
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
                bool status = false;
                ListBox lstBox = null;
                if (tab_folder_VL.IsSelected) { lstBox = ImageListBox_Folder; status = true; }
                if (tab_folder_VG.IsSelected) { lstBox = ImageLIstBox_Folder; status = true; ImageLIstBox_Folder.SelectedIndex = StaticClass.ImageListBoxSelectedIndex + 1; ImageLIstBox_Folder.ScrollIntoView(StaticClass.ImageListBoxSelectedItem); }
                if (tab_history_VL.IsSelected) { lstBox = ImageListBox; status = true; }
                if (tab_history_VG.IsSelected) { lstBox = ImageLIstBox; status = true; }

                if (status == false) { lstBox = ImageLIstBox_Folder; }

                __Pathupdate = PathUpdate.getInstance();
                switch (cmd)
                {

                    case WindowsCmdConsts.Next_Image:
                        if (lstBox.SelectedIndex < lstBox.Items.Count - 1)
                        {
                            ImageDetails item = lstBox.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                int ind = lstBox.Items.IndexOf(item);
                                lstBox.SelectedIndex = ind + 1;
                            }
                            item = lstBox.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                lstBox.ScrollIntoView(item);
                                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.Path, BitmapLoader.LargeThumbSize, 0);
                                __Pathupdate.PathImg = item.Path_Orginal;
                            }

                        }
                        break;
                    case WindowsCmdConsts.Prev_Image:
                        if (lstBox.SelectedIndex > 0)
                        {
                            ImageDetails item = lstBox.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                int ind = lstBox.Items.IndexOf(item);
                                lstBox.SelectedIndex = ind - 1;
                            }
                            item = lstBox.SelectedItem as ImageDetails;
                            if (item != null)
                            {
                                lstBox.ScrollIntoView(item);
                                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = (WriteableBitmap)BitmapLoader.Instance.LoadImage(item.Path, BitmapLoader.LargeThumbSize, 0);
                                __Pathupdate.PathImg = item.Path_Orginal; //clickedOnItem.Source.ToString();
                            }
                        }
                        else
                        {
                            if (StaticClass.ImageListBoxSelectedItem != null)
                            {
                                ImageDetails item = StaticClass.ImageListBoxSelectedItem;
                                if (item != null)
                                {
                                    int ind = lstBox.Items.IndexOf(item);
                                    lstBox.SelectedIndex = ind - 1;
                                }
                            }
                        }
                        break;
                    case "Fixed":
                        if (lstBox.Items.Count > 0)
                        {
                            ImageDetails item = StaticClass.ImageListBoxSelectedItem;
                            if (item != null)
                            {
                                int ind = lstBox.Items.IndexOf(item);
                                lstBox.SelectedIndex = ind;
                            }
                            item = lstBox.SelectedItem as ImageDetails;
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to process event ", exception);
            }

        }

        public void CallMouseClick(object sender, MouseButtonEventArgs e)
        {
            //Image_MouseDown(sender, e);
            ImageDetails item = StaticClass.ImageListBoxSelectedItem;
            //if (item != null)
            //{
            //    int ind = ListBoxSnapshots.Items.IndexOf(item);
            //    ListBoxSnapshots.SelectedIndex = ind - 1;
            //    ListBoxSnapshots.ScrollIntoView(item);
            //    StaticClass.ImageListBoxSelectedIndex = ind;
            //}
        }

        private void rbDec_Checked(object sender, RoutedEventArgs e)
        {
            ReloadImages(true);
        }

        private void rbDec_Unchecked(object sender, RoutedEventArgs e)
        {
            ReloadImages(false);
        }

        private void ReloadImages(bool _StatusOrder)
        {
            if (tab_folder.IsSelected)
            {
                if (_StatusOrder == false) { images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.CreationDateTime), Convert.ToDateTime(y.CreationDateTime))); }
                if (_StatusOrder == true) { images_Folder.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(y.CreationDateTime), Convert.ToDateTime(x.CreationDateTime))); }

                EditBottomViewModel.eb_Instance().SelectedFolderFiles = images_Folder;
                ImageListBox_Folder.Items.Clear();
                ImageLIstBox_Folder.Items.Clear();
                foreach (var img in images_Folder)
                {
                    ImageLIstBox_Folder.Items.Add(img);
                    ImageListBox_Folder.Items.Add(img);
                }
                EditBottomViewModel.eb_Instance().SelectedFolderFiles = images_Folder;
            }
            if (tab_history.IsSelected)
            {
                if (_StatusOrder == true) { }
                if (_StatusOrder == false) { }
            }
        }
    }
}
