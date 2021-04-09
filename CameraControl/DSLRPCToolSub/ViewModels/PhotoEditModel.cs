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

namespace DSLR_Tool_PC.ViewModels
{
    public class PhotoEditModel : BaseFieldClass
    {
        private readonly object _Sliderlockobj = new object();
        public RelayCommand ApplyAllFrames { get; set; }

        PathUpdate __PathUpdate = PathUpdate.getInstance();
        //EditLeftControl __editLeftControl = null;
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
        }

        PhotoEditModel()
        {
            ApplyAllFrames = new RelayCommand(Start);

            ResetAllControls();
        }

        private static PhotoEditModel _photoeditmodel_inst = null;
        public static PhotoEditModel GetInstance()
        {
            if (_photoeditmodel_inst == null) { _photoeditmodel_inst = new PhotoEditModel(); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                        if (!Monitor.Wait(_Sliderlockobj, 3))
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
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
                        if (!Monitor.Wait(_Sliderlockobj, 5))
                        {
                            if (ServiceProvider.Settings.SelectedBitmap.DisplayEditImage != null) { Task.Factory.StartNew(EditFiltersApply); }
                        }
                });
            }
        }


        public void FiltersCorrections(string sourcefile, string destfile)
        {
            if (sourcefile == "" || sourcefile == null) { return; }
            //if (destfile == "" || destfile == null) { return; }

            Bitmap _finalBmp;
            using (Bitmap bmp = new Bitmap(sourcefile))
            {
                if (IsBrightnessApply)
                {
                    int TempBrightness = 0;
                    if (Brightness > 0) { TempBrightness = Convert.ToInt32((-255) + (5.10 * Brightness)); }
                    if (Brightness < 0) { TempBrightness = Convert.ToInt32((255) + (5.10 * Brightness)); }
                    //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-255, +255] default 10
                    if (TempBrightness >= -255 && TempBrightness != 0 && TempBrightness <= 255)
                    {
                        try
                        {
                            ContrastCorrection filter = new ContrastCorrection(TempBrightness);
                            filter.ApplyInPlace(bmp);
                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (IsContrastApply)
                {
                    int TempContrast = 0;
                    if (Contrast > 0) { TempContrast = Convert.ToInt32((-127) + (2.54 * Contrast)); }
                    if (Contrast < 0) { TempContrast = Convert.ToInt32((127) + (2.54 * Contrast)); }
                    //The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing, value [-127, +127] default 10
                    if (TempContrast >= -127 && TempContrast != 0 && TempContrast <= 127)
                    {
                        try
                        {
                            ContrastCorrection filter = new ContrastCorrection(TempContrast);
                            filter.ApplyInPlace(bmp);
                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (IsSaturationApply)
                {
                    float TempSaturation = 0;
                    if (Saturation > 0) { TempSaturation = ((-100) + (2 * Saturation)); }
                    if (Saturation < 0) { TempSaturation = ((100) + (2 * Saturation)); }
                    //The filter accepts 24 and 32 bpp color images for processing, value specified percentage [-1, +1] default 0.1
                    if (TempSaturation >= -100f && TempSaturation != 0f && TempSaturation <= 100f)
                    {
                        try
                        {

                            SaturationCorrection filter = new SaturationCorrection(TempSaturation / 100);
                            filter.ApplyInPlace(bmp);

                        }
                        catch (Exception ex) { Log.Debug("FiltersCorrections", ex); }
                    }
                }

                if (_whiteBalance > 0 && _whiteBalance <= 100 && IsWhiteBalanceApply)
                {
                    _finalBmp = Convert_WhiteBalance(bmp, (_whiteBalance * 100));
                }
                if (_whiteBalance < 0 && _whiteBalance >= -100 && IsWhiteBalanceApply)
                {
                    _finalBmp = Convert_WhiteBalance(bmp, (_whiteBalance * 100));
                }
                else { _finalBmp = bmp; }

                if (WhiteClipping > 0 && WhiteClipping <= 100 && IsWhiteClippingApply)
                {
                    _finalBmp = Apply_WhiteClipping(_finalBmp);
                }

                if (BackgroundFilter > 0 && BackgroundFilter <= 100 && IsBackgroundFilterApply)
                {
                    _finalBmp = Apply_BackgroundFilter(_finalBmp);
                }

                if (destfile == "" || destfile == null)
                {
                    WriteableBitmap writeableBitmap = BitmapSourceConvert.CreateWriteableBitmapFromBitmap(_finalBmp);
                    ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = writeableBitmap;
                }
            }
            if (destfile != "" && destfile != null)
            {
                if (File.Exists(destfile)) { File.Delete(destfile); }
                StaticClass.saveBitmap2File(_finalBmp, destfile);
            }
            //else
            //{
            //    WriteableBitmap writeableBitmap = BitmapSourceConvert.CreateWriteableBitmapFromBitmap(_finalBmp);
            //    ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = writeableBitmap;
            //}
            //}
        }

        private Bitmap Apply_WhiteClipping(Bitmap _bmpImage)
        {
            string tempFile_In = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName().Replace(".", "") + "." + ImageFormat.Jpeg);

            if (File.Exists(tempFile_In))
                File.Delete(tempFile_In);

            _bmpImage.Save(tempFile_In, System.Drawing.Imaging.ImageFormat.Jpeg);

            int TempWhiteClipping = 0;
            if (WhiteClipping > 0) { TempWhiteClipping = Convert.ToInt32(WhiteClipping * 2.55); }
            StaticClass.WhiteClipping_usingPy(tempFile_In, tempFile_In, TempWhiteClipping, 0);

            Bitmap bmp = (Bitmap)Image.FromFile(tempFile_In);

            //if (File.Exists(tempFile_In))
            //    File.Delete(tempFile_In);

            return bmp;
        }

        private Bitmap Apply_BackgroundFilter(Bitmap _bmpImage)
        {
            Bitmap _returnBmp = null;
            try
            {
                string tempFile_In = Path.Combine(Settings.ApplicationTempFolder, Path.GetRandomFileName().Replace(".", "") + "." + ImageFormat.Jpeg);

                if (File.Exists(tempFile_In))
                    File.Delete(tempFile_In);

                _bmpImage.Save(tempFile_In, System.Drawing.Imaging.ImageFormat.Jpeg);

                int TempBgFilter = 0;
                if (BackgroundFilter > 0)
                {
                    if ((BackgroundFilter % 2) == 0) { TempBgFilter = BackgroundFilter + 1; } else { TempBgFilter = BackgroundFilter; }
                }
                StaticClass.RemoveBG_usingPy(tempFile_In, tempFile_In, TempBgFilter);
                Log.Debug(tempFile_In);

                Bitmap bmp = (Bitmap)Image.FromFile(tempFile_In);
                _returnBmp = bmp;

                //bmp.Dispose();

                //if (File.Exists(tempFile_In))
                //    File.Delete(tempFile_In);
            }
            catch (Exception ex) { Log.Debug("Apply_BackgroundFilter", ex); }

            return _returnBmp;
        }


        private void ResetAllControls()
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
            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),
                                                            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_pixelBuffer, 0, resultData.Scan0, _pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }

        private void Start()
        {
            string _pathImag = __PathUpdate.PathImg;//.Substring(8);
            if (_pathImag == null) { return; }
            ImageDetails _imageDetails = __PathUpdate.__SelectedImageDetails;
            if (_imageDetails == null) { return; }

            DirectoryInfo _dirInfoApplPath = new DirectoryInfo(System.IO.Path.GetDirectoryName(_imageDetails.Path_Orginal));
            string[] _pathImagFiles = Directory.GetFiles(_dirInfoApplPath.ToString());

            string _strApplPath = System.IO.Path.Combine(_dirInfoApplPath.ToString(), "JPG_ORG");
            if (!Directory.Exists(_strApplPath))
                Directory.CreateDirectory(_strApplPath);

            foreach (var _imgfl in _pathImagFiles)
            {
                try
                {
                    ////string TargetPath = "";
                    string _exFileName = System.IO.Path.GetFileName(_imgfl);
                    CopyBackUp(_imgfl, Path.Combine(_strApplPath, _exFileName));
                    FiltersCorrections(_imgfl, _imgfl);
                }
                catch (Exception ex) { }
            }
            MessageBox.Show("Apply All Frames and Saved Successfully...!", "Photo Edit", MessageBoxButton.OK, MessageBoxImage.Information);
            __mainWindowAdvanced.BrowseReload(_dirInfoApplPath.ToString());
            __PathUpdate.__SelectedImageDetails = null;
            __PathUpdate.PathImg = "";
            ServiceProvider.Settings.EditImageByte = null;
            ServiceProvider.Settings.SelectedBitmap.DisplayEditImage = null;
            ResetAllControls();
        }

        private void EditFiltersApply()
        {
            FiltersCorrections(__PathUpdate.PathImg, "");
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
                //Log.Error("Unable to make backup ", ex);
                return "";
            }
        }
    }
}
