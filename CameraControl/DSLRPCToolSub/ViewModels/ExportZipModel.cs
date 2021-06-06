using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Media.Imaging;
using DSLR_Tool_PC.Classes;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using CameraControl.Devices;
using CameraControl.Core.Classes;
using CameraControl;
using System.ComponentModel;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using CameraControl.DSLRPCToolSub.ViewModels;

namespace DSLR_Tool_PC.ViewModels
{
    public partial class ExportZipModel : BaseFieldClass
    {
        DSLR_Tool_PC.ViewModels.Watermark watermarkName = DSLR_Tool_PC.ViewModels.Watermark.GetInstance();
        public Window __Parent_window = null;
        public int ratiowidthzip = 0;
        public int ratioheightzip = 0;
        private ObservableRangeCollection<ImageDetails> _imagesZip = new ObservableRangeCollection<ImageDetails>();
        private readonly HashSet<string> _fileExtensions = new HashSet<string>();

        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
            //photoEdit = (PhotoEdit)__this;
        }

        private static ExportZipModel uni_class_inst = null;
        private ImageDetails _selectedImageZip;
        private ImageDetails _SelectedImageZipPreview;
        private double _exportwidthzip = 0;
        private double _exportheightzip = 0;
        private int _zipimagecount = 0;
        private ObservableRangeCollection<string> _fileszip = new ObservableRangeCollection<string>();
        private bool _imgfilmchecked = false;
        private bool successful = false;
        private System.Windows.Forms.FolderBrowserDialog _saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
        private string tempPathFolder = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName());
        private string zipPath = null;

        private static bool bgInitializer = false;

        BackgroundWorker bgWorker = new BackgroundWorker();
        private int count = 0;
        private int total = 0;
        #region Properties

        public bool ImageFilm
        {
            get { return _imgfilmchecked; }
            set
            {
                if (_imgfilmchecked != value)
                {
                    _imgfilmchecked = value;
                    NotifyPropertyChanged("ImageFilm");

                }
            }
        }
        public HashSet<string> FileExtensions
        {
            get
            {
                return _fileExtensions;
            }
        }

        public string _selectedFileExtension = "";
        public string SelectedFileExtension
        {
            get { return _selectedFileExtension; }
            set { _selectedFileExtension = value; }
        }

        public int ZipImageCount
        {
            get { return _zipimagecount; }
            set
            {
                if (_zipimagecount != value)
                {
                    _zipimagecount = value;
                    NotifyPropertyChanged("ZipImageCount");
                }

            }
        }
        public ImageDetails SelectedImageZip
        {
            get { return _selectedImageZip; }
            set
            {
                if (_selectedImageZip != value)
                {
                    _selectedImageZip = value;
                    NotifyPropertyChanged("SelectedImageZip");
                    //  CountSelectedImages();
                    //       NotifyPropertyChanged("ZipImageCount");

                    //  InitializeImage();
                    // CollectImages();
                }
            }
        }

        private string _initialImagePath = "";
        public string InitialImagePath
        {
            get { return _initialImagePath; }
            set
            {
                if (_initialImagePath != value)
                {
                    _initialImagePath = value;
                    // NotifyPropertyChanged("SelectedImageZip");
                    InitializeImage();
                    CollectImages();
                }
            }
        }

        public ImageDetails SelectedImageZip_Preview
        {
            get { return _SelectedImageZipPreview; }
            set
            {

                _SelectedImageZipPreview = value;
                NotifyPropertyChanged("SelectedImageZip_Preview");

            }
        }

        public double ExportWidthZip
        {
            get { return _exportwidthzip; }
            set
            {
                if (_exportwidthzip != value)
                {
                    _exportwidthzip = value;
                    NotifyPropertyChanged("ExportWidthZip");
                    _exportheightzip = (ExportWidthZip / ratiowidthzip) * ratioheightzip;
                    NotifyPropertyChanged("ExportHeightZip");
                }
            }
        }

        public string ShortPathZip { get; set; }

        public System.IO.DirectoryInfo FolderNameZip { get; set; }

        public string TargetFolderZip { get; set; }

        public ObservableRangeCollection<ImageDetails> ImagesZip
        {
            get { return _imagesZip; }
            set
            {
                if (_imagesZip != value)
                {
                    _imagesZip = value;
                    NotifyPropertyChanged("ImagesZip");

                }

            }
        }

        public double ExportHeightZip
        {
            get { return _exportheightzip; }
            set
            {
                if (_exportheightzip != value)
                {
                    _exportheightzip = value;
                    NotifyPropertyChanged("ExportHeightZip");
                    _exportwidthzip = (ExportHeightZip / ratioheightzip) * ratiowidthzip;
                    NotifyPropertyChanged("ExportWidthZip");

                }
            }
        }

        #endregion
        public static ExportZipModel getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new ExportZipModel();
           
            return uni_class_inst;
        }

        private void InitializeImage()
        {
            if (InitialImagePath == "")
            {
                return;
            }

            SelectedImageZip_Preview = SelectedImageZip;
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(InitialImagePath.ToString(), UriKind.Absolute);

            img.EndInit();

            ratiowidthzip = img.PixelWidth;
            ratioheightzip = img.PixelHeight;

            ExportWidthZip = ratiowidthzip;
            ExportHeightZip = ratioheightzip;

            if (!bgInitializer)
            {
                bgWorker.DoWork += BgWorker_DoWork;
                bgWorker.ProgressChanged += BgWorker_ProgressChanged;
                bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

                bgWorker.WorkerSupportsCancellation = true;
                bgWorker.WorkerReportsProgress = true;
                bgInitializer = true;
            }
        }

        private void CollectImages()
        {

            if (InitialImagePath != "")
            {
                ShortPathZip = InitialImagePath;//.Substring(8);
                FolderNameZip = new DirectoryInfo(System.IO.Path.GetDirectoryName(ShortPathZip));

                var _fileExtension = System.IO.Path.GetExtension(ShortPathZip);
                var FilesDetails = (Directory.GetFiles(FolderNameZip.ToString(), "*" + _fileExtension));

                ImagesZip.Clear();
                FileExtensions.Clear();
                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);
                FileExtensions.Add("Jpg");
                FileExtensions.Add("Png");
                FileExtensions.Add("Bmp");

                foreach (var f in FilesDetails)
                {
                    string a = f;
                    if (watermarkName.ImageName != "")
                    {
                        var file = Path.Combine(tempfolder, Path.GetFileName(f));
                        File.Copy(f, file);
                        a = WatermarkProperties.ApplyWatermark(file);
                    }
                    ImageDetails id = new ImageDetails()
                    {
                        Path = a,
                        FileName = System.IO.Path.GetFileName(a),
                        Extension = System.IO.Path.GetExtension(a),
                        DateModified = System.IO.File.GetCreationTime(a).ToString("yyyy-MM-dd"),
                        IsZIPSelected = false,
                        Width = (int)ExportWidthZip,
                        Height = (int)ExportHeightZip
                    };
                    //FileExtensions.Add(id.Extension);
                    ImagesZip.Add(id);
                }

                ZipImageCount = ImagesZip.Count(str => str.IsZIPSelected == true);
            }
        }

        public void CountSelectedImages()
        {
            ZipImageCount = ImagesZip.Count(str => str.IsZIPSelected == true);
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ImageFilm == true)
            {
                if (SelectedFileExtension == "" || SelectedFileExtension == null) { SelectedFileExtension = "Jpg"; }
                using (ZipArchive archive = System.IO.Compression.ZipFile.Open(zipPath, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile(Path.Combine(tempPathFolder, "imagefilm." + SelectedFileExtension), "imagefilm." + SelectedFileExtension);
                }
            }
            __mainWindowAdvanced.HideProgress();
            __mainWindowAdvanced.ChangesProgress.Value = 0;
            count = 0;

            if (successful == true)
            {
                MessageBox.Show("Saved Successfully at location " + Environment.NewLine + zipPath, "ExportZIP", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            //if (tempPathFolder != "")
            //    Directory.Delete(tempPathFolder, true);
            
            
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            __mainWindowAdvanced.ChangesProgress.Value = (count * 100) / total;
            __mainWindowAdvanced.ProgressLabel.Text = string.Format("Saving frame " + e.ProgressPercentage + " of " + total);
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
           
            try
            {
                
                try { if (!Directory.Exists(tempPathFolder)) { Directory.CreateDirectory(tempPathFolder); } }
                catch (Exception) { return; }
                
                zipPath = _saveFileDialog.SelectedPath;//Path.Combine(FolderNameZip.ToString(), "ZIPFiles");
                if (!Directory.Exists(zipPath)) { Directory.CreateDirectory(zipPath); }

                if (ImageFilm == true) { ImageFilmGeneration(tempPathFolder); }

                string _ZipFileName = "zp_" + DateTime.Now.Ticks.ToString() + ".zip";
                zipPath = Path.Combine(zipPath, _ZipFileName);

                total = ZipImageCount;

                foreach (var img in ImagesZip) //todaysFiles is list of file names (with full path) to be zipped
                {
                    count++;
                    bgWorker.ReportProgress(count);
                    if (img.IsZIPSelected == true)
                    {
                        Bitmap image = new Bitmap(img.Path);
                        Bitmap newImage = new Bitmap((int)ExportWidthZip, (int)ExportHeightZip, PixelFormat.Format24bppRgb);

                        string tempPath = Path.Combine(tempPathFolder, img.FileName);

                        // Draws the image in the specified size with quality mode set to HighQuality
                        using (Graphics graphics = Graphics.FromImage(newImage))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.DrawImage(image, 0, 0, (int)ExportWidthZip, (int)ExportHeightZip);
                            graphics.Dispose();
                        }
                        using (var ms = new MemoryStream())
                        {
                            using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                if (SelectedFileExtension.ToUpper() == "BMP") { newImage.Save(ms, ImageFormat.Bmp); }
                                else if (SelectedFileExtension.ToUpper() == "PNG") { newImage.Save(ms, ImageFormat.Png); }
                                else { newImage.Save(ms, ImageFormat.Jpeg); }

                                byte[] bytes = ms.ToArray();
                                fs.Write(bytes, 0, bytes.Length);
                                fs.Dispose();
                            }
                            ms.Dispose();
                        }

                        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                        {
                            string _fileName = Path.GetFileNameWithoutExtension(img.FileName) + "." + SelectedFileExtension; ;
                            archive.CreateEntryFromFile(tempPath, _fileName);
                            successful = true;
                        }
                        image.Dispose();
                        newImage.Dispose();
                        GC.Collect();
                    }
                }
            }
            catch (Exception ex) { Log.Debug("Zip images exception: ",ex); }
        }

        public void ProduceZIP()
        {
            try
            {

                if (!bgWorker.IsBusy)
                {
                    if (ZipImageCount <= 0) { return; }

                    _saveFileDialog.ShowDialog();
                    if (_saveFileDialog.SelectedPath == "") { return; }
                  
                    __mainWindowAdvanced.ChangesProgress.Value = 0;
                    __mainWindowAdvanced.ProgressText.Text = "Exporting frames to ZIP...";
                    __mainWindowAdvanced.ShowProgress();
                    bgWorker.RunWorkerAsync();
                }

            }
            catch (Exception ex) { Log.Debug("", ex); }
            finally { __Parent_window.Close(); }
        }

        private void ImageFilmGeneration(string tempPath)
        {
            List<System.Drawing.Bitmap> bitimages = new List<System.Drawing.Bitmap>();
            System.Drawing.Bitmap finalImage = null;

            try
            {
                int width = 0;
                int height = 0;

                foreach (var image in ImagesZip)
                {
                    //create a Bitmap from the file and add it to the list
                    if (image.IsZIPSelected == true)
                    {
                        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image.Path);

                        //update the size of the final bitmap
                        height += (int)ExportHeightZip;
                        width = (int)ExportWidthZip > width ? (int)ExportWidthZip : width;

                        bitimages.Add(bitmap);
                    }
                }

                //create a bitmap to hold the combined image
                finalImage = new System.Drawing.Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(System.Drawing.Color.Black);

                    //go through each image and draw it on the final image
                    int offset = 0;
                    foreach (System.Drawing.Bitmap image in bitimages)
                    {
                        g.DrawImage(image, new System.Drawing.Rectangle(0, offset, (int)ExportWidthZip, (int)ExportHeightZip));
                        offset += (int)ExportHeightZip;
                    }
                }
                using (var ms = new MemoryStream())
                {
                    using (FileStream fs = new FileStream(Path.Combine(tempPath, "imagefilm." + SelectedFileExtension), FileMode.Create, FileAccess.ReadWrite))
                    {
                        finalImage.Save(ms, ImageFormat.Jpeg);
                        byte[] bytes = ms.ToArray();
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }

            }
            catch (Exception)
            {
                if (finalImage != null)
                    finalImage.Dispose();
                //throw ex;
                //throw;
            }
            finally
            {
                //clean up memory
                foreach (System.Drawing.Bitmap image in bitimages)
                {
                    image.Dispose();

                }
                finalImage.Dispose();
            }
        }
    }
}

