using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BumpKit;
using System.Windows;
using System.Drawing;
using Gif.Components;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using CameraControl.Devices;
using CameraControl.Core.Classes;
using CameraControl;
using System.ComponentModel;
using static DSLR_Tool_PC.ViewModels.Watermark;
using CameraControl.DSLRPCToolSub.ViewModels;

namespace DSLR_Tool_PC.ViewModels
{
    public class ExportGIFModel : BaseFieldClass
    {
        DSLR_Tool_PC.ViewModels.Watermark watermarkName = GetInstance();
        #region Public Variables
        public Window __Parent_window = null; 
        public List<ImageDetails> ImagesGif = new List<ImageDetails>();
        public int framedelaytimer = 100;
        public double ratiowidth = 0;
        public double ratioheight = 0;
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
            //photoEdit = (PhotoEdit)__this;
        }
        #endregion

        #region Private Variables
        private double _exportwidth = 0;
        private double _exportheight = 0;
        private static ExportGIFModel uni_class_inst = null;
        private string _uripathimgGIF = "";
        private double _timeframe = 2;
        private bool _rotationstatus = false;
        private string _uripathimgGIFPreview = "";
        private bool SkipPreview = false;

        private string tempPathFolder = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName());
        private System.Windows.Forms.FolderBrowserDialog _saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
        private int total=0;
        private int count=0;
        private bool successful = false;
        private string gPath = null;
        #endregion
        #region Initializations

        public static ExportGIFModel getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new ExportGIFModel();

            return uni_class_inst;
        }

        BackgroundWorker bgWorker = new BackgroundWorker();

        ExportGIFModel()
        {
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.WorkerReportsProgress = true;
            Task.Factory.StartNew(CallPreview);
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            __mainWindowAdvanced.HideProgress();
            __mainWindowAdvanced.ChangesProgress.Value = 0;
            if (successful == true)
            {
                MessageBox.Show("Saved Successfully at location.." + Environment.NewLine + gPath, "ExportGIF", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            __mainWindowAdvanced.ChangesProgress.Value = e.ProgressPercentage;
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (var img in ImagesGif) //todaysFiles is list of file names (with full path) to be zipped
            {
                count++;
                bgWorker.ReportProgress(count);
                string tempPath = Path.Combine(tempPathFolder, img.FileName);

                Bitmap image = new Bitmap(img.Path);
                Bitmap newImage = new Bitmap((int)ExportWidth, (int)ExportHeight, PixelFormat.Format24bppRgb);

                // Draws the image in the specified size with quality mode set to HighQuality
                using (Graphics graphics = Graphics.FromImage(newImage))
                {
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.Low;
                    graphics.SmoothingMode = SmoothingMode.Default;
                    graphics.DrawImage(image, 0, 0, (int)ExportWidth, (int)ExportHeight);
                    graphics.Dispose();
                }
                using (var ms = new MemoryStream())
                {
                    using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        newImage.Save(ms, ImageFormat.Jpeg);
                        byte[] bytes = ms.ToArray();
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Dispose();
                    }
                    ms.Dispose();
                }
                image.Dispose();
                newImage.Dispose();
                GC.Collect();
            }
            GenerateGif(tempPathFolder, _saveFileDialog.SelectedPath.ToString());
        }

        #endregion

        #region Properties
        public static int Interval { get; set; }

        //public string TargetPath { get; set; }
        public double ExportWidth
        {
            get { return _exportwidth; }
            set
            {
                if (_exportwidth != value)
                {
                    _exportwidth = value;
                    NotifyPropertyChanged("ExportWidth");
                    _exportheight = (ExportWidth / ratiowidth) * ratioheight;
                    NotifyPropertyChanged("ExportHeight");
                }
            }
        }



        public double ExportHeight
        {
            get { return _exportheight; }
            set
            {
                if (_exportheight != value)
                {
                    _exportheight = value;
                    NotifyPropertyChanged("ExportHeight");
                    _exportwidth = (ExportHeight / ratioheight) * ratiowidth;
                    NotifyPropertyChanged("ExportWidth");

                }
            }
        }

        public string URIPathImgGIF
        {
            get { return _uripathimgGIF; }
            set
            {
                if (_uripathimgGIF != value)
                {
                    _uripathimgGIF = value;
                    NotifyPropertyChanged("URIPathImgGIF");
                    InitializeImage();
                    CollectImages();
                }
            }
        }
        public string URIPathImgGIF_Preview
        {
            get { return _uripathimgGIFPreview; }
            set
            {
                _uripathimgGIFPreview = value;
                NotifyPropertyChanged("URIPathImgGIF_Preview");
            }
        }

        public string ShortPathGIF { get; set; }

        public System.IO.DirectoryInfo FolderNameGIF { get; set; }

        public string TargetFolder { get; set; }

        public string[] FilesGIF { get; set; }

        public bool RotationCheck
        {
            get { return _rotationstatus; }
            set
            {
                if (_rotationstatus != value)
                {
                    _rotationstatus = value;
                    NotifyPropertyChanged("RotationCheck");
                    SkipPreview = true;
                }
            }
        }

        public double TimeFrame
        {
            get { return _timeframe; }
            set
            {
                if (_timeframe != value)
                {
                    _timeframe = value;
                    NotifyPropertyChanged("TimeFrame");
                    FrameDelayTimer();
                }
            }
        }
        #endregion

        #region Methods

        //private void CalcHnW(string input)
        //{
        //    if (input == "height")
        //    {
        //        ExportWidth = (ExportHeight / ratioheight) * ratiowidth;
        //    }
        //    else if (input == "width")
        //    {
        //        ExportHeight = (ExportWidth / ratiowidth) * ratioheight;
        //    }
        //}
        private void CallPreview()
        {
            for (; 0 < 1;)
            {
                ShowPreview();
            }
        }
        private void ShowPreview()
        {
            try
            {
                if (URIPathImgGIF == null)
                {
                    return;
                }

                if (RotationCheck == false)
                {
                    foreach (var img in ImagesGif)
                    {
                        if (SkipPreview == true)
                        {
                            SkipPreview = false;
                            return;
                        }
                        URIPathImgGIF_Preview = img.Path;
                        Thread.Sleep(framedelaytimer);
                    }
                }
                else
                {
                    for (int i = ImagesGif.Count - 1; i >= 0; i--)
                    {
                        if (SkipPreview == true)
                        {
                            SkipPreview = false;
                            return;
                        }
                        URIPathImgGIF_Preview = ImagesGif[i].Path;
                        Thread.Sleep(framedelaytimer);
                    }

                }
            }
            catch (Exception ex)
            {

            }
        }


        private void InitializeImage()
        {
            if (URIPathImgGIF == "")
            {
                return;
            }

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(URIPathImgGIF.ToString(), UriKind.Absolute);

            img.EndInit();

            ratiowidth = img.PixelWidth;

            ratioheight = img.PixelHeight;

            ExportWidth = ratiowidth;
            ExportHeight = ratioheight;


        }

        private void CollectImages()
        {

            if (URIPathImgGIF != "")
            {
                ShortPathGIF = URIPathImgGIF;//.Substring(8);
                FolderNameGIF = new DirectoryInfo(System.IO.Path.GetDirectoryName(ShortPathGIF));

                var _fileExtension = System.IO.Path.GetExtension(ShortPathGIF);
                FilesGIF = Directory.GetFiles(FolderNameGIF.ToString(), "*" + _fileExtension);

                //TargetFolder = System.IO.Path.Combine(FolderNameGIF.ToString(), "Gifs");
                //System.IO.Directory.CreateDirectory(TargetFolder);

                ImagesGif.Clear();

                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);

                foreach (var f in FilesGIF)
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
                        DateModified = System.IO.File.GetCreationTime(a).ToString("yyyy-MM-dd")

                    };
                    id.Width = (int)ExportWidth;
                    id.Height = (int)ExportHeight;

                    ImagesGif.Add(id);
                }
                TimeFrame = (int)ImagesGif.Count * 1000 / 1000;
            }
        }
        public void FrameDelayTimer()
        {

            double frames = ImagesGif.Count;
            if (_timeframe > 0)
            {
                framedelaytimer = (int)((_timeframe / frames) * 1000);
            }

        }
        #endregion

        private int _fps;
        public int Fps
        {
            get { return _fps; }
            set
            {
                _fps = value;
            }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
            }
        }

        private string _outPutFile;
        public string OutPutFile
        {
            get { return _outPutFile; }
            set
            {
                _outPutFile = value;
            }
        }


        public void ProduceGIF()
        {
            try
            {
                _saveFileDialog.ShowDialog();
                if (_saveFileDialog.SelectedPath == "") { return; }

                try { if (!Directory.Exists(tempPathFolder)) { Directory.CreateDirectory(tempPathFolder); } }
                catch (Exception) { return; }

                total = ImagesGif.Count;
                __mainWindowAdvanced.ProgressText.Text = "Exporting frames to GIF...";
                __mainWindowAdvanced.ProgressLabel.Text = "Exporting frames to GIF image...";
                bgWorker.RunWorkerAsync();
                __mainWindowAdvanced.ShowProgress();
                if(__Parent_window!=null)
                    __Parent_window.Hide();
                //GenarateGIFs(tempPathFolder);

                //try
                //{
                //    if (tempPathFolder != "")
                //        Directory.Delete(tempPathFolder, true);
                //}
                //catch (Exception) { }
            }
            catch (Exception ex) { Log.Debug("", ex); }
            finally { if (__Parent_window != null) { __Parent_window.Close(); } }
        }
        private void GenerateGif(string tempFolder, string gifPath)
        {
            successful = false;
            if (!Directory.Exists(gifPath)) { Directory.CreateDirectory(gifPath); }

            string _GifFileName = "gf_" + DateTime.Now.Ticks.ToString() + ".gif";
            gifPath = Path.Combine(gifPath, _GifFileName);
            gPath = gifPath;
            try
            {
                var _fileExtension = System.IO.Path.GetExtension(ShortPathGIF);
                var files = Directory.GetFiles(tempFolder, "*" + _fileExtension);

                AnimatedGifEncoder e = new AnimatedGifEncoder();
                e.Start(gifPath);
                e.SetQuality(100);
                e.SetDelay(framedelaytimer);
                e.SetSize((int)ExportWidth, (int)ExportHeight);
                //e.SetFrameRate(15);

                //-1:no repeat,0:always repeat
                e.SetRepeat(0);

                if (RotationCheck)
                {
                    e.AddFrame(Image.FromFile(files[0]));
                    for (int i = files.Length - 1, count = -1; i > count; i--)
                    {
                        e.AddFrame(Image.FromFile(files[i]));
                    }
                }
                else
                {
                    for (int i = 0, count = files.Length; i < count; i++)
                    {
                        e.AddFrame(Image.FromFile(files[i]));
                    }
                    e.AddFrame(Image.FromFile(files[0]));
                }

                e.Finish();
                GC.Collect();
                successful = true;
            }
            catch (Exception ex)
            {
                Log.Debug("", ex);
            }

            //if (successful == true)
            //{
            //    MessageBox.Show("Saved Successfully at location.." + Environment.NewLine + gifPath, "ExportGIF", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }
    }
}
