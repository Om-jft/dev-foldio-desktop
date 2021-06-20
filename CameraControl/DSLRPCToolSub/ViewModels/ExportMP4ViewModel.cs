using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using BumpKit;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using Timer = System.Timers.Timer;
using System.Timers;
using System.ComponentModel;
using System.Diagnostics;
using ImageMagick;
using CameraControl.Core.Classes;
using DSLR_Tool_PC.Controles;
using CameraControl.Devices;
using CameraControl;
using CameraControl.DSLRPCToolSub.ViewModels;
using System.Threading.Tasks;

namespace DSLR_Tool_PC.ViewModels
{
    public class ExportMP4ViewModel : BaseFieldClass
    {
        DSLR_Tool_PC.ViewModels.Watermark watermarkName = DSLR_Tool_PC.ViewModels.Watermark.GetInstance();
        public Window __Parent_window = null;
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private Timer _PlayTimer = new Timer(1000);
        public List<ImageDetails> images = new List<ImageDetails>();
        public int __FrameRate = 1000;
        public int __ratiowidth = 0;
        public int __ratioheight = 0;

        private int _exportwidth = 0;
        private int _exportheight = 0;
        private string _uripathimgGIF = "";
        private bool _rotationstatus = false;
        private string _uripathimgGIFPreview = "";
        private string oPath = null;
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
            //photoEdit = (PhotoEdit)__this;
        }

        public RelayCommand PlayCommand { get; set; }
        public RelayCommand PreviousCommand { get; set; }
        public RelayCommand NextCommand { get; set; }
        public RelayCommand CreateVedioCommand { get; set; }

        private static ExportMP4ViewModel uni_class_inst = null;
        public static ExportMP4ViewModel getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new ExportMP4ViewModel();

