using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using DSLR_Tool_PC.Classes;
using DSLR_Tool_PC.ViewModels;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.DSLRPCToolSub.ViewModels
{
    public class EditLeftControlViewModel : BaseFieldClass
    {
        private static EditLeftControlViewModel uni_class_inst = null;
        public RelayCommand ButtonBrowseClick { get; set; }

        public static EditLeftControlViewModel getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new EditLeftControlViewModel();

            return uni_class_inst;
        }

        public EditLeftControlViewModel()
        {
            ButtonBrowseClick = new RelayCommand(FolderBrowseEvent);

            IntializeEvents();
        }

        private ObservableRangeCollection<ImageDetails> _imageLIstBox_Folder = new ObservableRangeCollection<ImageDetails>();
        public ObservableRangeCollection<ImageDetails> ImgList_Folder
        {
            get { return _imageLIstBox_Folder; }
            set
            {
                _imageLIstBox_Folder = value;
                NotifyPropertyChanged("ImgList_Folder");
            }
        }

        private string _folderName = "";
        public string FolderName
        {
            get { return _folderName; }
            set
            {
                _folderName = value;
                NotifyPropertyChanged("FolderName");
            }
        }

        public void IntializeEvents()
        {
            ServiceProvider.Settings.EditImageByte = null;
            string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
            {
                root = System.IO.Path.GetDirectoryName(@"D:/Pics");
            }

            string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };
            var files = Directory.GetFiles(System.IO.Path.Combine(root, "Pics"), "*.*").Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));
            foreach (var file in files)
            {
                ImageDetails id = new ImageDetails()
                {
                    Path = file,
                    FileName = System.IO.Path.GetFileName(file),
                    Extension = System.IO.Path.GetExtension(file),
                    DateModified = System.IO.File.GetCreationTime(file).ToString("yyyy-MM-dd")

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
                //images.Add(id);
            }

            //images.Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.DateModified), Convert.ToDateTime(y.DateModified)));

            //foreach (var img1 in images)
            //{
            //    ImageLIstBox.Items.Add(img1);
            //    ImageListBox.Items.Add(img1);
            //}
        }

        private void FolderBrowseEvent()
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog(); 
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _imageLIstBox_Folder = new ObservableRangeCollection<ImageDetails>();
                    FolderName = dialog.SelectedPath; 
                    string root = System.IO.Path.GetDirectoryName(dialog.SelectedPath);//System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };

                    var files = Directory.GetFiles(FolderName).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

                    var tempFolder = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName());
                    File.Delete(tempFolder);
                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder);
                    Directory.CreateDirectory(tempFolder);

                    foreach (var f in files)
                    {
                        var file = Path.Combine(tempFolder, Path.GetFileName(f));
                        File.Copy(f, file);

                        ImageDetails id = new ImageDetails()
                        {
                            Path_Orginal = f,
                            Path = file,
                            FileName = System.IO.Path.GetFileName(file),
                            Extension = System.IO.Path.GetExtension(file),
                            DateModified = (System.IO.File.GetCreationTime(file)).ToString("yyyy-MM-dd")
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
                        _imageLIstBox_Folder.Add(id);
                    }
                    
                    //_imageLIstBox_Folder .Sort((x, y) => DateTime.Compare(Convert.ToDateTime(x.DateModified), Convert.ToDateTime(y.DateModified)));

                    //foreach (var img in _imageLIstBox_Folder)
                    //{
                    //    ImageLIstBox_Folder.Items.Add(img);
                    //    ImageListBox_Folder.Items.Add(img);
                    //}
                }
            }
            catch (Exception ex)
            {
                //this.ShowMessageAsync(TranslationStrings.LabelError, TranslationStrings.LabelErrorSetFolder);
                //Log.Error("Error set folder ", ex);
            }
        }
    }
}
