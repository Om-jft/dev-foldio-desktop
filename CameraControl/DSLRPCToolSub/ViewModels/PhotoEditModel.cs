using System;
using System.IO;
using CameraControl.Core.Classes;
using System.Drawing;
using System.Drawing.Imaging;
using Accord.Imaging.Filters;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using DSLR_Tool_PC.Controles;
using CameraControl.Core;
using CameraControl.Devices;
using CameraControl;
using CameraControl.DSLRPCToolSub.Controles;
using System.ComponentModel;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using CameraControl.DSLRPCToolSub.Classes;
using CameraControl.DSLRPCToolSub.ViewModels;
using CameraControl.DSLRPCToolSub.UndoRedo;
using FileInfo = System.IO.FileInfo;

namespace DSLR_Tool_PC.ViewModels
{
    public class PhotoEditModel : BaseFieldClass
    {
        private readonly object _Sliderlockobj = new object();
        private string imageFolder = null;
        public RelayCommand ApplyAllFrames { get; set; }
        //PhotoEdit __photoEdit = PhotoEdit.getInstance();
        BackgroundWorker bgWorker = new BackgroundWorker();
        string _strApplPath = null;
        int total = 0;
        int count = 0;
        ExportPathUpdate __exportPathUpdate = ExportPathUpdate.getInstance();
        PathUpdate __PathUpdate = PathUpdate.getInstance();
        //EditLeftControl __editLeftControl = null;
        public int filterFlag = 0;
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
            //photoEdit = (PhotoEdit)__this;
        }
        //public PhotoEdit photoEdit = PhotoEdit.GetInstance();
        PhotoEditModel()
        {
            ApplyAllFrames = new RelayCommand(Start);
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.WorkerReportsProgress = true;

            state_bgWorker.DoWork += state_BgWorker_DoWork;
            state_bgWorker.ProgressChanged += state_BgWorker_ProgressChanged;
            state_bgWorker.RunWorkerCompleted += state_BgWorker_RunWorkerCompleted;

            state_bgWorker.WorkerSupportsCancellation = true;
            state_bgWorker.WorkerReportsProgress = true;

            FilterCorrection_bgWorker.DoWork += FilterCorrection_BgWorker_DoWork;
            FilterCorrection_bgWorker.RunWorkerCompleted += FilterCorrection_BgWorker_RunWorkerCompleted;
            FilterCorrection_bgWorker.WorkerSupportsCancellation = true;

            ResetAllControls();
        }
        private static PhotoEditModel _photoeditmodel_inst = null;
        public static PhotoEditModel GetInstance()
        {
            if (_photoeditmodel_inst == null) 
            {
                _photoeditmodel_inst = new PhotoEditModel();
               
            }
            return _photoeditmodel_inst;
        }

        private bool _isIncludeBGApply = true;
        public bool IsIncludeBGApply
        {
            get { return _isIncludeBGApply; }
            set
            {
                _isIncludeBGApply = value;
                NotifyPropertyChanged("IsIncludeBGApply");
            }
        }

        private bool _isObjectOnlyApply = false;
        public bool IsObjectOnlyApply
        {
            get { return _isObjectOnlyApply; }
            set
            {
                _isObjectOnlyApply = value;
                NotifyPropertyChanged("IsObjectOnlyApply");
            }
        }

        private byte[] _imageData;
        public byte[] ImageData
        {
            get { return _imageData; }
            set { _imageData = value; ResetAllControls(); }
        }

        //private string _ImagePath;
        //public string ImagePath
        //{
        //    get { return _ImagePath; }
        //    set { _ImagePath = value; }
        //}

        #region "Brightness"
        private bool _isBrightnessApply = false;
        public bool IsBrightnessApply
        {
            get { return _isBrightnessApply; }
            set
            {
                _isBrightnessApply = value;
                NotifyPropertyChanged("IsBrightnessApply");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 300))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private int _brightness = 0;
        public int Brightness
        {
            get { return _brightness; }
            set
            {
                _brightness = value;
                NotifyPropertyChanged("Brightness");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 300))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        #endregion