            return uni_class_inst;
        }

        ExportMP4ViewModel()
        {
            IsEnableButton = true;
            PlayCommand = new RelayCommand(PlayImageSequence);
            PreviousCommand = new RelayCommand(PreviousFrame);
            NextCommand = new RelayCommand(NextFrame);
            CreateVedioCommand = new RelayCommand(Start);

            _PlayTimer.Elapsed += _PlayTimer_Elapsed;
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;
            _backgroundWorker.WorkerSupportsCancellation = true;
            //Task.Factory.StartNew(CallPreview);
        }

        public static string __SelectedFileName = "";

        void _PlayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ShowPreview();
        }

        private bool _IsPlay = false;
        private void PlayImageSequence()
        {
            if (!_IsPlay)
            {
                _IsPlay = true;
                _PlayTimer.Start();
                _PlayTimer.Interval = __FrameRate;
            }
            else
            {
                _IsPlay = false;
                _PlayTimer.Stop();
            }
        }
        private void PreviousFrame()
        {
            __PlayImagePosition--;
            if (__PlayImagePosition < 0) { __PlayImagePosition = images.Count - 1; }

            URIPathImgGIF_Preview = images[__PlayImagePosition].Path;
            InitializeImage(URIPathImgGIF_Preview);
        }
        private void NextFrame()
        {
            __PlayImagePosition++;
            if (__PlayImagePosition > images.Count - 1) { __PlayImagePosition = 0; }

            URIPathImgGIF_Preview = images[__PlayImagePosition].Path;
            InitializeImage(URIPathImgGIF_Preview);
        }
        private void Start()
        {
            try
            {
                if (_backgroundWorker.IsBusy) { return; }
                if (ExportWidth < 800) { _exportwidth = 800; }
                if (ExportHeight < 600) { _exportheight = 600; }

                System.Windows.Forms.FolderBrowserDialog _saveFileDialog = new System.Windows.Forms.FolderBrowserDialog();
                _saveFileDialog.ShowDialog();
                if (_saveFileDialog.SelectedPath == "") { return; }

                __SaveFilePath = _saveFileDialog.SelectedPath.ToString();
                IsEnableButton = false;
                __mainWindowAdvanced.ChangesProgress.Value = 0;
                __mainWindowAdvanced.ProgressLabel.Text = "Exporting to MP4...";
                __mainWindowAdvanced.ProgressText.Text = "Expoting frames to MP4...";
                __mainWindowAdvanced.ShowProgress();
                __mainWindowAdvanced.ProgressPannel.Visibility = Visibility.Visible;
                _backgroundWorker.RunWorkerAsync();
                __Parent_window.Hide();
            }
            catch (Exception ex) { Log.Debug("", ex); }
        }
        private string __SaveFilePath = "";

        private bool _isEnableButton = true;
        public bool IsEnableButton
        {
            get { return _isEnableButton; }
            set
            {
                _isEnableButton = value;
                NotifyPropertyChanged("IsEnableButton");
            }
        }

        public string TargetPath { get; set; }
        public int ExportWidth
        {
            get { return _exportwidth; }
            set
            {
                if (_exportwidth != value)
                {
                    _exportwidth = value;
                    //if (_exportwidth < 800) { _exportwidth = 800; }
                    NotifyPropertyChanged("ExportWidth");

                    _exportheight = (ExportWidth / __ratiowidth) * __ratioheight;
                    //if (_exportheight < 600) { _exportheight = 600; }
                    NotifyPropertyChanged("ExportHeight");
                }
            }
        }

        public int ExportHeight
        {
            get { return _exportheight; }
            set
            {
                if (_exportheight != value)
                {
                    _exportheight = value;
                    //if (_exportheight < 600) { _exportheight = 600; }
                    NotifyPropertyChanged("ExportHeight");
                    //_exportwidth = (ExportHeight / __ratioheight) * __ratiowidth;
                    //NotifyPropertyChanged("ExportWidth");
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
                    URIPathImgGIF_Preview = value;
                    NotifyPropertyChanged("URIPathImgGIF_Preview");

                    InitializeImage(URIPathImgGIF_Preview);
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
                }
            }
        }

        private double _playTime = 2;
        public double PlayTime
        {
            get { return _playTime; }
            set
            {
                if (_playTime != value)
                {
                    _playTime = value;
                    NotifyPropertyChanged("PlayTime");
                    FrameRateCalculation();
                    if (_IsPlay) { _IsPlay = false; _PlayTimer.Stop(); PlayImageSequence(); }
                }
            }
        }

        public int Fps { get; private set; }
        public int Progress { get; private set; }
        public bool FillImage { get; private set; }

        private int __PlayImagePosition = 0;

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
                if (URIPathImgGIF == null) { return; }

                if (RotationCheck == false)
                {
                    __PlayImagePosition++;
                    if (__PlayImagePosition > images.Count - 1) { __PlayImagePosition = 0; }
                    URIPathImgGIF_Preview = images[__PlayImagePosition].Path;

                    return;
                }
                else if (RotationCheck == true)
                {
                    __PlayImagePosition--;
                    if (__PlayImagePosition < 0) { __PlayImagePosition = images.Count - 1; }
                    URIPathImgGIF_Preview = images[__PlayImagePosition].Path;

                    return;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void ProduceGIF()
        {
            bool successful = false;

            if (RotationCheck == false)
            {
                using (var targetfile = File.Create(TargetFolder + "/targetfile" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".gif")) { TargetPath = targetfile.Name.ToString(); }

                using (var gif = File.OpenWrite(TargetPath))
                {
                    using (var encoder = new GifEncoder(gif))
                    {
                        foreach (var img in images)
                        {
                            using (var frame = System.Drawing.Image.FromFile(img.Path).GetThumbnailImage((int)ExportWidth, (int)ExportHeight, null, IntPtr.Zero))
                            {
                                if (__FrameRate > 0.0)
                                {
                                    encoder.FrameDelay = TimeSpan.FromMilliseconds(__FrameRate);
                                }
                                encoder.AddFrame(frame);
                            }

                        }
                        using (var frame = System.Drawing.Image.FromFile(images[0].Path).GetThumbnailImage((int)ExportWidth, (int)ExportHeight, null, IntPtr.Zero))
                        {
                            if (__FrameRate > 0.0)
                            {
                                encoder.FrameDelay = TimeSpan.FromMilliseconds(__FrameRate);
                            }
                            encoder.AddFrame(frame);
                        }
                    }

                    successful = true;
                }

            }

            else if (RotationCheck == true)
            {
                using (var targetfile = File.Create(TargetFolder + "/targetfileRR" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".gif")) { TargetPath = targetfile.Name.ToString(); }

                //  int count = images.Count;

                using (var gif = File.OpenWrite(TargetPath))
                {
                    using (var encoder = new GifEncoder(gif))
                    {
                        using (var frame = System.Drawing.Image.FromFile(images[0].Path).GetThumbnailImage((int)ExportWidth, (int)ExportHeight, null, IntPtr.Zero))
                        {
                            if (__FrameRate > 0.0)
                            {
                                encoder.FrameDelay = TimeSpan.FromMilliseconds(__FrameRate);
                            }
                            encoder.AddFrame(frame);
                        }
                        foreach (var img in images.Reverse<ImageDetails>())
                        {

                            using (var frame = System.Drawing.Image.FromFile(img.Path).GetThumbnailImage((int)ExportWidth, (int)ExportHeight, null, IntPtr.Zero))
                            {
                                if (__FrameRate > 0.0)
                                {
                                    encoder.FrameDelay = TimeSpan.FromMilliseconds(__FrameRate);
                                }
                                encoder.AddFrame(frame);
                            }

                        }
                    }
                }
                successful = true;
            }

            if (successful == true)
            {
                MessageBox.Show("Saved Successfully at location " + TargetPath, "ExportMP4", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void InitializeImage(string __ImagePath)
        {
            if (__ImagePath == "")
            {
                return;
            }

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(__ImagePath, UriKind.Absolute);

            img.EndInit();

            __ratiowidth = img.PixelWidth;
            __ratioheight = img.PixelHeight;

            ExportWidth = __ratiowidth;
            ExportHeight = __ratioheight;
        }

        private void CollectImages()
        {

            if (URIPathImgGIF != "")
            {
                ShortPathGIF = URIPathImgGIF; //.Substring(8);
                FolderNameGIF = new DirectoryInfo(System.IO.Path.GetDirectoryName(ShortPathGIF));

                var _fileExtension = System.IO.Path.GetExtension(ShortPathGIF);
                FilesGIF = Directory.GetFiles(FolderNameGIF.ToString(), "*" + _fileExtension);

                TargetFolder = System.IO.Path.Combine(FolderNameGIF.ToString(), "MP4");
                System.IO.Directory.CreateDirectory(TargetFolder);

                images.Clear();
                string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
                if (!Directory.Exists(tempfolder))
                    Directory.CreateDirectory(tempfolder);
                foreach (var f in FilesGIF)
                {
                    string a = f;
                    //if (watermarkName.ImageName != "")
                    //{
                    //    var file = Path.Combine(tempfolder, Path.GetFileName(f));
                    //    File.Copy(f, file);
                    //    WatermarkProperties.ApplyWatermark(file);
                    //    a = file;
                    //}
                    ImageDetails id = new ImageDetails()
                    {
                        Path = a,
                        FileName = System.IO.Path.GetFileName(a),
                        Extension = System.IO.Path.GetExtension(a),
                        DateModified = System.IO.File.GetCreationTime(a).ToString("yyyy-MM-dd")
                    };
                    id.Width = (int)ExportWidth;
                    id.Height = (int)ExportHeight;

                    images.Add(id);
                    if (id.IsSelected) { __PlayImagePosition = images.Count - 1; }
                }
                PlayTime = (int)images.Count * 1000 / 1000;
            }
        }


        private void FrameRateCalculation()
        {
            double TotalFrames = images.Count;
            if (_playTime > 0)
            {
                //__FrameRate  = (int)(TotalFrames / _playTime);
                __FrameRate = (int)(_playTime / TotalFrames) * 1000;
                if (__FrameRate <= 1000) { __FrameRate = 1000; }
            }
        }

        private void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //RaisePropertyChanged(() => IsBusy);
            //RaisePropertyChanged(() => IsFree);
            __Parent_window.Close();
            __mainWindowAdvanced.HideProgress();
            __mainWindowAdvanced.ProgressPannel.Visibility = Visibility.Collapsed;
            __mainWindowAdvanced.ChangesProgress.Value = 0;
            if (!File.Exists(oPath))
            {
                MessageBox.Show("Video file not found !");
            }
            else
            {
                MessageBox.Show("Saved Successfully at location " + oPath, "ExportMP4", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            IsEnableButton = true;
        }


        private void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
           
            try
            {

                string tempFolder = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName());

                try
                {
                    if (!Directory.Exists(tempFolder))
                        Directory.CreateDirectory(tempFolder);
                }
                catch (Exception ex)
                {
                    return;
                }

                Thread.Sleep(500);
                //TotalImages = images.Count;
                int counter = 1;
                //ProgressMax = MaxValue - MinValue;
                //Progress = 0;

                if (RotationCheck == false)
                {
                    for (int i = 0; i < images.Count; i++)
                    {
                        if (_backgroundWorker.CancellationPending)
                        {
                            DeleteTempFolder(tempFolder);
                            return;
                        }

                        try
                        {
                            //S1:
                            //Progress++;
                            string fileName = images[i].Path;
                            string outfile = Path.Combine(tempFolder, "img" + counter.ToString("000000") + ".jpg");

                            CopyFile(fileName, outfile);
                            counter++;
                            //if (counter == images.Count) { goto S1; }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                else
                {
                    for (int i = images.Count - 1; i >= 0; i--)
                    {
                        if (_backgroundWorker.CancellationPending)
                        {
                            DeleteTempFolder(tempFolder);
                            return;
                        }

                        try
                        {
                            //Progress++;
                            string fileName = images[i].Path;
                            string outfile = Path.Combine(tempFolder, "img" + counter.ToString("000000") + ".jpg");

                            CopyFile(fileName, outfile);
                            counter++;
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                GenerateMp4(tempFolder);

                DeleteTempFolder(tempFolder);

            }
            catch (Exception ex) { Log.Debug("", ex); }
        }

        private void GenerateMp4(string tempFolder)
        {
            string OutPutFile = Path.Combine(__SaveFilePath, "mp_" + DateTime.Now.Ticks.ToString() + ".mp4");
            oPath = OutPutFile;
            try
            {
                string ffmpegPath = Path.Combine(Settings.ApplicationFolder, "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                {
                    MessageBox.Show("ffmpeg not found! Please reinstall the application.");
                    return;
                }
                string _outLastfileName = Path.Combine(tempFolder, "img%06d.jpg");
                float fr = Convert.ToSingle(images.Count / PlayTime); //(1f / (Convert.ToSingle(PlayTime) * (1f / 3f)));

                //string parameters = @"-r {0} -i {1}\img00%04d.jpg -c:v libx264 -vf fps=25 -pix_fmt yuv420p";
                string parameters = @" -framerate {0} -i {1} -c:v libx264 -vf fps=25 -pix_fmt yuv420p -vf scale={3}:{4} {2}";

                //new VideoType("4K 16:9", 3840, 2160, ".mp4"),
                //new VideoType("HD 1080 16:9", 1920, 1080, ".mp4"),
                //new VideoType("UXGA 4:3", 1600, 1200, ".mp4"),
                //new VideoType("HD 720 16:9", 1280, 720, ".mp4"),
                //new VideoType("Super VGA 4:3", 800, 600, ".mp4"),


                Process newprocess = new Process();
                newprocess.StartInfo = new ProcessStartInfo()
                {
                    FileName = ffmpegPath,
                    Arguments = string.Format(parameters, fr, _outLastfileName, OutPutFile, ExportWidth, ExportHeight),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                newprocess.Start();
                newprocess.OutputDataReceived += newprocess_OutputDataReceived;
                newprocess.ErrorDataReceived += newprocess_OutputDataReceived;
                newprocess.BeginOutputReadLine();
                newprocess.BeginErrorReadLine();
                newprocess.WaitForExit();
            }
            catch (Exception ex) { Log.Debug("", ex); }

            //if (!File.Exists(OutPutFile))
            //{
            //    MessageBox.Show("Video file not found !");
            //}
            //else
            //{
            //    MessageBox.Show("Saved Successfully at location " + OutPutFile, "ExportMP4", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }

        private void CopyFile(string filename, string destFile)
        {
            using (MagickImage image = new MagickImage(filename))
            {
                double zw = (double)ExportWidth / image.Width;
                double zh = (double)ExportHeight / image.Height;
                double za = FillImage ? ((zw <= zh) ? zw : zh) : ((zw >= zh) ? zw : zh);

                MagickGeometry geometry = new MagickGeometry(ExportWidth, ExportHeight)
                {
                    IgnoreAspectRatio = false,
                    FillArea = false
                };

                image.FilterType = FilterType.Point;
                image.Resize(geometry);
                image.Quality = 100;
                image.Format = MagickFormat.Jpeg;
                image.Write(destFile);
            }
        }

        private void DeleteTempFolder(string tempFolder)
        {
            try
            {
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
            }
        }

        private object _locker = new object();
        private void newprocess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                lock (_locker)
                {
                    //frame=   20 fps= 12 q=0.0 size=       0kB time=00:00:00.00 bitrate=N/A    
                    if (e.Data != null && e.Data.StartsWith("frame="))
                    {
                        var s = e.Data.Substring(7, 5).Trim();
                        int i = 0;
                        if (int.TryParse(e.Data.Substring(7, 5).Trim(), out i))
                            Progress = i;
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