        private bool _isContrastApply = false;
        public bool IsContrastApply
        {
            get { return _isContrastApply; }
            set
            {
                _isContrastApply = value;
                NotifyPropertyChanged("IsContrastApply");
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply);}
            }
        }

        private int _contrast = 0;
        public int Contrast
        {
            get { return _contrast; }
            set
            {
                _contrast = value;
                NotifyPropertyChanged("Contrast");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null){ Task.Factory.StartNew(EditFiltersApply);}
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 300))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private bool _isSaturationApply = false;
        public bool IsSaturationApply
        {
            get { return _isSaturationApply; }
            set
            {
                _isSaturationApply = value;
                NotifyPropertyChanged("IsSaturationApply");
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
            }
        }

        private int _saturation = 0;
        public int Saturation
        {
            get { return _saturation; }
            set
            {
                _saturation = value;
                NotifyPropertyChanged("Saturation");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 300))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private bool _isWhiteClippingApply = false;
        public bool IsWhiteClippingApply
        {
            get { return _isWhiteClippingApply; }
            set
            {
                _isWhiteClippingApply = value;
                NotifyPropertyChanged("IsWhiteClippingApply");
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
            }
        }

        private int _whiteclipping = 0;
        public int WhiteClipping
        {
            get { return _whiteclipping; }
            set
            {
                _whiteclipping = value;
                NotifyPropertyChanged("WhiteClipping");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 300))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private bool _isWhiteBalanceApply = false;
        public bool IsWhiteBalanceApply
        {
            get { return _isWhiteBalanceApply; }
            set
            {
                _isWhiteBalanceApply = value;
                NotifyPropertyChanged("IsWhiteBalanceApply");
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
            }
        }

        private int _whiteBalance = 0;
        public int WhiteBalance
        {
            get { return _whiteBalance; }
            set
            {
                _whiteBalance = value;
                NotifyPropertyChanged("WhiteBalance");
                //if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 1000))
                        {
                            //MessageBox.Show("Background Filter.");
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private bool _isBackgroundFilterApply = false;
        public bool IsBackgroundFilterApply
        {
            get { return _isBackgroundFilterApply; }
            set
            {
                _isBackgroundFilterApply = value;
                
                NotifyPropertyChanged("IsBackgroundFilterApply");
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) {
                    //MessageBox.Show("In IsBackground Filter Apply.");
                    //Task.Factory.StartNew(EditFiltersApply);
                    }
            }
        }

        private int _backgroundFilter = 0;
        public int BackgroundFilter
        {
            get { return _backgroundFilter; }
            set
            {
                _backgroundFilter = value;
                NotifyPropertyChanged("BackgroundFilter");
                lock (_Sliderlockobj)
                    Monitor.PulseAll(_Sliderlockobj);
                Task.Run(() =>
                {
                    lock (_Sliderlockobj)
                        if (!Monitor.Wait(_Sliderlockobj, 1000))
                        {
                            //MessageBox.Show("Background Filter.");
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }

        private string sourcefile;
        private string destfile;
        private bool ApplyAll=false;
        string loaderText="this";
        public void FiltersCorrections(string _sourcefile, string _destfile,bool _ApplyAll)
        {
            if (!IsBackgroundFilterApply && !IsBrightnessApply && !IsContrastApply && !IsSaturationApply && !IsWhiteBalanceApply && !IsWhiteClippingApply) { return; }
            sourcefile = _sourcefile;
            destfile = _destfile;
            ApplyAll = _ApplyAll;
            if (!FilterCorrection_bgWorker.IsBusy && getDiff())
            {
                if (!ApplyAll)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        loaderText = __mainWindowAdvanced.preloaderText.Text;
                        __mainWindowAdvanced.preloaderText.Text = String.Format("Applying filters...");
                        __mainWindowAdvanced.preLoader.Visibility = Visibility.Visible;
                        __mainWindowAdvanced.ShowProgress();
                    });
                    
                }
                FilterCorrection_bgWorker.RunWorkerAsync();
            }
        }

        public Bitmap RotateFrame(int rotationAngle,Bitmap finalBmp)
        {
            using (finalBmp)
            {
                switch (rotationAngle)
                {
                    case 0:
                        finalBmp.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                        break;
                    case 90:
                        finalBmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 180:
                        finalBmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 270:
                        finalBmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
                return new Bitmap(finalBmp);
            }
        }
        public int getIndex(string filename)
        {
            int retInt = -1;
           foreach(var ele in __mainWindowAdvanced.images_Folder)
            {
                retInt++;
                if (ele.FileName.Equals(filename)) 
                {
                    return retInt;
                }
            }
            return retInt;
        }

        private Bitmap Apply_WhiteClipping(Bitmap _bmpImage,int WC)
        {
            string tempwc = Path.Combine(Settings.ApplicationTempFolder, ".ClippingApply");
            if (!Directory.Exists(tempwc)) { Directory.CreateDirectory(tempwc); }
            string tempFile_In = Path.Combine(tempwc, Path.GetRandomFileName().Replace(".", "") + "." + ImageFormat.Jpeg);

            if (File.Exists(tempFile_In))
                File.Delete(tempFile_In);

            _bmpImage.Save(tempFile_In, System.Drawing.Imaging.ImageFormat.Jpeg);

            int TempWhiteClipping = 0;
            if (WC > 0) { TempWhiteClipping = Convert.ToInt32(WC * 1.275); }
            if (WC < 0) { TempWhiteClipping = Convert.ToInt32(-WC * 1.275); }
            StaticClass.WhiteClipping_usingPy(tempFile_In, tempFile_In, TempWhiteClipping, 0);

            return new Bitmap(tempFile_In);
                
            //if (File.Exists(tempFile_In))
            //    File.Delete(tempFile_In);
        }

        private Bitmap Apply_BackgroundFilter(Bitmap _bmpImage,int bgf)
        {
            Bitmap _returnBmp = null;
            try
            {
                string tempbg = Path.Combine(Settings.ApplicationTempFolder, "BGApply");
                if (!Directory.Exists(tempbg)) { Directory.CreateDirectory(tempbg); }
                string tempFile_In = Path.Combine(tempbg, Path.GetRandomFileName().Replace(".", "") + "." + ImageFormat.Jpeg);

                if (File.Exists(tempFile_In))
                    File.Delete(tempFile_In);

                _bmpImage.Save(tempFile_In, System.Drawing.Imaging.ImageFormat.Jpeg);

                int TempBgFilter = 0;
                if (bgf > 1)
                {
                    if ((bgf % 2) == 0) { TempBgFilter = bgf + 1; } else { TempBgFilter = bgf; }
                    //MessageBox.Show(TempBgFilter.ToString());
                     StaticClass.RemoveBG_usingPy(tempFile_In, tempFile_In, TempBgFilter);
                    Log.Debug(tempFile_In);
                }

                Bitmap bmp = (Bitmap)Image.FromFile(tempFile_In);
                _returnBmp = bmp;

                //bmp.Dispose();

                //if (File.Exists(tempFile_In))
                //    File.Delete(tempFile_In);
            }
            catch (Exception ex) { Log.Debug("Apply_BackgroundFilter", ex); }

            return _returnBmp;
        }

        public void ResetAllControls()
        {
            WhiteBalance = 0;
            Contrast = 0;
            Brightness = 0;
            Saturation = 0;
            WhiteClipping = 0;
            BackgroundFilter = 0;

            IsBrightnessApply = false;
            IsContrastApply = false;
            IsSaturationApply = false;
            IsWhiteBalanceApply = false;
            IsWhiteClippingApply = false;
            IsBackgroundFilterApply = false;

            IsIncludeBGApply = true;
        }

        private Bitmap Convert_WhiteBalance(Bitmap _sourcebmp, int _kelvinWB)
        {
            BitmapData _sourceData = _sourcebmp.LockBits(new Rectangle(0, 0, _sourcebmp.Width, _sourcebmp.Height),
                                                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] _pixelBuffer = new byte[_sourceData.Stride * _sourceData.Height];
            Marshal.Copy(_sourceData.Scan0, _pixelBuffer, 0, _pixelBuffer.Length);
            _sourcebmp.UnlockBits(_sourceData);

            float blueLevelFloat = 0f;
            float greenLevelFloat = 0f;
            float redLevelFloat = 0f;


            float temp = (float)(_kelvinWB / 100.0);
            if (temp <= 66)
            {
                redLevelFloat = 255;
                greenLevelFloat = temp;
                greenLevelFloat = Convert.ToSingle(99.4708025861 * Math.Log(greenLevelFloat) - 161.1195681661);

                if (temp <= 19) { blueLevelFloat = 0; }
                else
                {
                    blueLevelFloat = temp - 10;
                    blueLevelFloat = Convert.ToSingle(138.5177312231 * Math.Log(blueLevelFloat) - 305.0447927307);
                }
            }
            else
            {
                redLevelFloat = temp - 60;
                redLevelFloat = Convert.ToSingle(329.698727446 * Math.Pow(redLevelFloat, -0.1332047592));

                greenLevelFloat = temp - 60;
                greenLevelFloat = Convert.ToSingle(288.1221695283 * Math.Pow(greenLevelFloat, -0.0755148492));

                blueLevelFloat = 255;
            }

            for (int k = 0; k + 4 < _pixelBuffer.Length; k += 4)
            {
                float blue = 0;
                float green = 0;
                float red = 0;

                blue = 255.0f / blueLevelFloat * (float)_pixelBuffer[k];
                green = 255.0f / greenLevelFloat * (float)_pixelBuffer[k + 1];
                red = 255.0f / redLevelFloat * (float)_pixelBuffer[k + 2];

                if (blue > 255) { blue = 255; }
                else if (blue < 0) { blue = 0; }

                if (green > 255) { green = 255; }
                else if (green < 0) { green = 0; }

                if (red > 255) { red = 255; }
                else if (red < 0) { red = 0; }

                _pixelBuffer[k] = (byte)blue;
                _pixelBuffer[k + 1] = (byte)green;
                _pixelBuffer[k + 2] = (byte)red;
            }

            Bitmap resultBitmap = new Bitmap(_sourcebmp.Width, _sourcebmp.Height);
            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_pixelBuffer, 0, resultData.Scan0, _pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);
            return resultBitmap;
        }

        
        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            __mainWindowAdvanced.BrowseReload(imageFolder);
            __mainWindowAdvanced.HideProgress();
            __mainWindowAdvanced.ProgressPannel.Visibility = Visibility.Collapsed;
            ResetAllControls();
            count = 0;
            System.Windows.MessageBox.Show("Applied successfully...!", "360 PC Tool", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //this.lblCounter.Content = e.ProgressPercentage;
            //this.progressbar.Value = e.ProgressPercentage;
            __mainWindowAdvanced.ChangesProgress.Value = (count*100)/total;
            __mainWindowAdvanced.ProgressLabel.Text = string.Format("Saving frame " + e.ProgressPercentage + " of " + total);
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ImageDetails _imageDetails = __PathUpdate.__SelectedImageDetails;
            if (_imageDetails == null) { return; }

            DirectoryInfo _dirInfoApplPath = new DirectoryInfo(System.IO.Path.GetDirectoryName(_imageDetails.Path_Orginal));
            string[] _pathImagFiles = Directory.GetFiles(_dirInfoApplPath.ToString());
            total = _pathImagFiles.Length;

            string tempSF = Path.Combine(Settings.ApplicationTempFolder, ".SaveAllFrame_"+Path.GetRandomFileName());
            if (!Directory.Exists(tempSF)) { Directory.CreateDirectory(tempSF); }
            try
                {
                foreach (var _imgfl in _pathImagFiles)
                {
                    count++;
                    bgWorker.ReportProgress(count);
                    FilterApply(_imgfl, Path.Combine(tempSF,Path.GetFileName(_imgfl)));
                }
                imageFolder = tempSF;
            }
            catch (Exception ex) { Log.Debug("Apply all frames: ",ex); }
        }

        private void Start()
        {
            if (Brightness != 0 || WhiteClipping != 0 || _whiteBalance != 0 || Contrast != 0 || Saturation != 0 || BackgroundFilter != 0)
            {
                if (!bgWorker.IsBusy)
                {
                    __mainWindowAdvanced.ProgressText.Text = String.Format("Applying All Frames...");
                    __mainWindowAdvanced.ChangesProgress.Value = 0;
                    __mainWindowAdvanced.ShowProgress();
                    __mainWindowAdvanced.ProgressPannel.Visibility = Visibility.Visible;
                    bgWorker.RunWorkerAsync();
                }
            }
            else { return; }
                
        }

        #region Depricated_Apply_All_Frame_code
        private void Start_Test()
        {
            string _pathImag = __PathUpdate.PathImg;//.Substring(8);
            if (_pathImag == null) { return; }
            ImageDetails _imageDetails = __PathUpdate.__SelectedImageDetails;
            if (_imageDetails == null) { return; }

            DirectoryInfo _dirInfoApplPath = new DirectoryInfo(System.IO.Path.GetDirectoryName(_imageDetails.Path_Orginal));
            string[] _pathImagFiles = Directory.GetFiles(_dirInfoApplPath.ToString());

            //string _strApplPath = System.IO.Path.Combine(_dirInfoApplPath.ToString(), "JPG_ORG");
            try
            {
                foreach (var _imgfl in _pathImagFiles)
                {
                    //string TargetPath = "";
                    string _exFileName = System.IO.Path.GetFileName(_imgfl);
                    //CopyBackUp(_imgfl, Path.Combine(__mainWindowAdvanced.OGFolder, _exFileName));
                    //FiltersCorrections(_imgfl, Path.Combine(_strApplPath, _exFileName));

                }
            }
            catch (Exception ex) { ex.ToString(); }
            System.Windows.MessageBox.Show("Apply All Frames and Saved Successfully...!", "Photo Edit", MessageBoxButton.OK, MessageBoxImage.Information);
            __mainWindowAdvanced.BrowseReload(_strApplPath);
            //__PathUpdate.__SelectedImageDetails = null;
            //__PathUpdate.PathImg = "";
            //ServiceProvider.Settings.EditImageByte = null;
            //ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = null;
            ResetAllControls();
        }
        #endregion
        private void EditFiltersApply()
        {
            FiltersCorrections(__PathUpdate.PathImg, "",false);
            //ServiceProvider.Settings.EditImageByte = _applyfilterImage;
        }

        private string CopyBackUp(string source, string dest)
        {
            try
            {
                string backupFile = dest;

            s1:
                // if file already exist with same name generate a unique name
                if (File.Exists(backupFile))
                {
                    backupFile = Path.Combine(Path.GetDirectoryName(backupFile), Path.GetFileNameWithoutExtension(backupFile) + "(1)" + Path.GetExtension(backupFile));
                    goto s1;
                }
                File.Copy(source, backupFile);
                return backupFile;
            }
            catch (Exception ex)
            {
                Log.Error("Unable to make backup ", ex);
                return "";
            }
        }

        public Bitmap getBitmapFromImageFolder(string path)
        {
            string filename = Path.GetFileName(path);
            using(Bitmap bitmapresult=new Bitmap(path))
            {
                return (Bitmap)bitmapresult.Clone();
            }
        }
        #region Filterdifference
        public int DiffSaturation = 0;
        public int DiffWhiteBalance = 0;
        public int DiffBrightness = 0;
        public int DiffContrast = 0;
        public int DiffWhiteClipping = 0;
        public int DiffBackgroundFilter = 0;
        #endregion

        public bool getDiff()
        {
            if (Saturation == 0 && Brightness == 0 && WhiteBalance == 0 && Contrast == 0 && WhiteClipping == 0 && BackgroundFilter == 0) { return false; }
            else
            {
                if (DiffSaturation == Saturation && DiffWhiteBalance== WhiteBalance && DiffWhiteClipping == WhiteClipping && DiffContrast== Contrast && DiffBrightness== Brightness && DiffBackgroundFilter== BackgroundFilter) { return false; }
                else { return true; }
            }
        }
        public void setDiff()
        {
            if (Saturation == 0 && Brightness == 0 && WhiteBalance == 0 && Contrast == 0 && WhiteClipping == 0 && BackgroundFilter == 0) { return; }
            else
            {
                DiffBackgroundFilter = BackgroundFilter;
                DiffBrightness = Brightness;
                DiffContrast = Contrast;
                DiffSaturation = Saturation;
                DiffWhiteBalance = WhiteBalance;
                DiffWhiteClipping = WhiteClipping;
            }
        }

        #region StateParameter
        int _stIndex = 0;
        int _STwhiteclipping = 0;
        int _STwhitebalance = 0;
        int _STbrightness = 0;
        int _STcontrast = 0;
        int _STsaturation = 0;
        int _STbgFilter = 0;
        ImageDetails _imageDetailsObjet = null;
        string preloaderText = null;
        #endregion
        public BackgroundWorker state_bgWorker = new BackgroundWorker();
        public void applyStateFilter(int index, ImageDetails imageDetailsObjet)
        {
            _stIndex = index;
            _STwhiteclipping = imageDetailsObjet.WhiteClipping;
            _STwhitebalance = imageDetailsObjet.WhiteBalance;
            _STbrightness = imageDetailsObjet.Brightness;
            _STcontrast = imageDetailsObjet.Contrast;
            _STsaturation = imageDetailsObjet.Saturation;
            _STbgFilter = imageDetailsObjet.BackgroundFilter;
            _imageDetailsObjet = imageDetailsObjet;
            preloaderText= __mainWindowAdvanced.preloaderText.Text;
            __mainWindowAdvanced.preloaderText.Text = string.Format("Resetting changes. Please wait...");
            __mainWindowAdvanced.preLoader.Visibility = Visibility.Visible;
            __mainWindowAdvanced.ShowProgress();
            state_bgWorker.RunWorkerAsync();
        }
        private void state_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            __mainWindowAdvanced.HideProgress();
            __mainWindowAdvanced.preLoader.Visibility = Visibility.Collapsed;
            __mainWindowAdvanced.preloaderText.Text = preloaderText;
        }

        private void state_BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //progresschage events
        }

        private void state_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Bitmap _finalBmp = new Bitmap(__mainWindowAdvanced.images_Folder[_stIndex].Path);
            
                if (_STbrightness != 0)
                {
                    int TempBrightness = 0;
                    if (_STbrightness > 0) { TempBrightness = Convert.ToInt32((2.55 * _STbrightness)); }
                    if (_STbrightness < 0) { TempBrightness = Convert.ToInt32((2.55 * _STbrightness)); }
                    //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-255, +255] default 10
                    if (TempBrightness >= -255 && TempBrightness != 0 && TempBrightness <= 255)
                    {
                        try
                        {
                            BrightnessCorrection filter = new BrightnessCorrection(TempBrightness);
                            //ContrastCorrection filter = new ContrastCorrection(TempBrightness);
                            filter.ApplyInPlace(_finalBmp);
                            //_finalBmp = new Bitmap(bmp);

                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (_STcontrast!=0)
                {
                    int TempContrast = 0;
                    if (_STcontrast > 0) { TempContrast = Convert.ToInt32((1.27 * _STcontrast)); }
                    if (_STcontrast < 0) { TempContrast = Convert.ToInt32((1.27 * _STcontrast)); }
                    //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-127, +127] default 10
                    if (TempContrast >= -127 && TempContrast != 0 && TempContrast <= 127)
                    {
                        try
                        {
                            ContrastCorrection filter = new ContrastCorrection(TempContrast);
                            filter.ApplyInPlace(_finalBmp);
                            //_finalBmp = new Bitmap(bmp);

                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (_STsaturation!=0)
                {
                    float TempSaturation = 0;
                    if (_STsaturation  > 0) { TempSaturation = ((_STsaturation) + (-100)); }
                    if (_STsaturation  < 0) { TempSaturation = (100 + (_STsaturation)); }
                    //The filter accepts 24 and 32 bpp color images for processing, value specified percentage [-1, +1] default 0.1
                    if (TempSaturation >= -100f && TempSaturation != 0f && TempSaturation <= 100f)
                    {
                        try
                        {
                            SaturationCorrection filter = new SaturationCorrection(TempSaturation / 10);
                            filter.ApplyInPlace(_finalBmp);
                            //_finalBmp = new Bitmap(bmp);
                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (_STwhitebalance!=0)
                {
                    int TempWB = 0;
                    if (_STwhitebalance > 0 && _STwhitebalance <= 100) { TempWB = (_STwhitebalance + 55) * 2; }
                    if (_STwhitebalance < 0 && _STwhitebalance >= -100) { TempWB = (_STwhitebalance + 100) / 2; }
                    try
                    {
                        if (TempWB != 0) { _finalBmp = Convert_WhiteBalance(_finalBmp, (TempWB * 100)); }
                    }
                    catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                }

                if (_STwhiteclipping!=0)
                {
                    if ((_STwhiteclipping > 0 && _STwhiteclipping <= 100) || (_STwhiteclipping < 0 && _STwhiteclipping >= -100))
                    {
                        _finalBmp = (Bitmap)Apply_WhiteClipping(_finalBmp,_STwhiteclipping).Clone();
                    }
                }
                if (_STbgFilter!=0)
                {
                    _finalBmp = Apply_BackgroundFilter(_finalBmp,_STbgFilter);
                }

                if (_imageDetailsObjet.croppedImage)
                {
                    _finalBmp = (Bitmap)CropFrame(_finalBmp, _imageDetailsObjet).Clone();
                }
                if (__mainWindowAdvanced.images_Folder[_stIndex].rotateAngle != 0)
                {
                    _finalBmp = new Bitmap(RotateFrame(_imageDetailsObjet.rotateAngle, _finalBmp));
                }
                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = BitmapSourceConvert.CreateWriteableBitmapFromBitmap(_finalBmp);
                if (__mainWindowAdvanced.images_Folder[_stIndex].Frame != null)
                {
                    __mainWindowAdvanced.images_Folder[_stIndex].Frame.Dispose();
                    __mainWindowAdvanced.images_Folder[_stIndex].Frame = null;
                }
                __mainWindowAdvanced.images_Folder[_stIndex].Frame = (Bitmap)_finalBmp.Clone();
                _finalBmp.Dispose();
                _finalBmp = null;
                StateCropAndRotate(_stIndex, _imageDetailsObjet);
            }
            catch (Exception ex) { /*MessageBox.Show(ex.ToString());*/ Log.Debug("State worker: ",ex); }
        }

        private void StateCropAndRotate(int index,ImageDetails img)
        {
            __mainWindowAdvanced.images_Folder[index].rotateAngle = img.rotateAngle;
            __mainWindowAdvanced.images_Folder[index].croppedImage = img.croppedImage;
            __mainWindowAdvanced.images_Folder[index].crop_X = img.crop_X;
            __mainWindowAdvanced.images_Folder[index].crop_Y = img.crop_Y;
            __mainWindowAdvanced.images_Folder[index].crop_W = img.crop_W;
            __mainWindowAdvanced.images_Folder[index].crop_H = img.crop_H;
            __mainWindowAdvanced.images_Folder[index].resizeH = img.resizeH;
            __mainWindowAdvanced.images_Folder[index].resizeW = img.resizeW;
        }

        public Bitmap CropFrame(Bitmap frame, ImageDetails imageDetail)
        {
            try
            {
                var _x = imageDetail.crop_X;
                var _y = imageDetail.crop_Y;
                var _w = imageDetail.crop_W;
                var _h = imageDetail.crop_H;
                if ((_y + _h) > imageDetail.resizeH)
                {
                    _h = (int)(imageDetail.resizeH - _y);
                    if (_h < 0) { _h = 0; }
                }
                if ((_x + _w) > imageDetail.resizeW)
                {
                    _w = (int)(imageDetail.resizeW - _x);
                    if (_x < 0) { _x = 0; }
                }
                using (Bitmap b = (Bitmap)__mainWindowAdvanced.ResizeBitmap(frame, imageDetail.resizeW, imageDetail.resizeH))
                {
                    using (Bitmap bmpCrop = b.Clone(new Rectangle(_x, _y, _w - 1, _h - 1), b.PixelFormat))
                    {
                        frame = (Bitmap)bmpCrop.Clone();
                    }
                }
            }
            catch (Exception ex) { Log.Debug("Crop image exception: ", ex); }
            return frame;
        }

        public void SetMementoValues(int index)
        {
            __mainWindowAdvanced.images_Folder[index].BackgroundFilter = BackgroundFilter;
            __mainWindowAdvanced.images_Folder[index].Contrast = Contrast;
            __mainWindowAdvanced.images_Folder[index].Brightness = Brightness;
            __mainWindowAdvanced.images_Folder[index].Saturation = Saturation;
            __mainWindowAdvanced.images_Folder[index].WhiteBalance = WhiteBalance;
            __mainWindowAdvanced.images_Folder[index].WhiteClipping = WhiteClipping;
        }

        public BackgroundWorker FilterCorrection_bgWorker = new BackgroundWorker();

        private void FilterCorrection_BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!ApplyAll)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    __mainWindowAdvanced.HideProgress();
                    __mainWindowAdvanced.preLoader.Visibility = Visibility.Collapsed;
                    __mainWindowAdvanced.preloaderText.Text = loaderText;
                });
            }
            sourcefile = null;
            destfile = null;
            ApplyAll = false;

        }

        private void FilterCorrection_BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (sourcefile == "" || sourcefile == null) { return; }
                int ind = getIndex(Path.GetFileName(sourcefile));
                
                try
                {
                    Bitmap _finalBmp;
                    if (Brightness != 0 || WhiteClipping != 0 || _whiteBalance != 0 || Contrast != 0 || Saturation != 0 || BackgroundFilter != 0)
                    {
                        if (!ApplyAll && __mainWindowAdvanced.images_Folder[ind].IsEdited == false) { __mainWindowAdvanced.UnDoObject.SetStateForUndoRedo(new Memento(ind, new ImageDetails(__mainWindowAdvanced.images_Folder[ind]))); }
                    }
                    else { return; }
                    _finalBmp = new Bitmap(__mainWindowAdvanced.images_Folder[ind].Path);

                    if (IsBrightnessApply)
                    {
                        int TempBrightness = 0;
                        if (Brightness > 0) { TempBrightness = Convert.ToInt32((2.55 * Brightness)); }
                        if (Brightness < 0) { TempBrightness = Convert.ToInt32((2.55 * Brightness)); }
                        //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-255, +255] default 10
                        if (TempBrightness >= -255 && TempBrightness != 0 && TempBrightness <= 255)
                        {
                            try
                            {
                                BrightnessCorrection filter = new BrightnessCorrection(TempBrightness);
                                //ContrastCorrection filter = new ContrastCorrection(TempBrightness);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);

                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsContrastApply)
                    {
                        int TempContrast = 0;
                        if (Contrast > 0) { TempContrast = Convert.ToInt32((1.27 * Contrast)); }
                        if (Contrast < 0) { TempContrast = Convert.ToInt32((1.27 * Contrast)); }
                        //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-127, +127] default 10
                        if (TempContrast >= -127 && TempContrast != 0 && TempContrast <= 127)
                        {
                            try
                            {
                                ContrastCorrection filter = new ContrastCorrection(TempContrast);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);

                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsSaturationApply)
                    {
                        float TempSaturation = 0;
                        if (Saturation > 0) { TempSaturation = ((Saturation) + (-100)); }
                        if (Saturation < 0) { TempSaturation = (100 + (Saturation)); }
                        //The filter accepts 24 and 32 bpp color images for processing, value specified percentage [-1, +1] default 0.1
                        if (TempSaturation >= -100f && TempSaturation != 0f && TempSaturation <= 100f)
                        {
                            try
                            {
                                SaturationCorrection filter = new SaturationCorrection(TempSaturation / 10);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);
                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsWhiteBalanceApply)
                    {
                        int TempWB = 0;
                        if (_whiteBalance > 0 && _whiteBalance <= 100) { TempWB = (_whiteBalance + 55) * 2; }
                        if (_whiteBalance < 0 && _whiteBalance >= -100) { TempWB = (_whiteBalance + 100) / 2; }
                        try
                        {
                            if (TempWB != 0) { _finalBmp = Convert_WhiteBalance(_finalBmp, (TempWB * 100)); }
                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }

                    if (IsWhiteClippingApply)
                    {
                        if ((WhiteClipping > 0 && WhiteClipping <= 100) || (WhiteClipping < 0 && WhiteClipping >= -100))
                        {
                            _finalBmp = (Bitmap)Apply_WhiteClipping(_finalBmp, WhiteClipping).Clone();
                        }
                    }
                    if (BackgroundFilter > 1 && BackgroundFilter <= 100 && IsBackgroundFilterApply)
                    {
                        _finalBmp = Apply_BackgroundFilter(_finalBmp, BackgroundFilter);
                    }

                    if (destfile == "" || destfile == null)
                    {
                        if (Brightness != 0 || WhiteClipping != 0 || _whiteBalance != 0 || Contrast != 0 || Saturation != 0 || BackgroundFilter != 0)
                        {
                            if (__mainWindowAdvanced.images_Folder[ind].croppedImage)
                            {
                                _finalBmp = (Bitmap)__mainWindowAdvanced.CropFrame(_finalBmp, ind).Clone();

                            }
                            if (__mainWindowAdvanced.images_Folder[ind].rotateAngle != 0)
                            {
                                _finalBmp = new Bitmap(RotateFrame(__mainWindowAdvanced.images_Folder[ind].rotateAngle, _finalBmp));
                            }
                            if (__mainWindowAdvanced.images_Folder[ind].Frame != null)
                            {
                                __mainWindowAdvanced.images_Folder[ind].Frame.Dispose();
                                __mainWindowAdvanced.images_Folder[ind].Frame = null;
                            }

                            if (!ApplyAll && getDiff())
                            {
                                ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = BitmapSourceConvert.CreateWriteableBitmapFromBitmap(_finalBmp);
                                SetMementoValues(ind);
                                __mainWindowAdvanced.UnDoObject.SetStateForUndoRedo(new Memento(ind, new ImageDetails(__mainWindowAdvanced.images_Folder[ind])));
                            }

                            __mainWindowAdvanced.images_Folder[ind].Frame = (Bitmap)_finalBmp.Clone();
                            __mainWindowAdvanced.images_Folder[ind].IsEdited = true;
                            _finalBmp.Dispose();
                            _finalBmp = null;
                            setDiff();
                        }
                    }
                    if (destfile != "" && destfile != null)
                    {
                        if (File.Exists(destfile)) { File.Delete(destfile); }

                        if (__mainWindowAdvanced.images_Folder[ind].croppedImage)
                        {
                            _finalBmp = (Bitmap)__mainWindowAdvanced.CropFrame(_finalBmp, ind).Clone();
                        }
                        if (__mainWindowAdvanced.images_Folder[ind].rotateAngle != 0)
                        {
                            _finalBmp = new Bitmap(RotateFrame(__mainWindowAdvanced.images_Folder[ind].rotateAngle, _finalBmp));
                        }
                        StaticClass.saveBitmap2File(_finalBmp, destfile);
                        if (__mainWindowAdvanced.images_Folder[ind].Frame != null) { __mainWindowAdvanced.images_Folder[ind].Frame.Dispose(); }

                        _finalBmp.Dispose();
                    }
                }
                catch (Exception ex) { Log.Debug("Filter Correction error: ", ex); }
                System.GC.Collect();

            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void FilterApply(string Sourcefile,string Destfile)
        {
            try
            {
                if (Sourcefile == "" || Sourcefile == null) { return; }
                int ind = getIndex(Path.GetFileName(Sourcefile));
                try
                {
                    Bitmap _finalBmp;
                    _finalBmp = new Bitmap(__mainWindowAdvanced.images_Folder[ind].Path);
                    if (IsBrightnessApply)
                    {
                        int TempBrightness = 0;
                        if (Brightness > 0) { TempBrightness = Convert.ToInt32((2.55 * Brightness)); }
                        if (Brightness < 0) { TempBrightness = Convert.ToInt32((2.55 * Brightness)); }
                        //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-255, +255] default 10
                        if (TempBrightness >= -255 && TempBrightness != 0 && TempBrightness <= 255)
                        {
                            try
                            {
                                BrightnessCorrection filter = new BrightnessCorrection(TempBrightness);
                                //ContrastCorrection filter = new ContrastCorrection(TempBrightness);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);

                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsContrastApply)
                    {
                        int TempContrast = 0;
                        if (Contrast > 0) { TempContrast = Convert.ToInt32((1.27 * Contrast)); }
                        if (Contrast < 0) { TempContrast = Convert.ToInt32((1.27 * Contrast)); }
                        //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-127, +127] default 10
                        if (TempContrast >= -127 && TempContrast != 0 && TempContrast <= 127)
                        {
                            try
                            {
                                ContrastCorrection filter = new ContrastCorrection(TempContrast);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);

                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsSaturationApply)
                    {
                        float TempSaturation = 0;
                        if (Saturation > 0) { TempSaturation = ((Saturation) + (-100)); }
                        if (Saturation < 0) { TempSaturation = (100 + (Saturation)); }
                        //The filter accepts 24 and 32 bpp color images for processing, value specified percentage [-1, +1] default 0.1
                        if (TempSaturation >= -100f && TempSaturation != 0f && TempSaturation <= 100f)
                        {
                            try
                            {
                                SaturationCorrection filter = new SaturationCorrection(TempSaturation / 10);
                                filter.ApplyInPlace(_finalBmp);
                                //_finalBmp = new Bitmap(bmp);
                            }
                            catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                        }
                    }

                    if (IsWhiteBalanceApply)
                    {
                        int TempWB = 0;
                        if (_whiteBalance > 0 && _whiteBalance <= 100) { TempWB = (_whiteBalance + 55) * 2; }
                        if (_whiteBalance < 0 && _whiteBalance >= -100) { TempWB = (_whiteBalance + 100) / 2; }
                        try
                        {
                            if (TempWB != 0) { _finalBmp = Convert_WhiteBalance(_finalBmp, (TempWB * 100)); }
                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }

                    if (IsWhiteClippingApply)
                    {
                        if ((WhiteClipping > 0 && WhiteClipping <= 100) || (WhiteClipping < 0 && WhiteClipping >= -100))
                        {
                            _finalBmp = (Bitmap)Apply_WhiteClipping(_finalBmp, WhiteClipping).Clone();
                        }
                    }
                    if (BackgroundFilter > 1 && BackgroundFilter <= 100 && IsBackgroundFilterApply)
                    {
                        _finalBmp = Apply_BackgroundFilter(_finalBmp, BackgroundFilter);
                    }

                    if (Destfile != "" && Destfile != null)
                    {
                        if (File.Exists(Destfile)) { File.Delete(Destfile); }

                        if (__mainWindowAdvanced.images_Folder[ind].croppedImage)
                        {
                            _finalBmp = (Bitmap)__mainWindowAdvanced.CropFrame(_finalBmp, ind).Clone();
                        }
                        if (__mainWindowAdvanced.images_Folder[ind].rotateAngle != 0)
                        {
                            _finalBmp = new Bitmap(RotateFrame(__mainWindowAdvanced.images_Folder[ind].rotateAngle, _finalBmp));
                        }
                        StaticClass.saveBitmap2File(_finalBmp, Destfile);
                        if (__mainWindowAdvanced.images_Folder[ind].Frame != null) { __mainWindowAdvanced.images_Folder[ind].Frame.Dispose(); }
                        _finalBmp.Dispose();
                    }
                }
                catch (Exception ex) { Log.Debug("Filter Correction error: ", ex); }
                System.GC.Collect();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
    }
}
