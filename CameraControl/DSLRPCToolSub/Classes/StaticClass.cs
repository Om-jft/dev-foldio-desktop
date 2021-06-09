using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using DSLR_Tool_PC.ViewModels;
using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Drawing2D;

namespace DSLR_Tool_PC
{
    public static class StaticClass
    { 
        private const int MaxThumbSize = 1920 * 2;
        public const int LargeThumbSize = 1600;
        public const int SmallThumbSize = 400;

        public static bool IsCapturedPhoto = false;
        public static string FileName_LastCapturedImage = "";
        public static int CapturedImageCount = 0;

        //public static double ImageMouse_X = 0;
        //public static double ImageMouse_Y = 0;

        public static ImageDetails ImageListBoxSelectedItem = null;
        public static int ImageListBoxSelectedIndex = -1;

        public static int TypeOfCapture_SelectedTabIndex = 0;
        public static bool Is360CaptureProcess = false;

        public static bool __CaptureInSdRam = true;
        public static int __Ratio_Diff_Width = 0;
        public static int __Ratio_Diff_Height = 0;
        public static byte[] ConvertImageToByteArray(string imagePath)
        {
            byte[] imageByteArray = null;
            //imageByteArray = File.ReadAllBytes(imagePath);
            FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                imageByteArray = new byte[reader.BaseStream.Length];
                for (int i = 0; i < reader.BaseStream.Length; i++)
                    imageByteArray[i] = reader.ReadByte();
            }
            return imageByteArray;
        }

        public static byte[] SaveJpeg(WriteableBitmap image)
        {
            var enc = new JpegBitmapEncoder();
            //enc.QualityLevel = 50;
            enc.Frames.Add(BitmapFrame.Create(image));

            using (MemoryStream stm = new MemoryStream())
            {
                enc.Save(stm);
                return stm.ToArray();
            }
        }

        public static byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }

        public static System.Drawing.Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        //get python path from registry key
        public static string GetPythonPathFromRegistry()
        {
            //const string regKey = "Python";
            //string pythonPath = null;
            //try
            //{
            //    RegistryKey registryKey = Registry.LocalMachine;
            //    RegistryKey subKey = registryKey.OpenSubKey("SOFTWARE");
            //    if (subKey == null)
            //        return null;

            //    RegistryKey esriKey = subKey.OpenSubKey("ESRI");
            //    if (esriKey == null)
            //        return null;

            //    string[] subkeyNames = esriKey.GetSubKeyNames();//get all keys under "ESRI" key
            //    int index = -1;
            //    /*"Python" key contains arcgis version no in its name. So, the key name may be 
            //    varied version to version. For ArcGIS10.0, key name is: "Python10.0". So, from
            //    here I can get ArcGIS version also*/
            //    for (int i = 0; i < subkeyNames.Length; i++)
            //    {
            //        if (subkeyNames[i].Contains("Python"))
            //        {
            //            index = i;
            //            break;
            //        }
            //    }
            //    if (index < 0)
            //        return null;
            //    RegistryKey pythonKey = esriKey.OpenSubKey(subkeyNames[index]);

            //    string arcgisVersion = subkeyNames[index].Remove(0, 6); //remove "python" and get the version
            //    var pythonValue = pythonKey.GetValue("Python") as string;
            //    if (pythonValue != "True")//I guessed the true value for python says python is installed with ArcGIS.
            //        return "";

            //    var pythonDirectory = pythonKey.GetValue("PythonDir") as string;
            //    if (pythonDirectory != null && Directory.Exists(pythonDirectory))
            //    {
            //        string pythonPathFromReg = pythonDirectory + "ArcGIS" + arcgisVersion + "\\python.exe";
            //        if (File.Exists(pythonPathFromReg))
            //            pythonPath = pythonPathFromReg;
            //    }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e + "\r\nReading registry " + regKey.ToUpper());
            //    pythonPath = null;
            //}
            string _PyScriptFile = Path.Combine(Settings.ApplicationFolder, "python-3.9.4\\python.exe");
            
            return _PyScriptFile;
        }

        //get python path from environtment variable
        private static string GetPythonPath()
        {
            string __returnPath = "";
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            string pathVariable = environmentVariables["Path"] as string;
            if (pathVariable != null)
            {
                string[] allPaths = pathVariable.Split(';');
                foreach (var path in allPaths)
                {
                    string pythonPathFromEnv = path + @"\python.exe";
                    if (File.Exists(pythonPathFromEnv))
                    {
                        __returnPath = pythonPathFromEnv;
                        break;
                    }
                }
                if (__returnPath == null || __returnPath == "")
                {
                    //var thread = new Thread(DownLoadAndInstallPython);
                    //thread.Start();
                }
            }
            return __returnPath;
        }

        private static bool RunPythonFile_cmdMode(string _arguments)
        {
            bool _returnValue = true;
            //int a = 1;

            //string s = GetPythonPathFromRegistry();

            //IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            //string pathVariable = environmentVariables["Path"] as string;
            //if (pathVariable != null)
            //{
            //    string[] allPaths = pathVariable.Split(';');
            //    foreach (var path in allPaths)
            //    {
            //        // var paths = @"C:\Windows\SysNative";
            //        //string pythonPathFromEnv = Path.Combine(path, "python.exe");
            //        //if (File.Exists(pythonPathFromEnv))
            //        //{
            //        //    Log.Debug(pythonPathFromEnv);
            //        //    _returnValue = RunPythonFile_cmdMode(_arguments, pythonPathFromEnv);
            //        //    if (_returnValue == true) { break; }
            //        //}

            //        //bool IsFileexist = File.Exists(pythonPathFromEnv) ? true : false;
            //        //if (IsFileexist)
            //        //{
            //        //    Log.Debug(pythonPathFromEnv);
            //        //    _returnValue = RunPythonFile_cmdMode(_arguments, pythonPathFromEnv);
            //        //    if (_returnValue == true) { break; }
            //        //}

            //    }
            //}
            string _PyScriptFile = Path.Combine(Settings.ApplicationFolder, "python-3.9.4\\python.exe");
            _returnValue = RunPythonFile_cmdMode(_arguments, _PyScriptFile);
            return _returnValue;
        }

        static string GetPythonExecutablePath(int major = 3)
        {
            var software = "SOFTWARE";
            var key = Registry.CurrentUser.OpenSubKey(software);
            if (key == null)
                key = Registry.LocalMachine.OpenSubKey(software);
            if (key == null)
                return null;

            var pythonCoreKey = key.OpenSubKey(@"Python\PythonCore");
            if (pythonCoreKey == null)
                pythonCoreKey = key.OpenSubKey(@"Wow6432Node\Python\PythonCore");
            if (pythonCoreKey == null)
                return null;

            var pythonVersionRegex = new Regex("^" + major + @"\.(\d+)-(\d+)$");
            var targetVersion = pythonCoreKey.GetSubKeyNames().
                                                Select(n => pythonVersionRegex.Match(n)).
                                                Where(m => m.Success).
                                                OrderByDescending(m => int.Parse(m.Groups[1].Value)).
                                                ThenByDescending(m => int.Parse(m.Groups[2].Value)).
                                                Select(m => m.Groups[0].Value).First();

            var installPathKey = pythonCoreKey.OpenSubKey(targetVersion + @"\InstallPath");
            if (installPathKey == null)
                return null;

            return (string)installPathKey.GetValue("ExecutablePath");
        }

        private static bool RunPythonFile_cmdMode(string _arguments, string PythonInstallPath)
        {
            bool _returnValue = true;

            try
            {
                //string PythonInstallPath = GetPythonPath();
                var psi = new ProcessStartInfo();
                psi.FileName = PythonInstallPath;

                //psi.FileName = @"C:\PythonInstall\python.exe";
                //var script = @"E:\Im3.py";
                //var path = @"E:\Test.Jpg";
                //var modelPath = @"E:\model.yml";
                //var outputPath = @"E:\Projects\Tester.jpg";
                //var gaussian_Kernal = 5;
                //psi.Arguments = string.Format("{0} {1} {2} {3} {4}", script, path, modelPath, outputPath, gaussian_Kernal);

                psi.Arguments = _arguments;

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                var errors = "";
                var results = "";
                Log.Debug(psi.Arguments);
                Log.Debug(psi.FileName);
                using (var process = Process.Start(psi))
                {
                    process.WaitForExit();

                    errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    results = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }

                if (errors != "")
                {
                    Log.Debug("Err:" + errors);
                    //MessageBox.Show(errors);
                    //if (errors.Contains("Python was not found"))
                    //{
                    //    //var thread = new Thread(DownLoadAndInstallPython);
                    //    //thread.Start();
                    //}
                    _returnValue = false;
                }
                else
                {
                    Log.Debug("Res:" + results);
                    //MessageBox.Show(results);
                    _returnValue = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to set property ", ex);
            }

            return _returnValue;
        }

        private static void DownLoadAndInstallPython()
        {
            Process proc = null;
            try
            {
                string batDir = Path.Combine(Settings.ApplicationFolder, "pyFiles");
                proc = new Process();
                proc.StartInfo.WorkingDirectory = batDir;
                proc.StartInfo.FileName = "DownloadInstallPython.bat";
                proc.StartInfo.CreateNoWindow = false;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        public static bool RemoveBG_usingPy(string _SourcePath, string _DestinationPath, int _GaussianKernal)
        {
            string _PyScriptFile = Path.Combine(Settings.ApplicationFolder, "remove_bg.py");
            string _ModelPath = Path.Combine(Settings.ApplicationFolder, "model.yml");
            string argv = string.Format("{0} {1} {2} {3} {4}", _PyScriptFile, _ModelPath, _SourcePath, _DestinationPath, _GaussianKernal);
            string commandText =  "\"" + _PyScriptFile + "\"" + " " + "\"" + _ModelPath + "\"" + " " + "\"" + _SourcePath + "\"" + " " + "\"" + _DestinationPath + "\"" + " " + "\"" + _GaussianKernal + "\"";
            return RunPythonFile_cmdMode(commandText);
        }

        public static bool WhiteClipping_usingPy(string _SourcePath, string _DestinationPath, int _WCValue, int _SingleOrMulti)
        {
            string _PyScriptFile = Path.Combine(Settings.ApplicationFolder, "whiteClipping.py");
            //string _PyScriptFile = @"E:\whiteClipping.py";
            string argv = string.Format("{0} {1} {2} {3} {4}", _PyScriptFile, _SourcePath, _DestinationPath, _WCValue, _SingleOrMulti);
            string commandText = "\"" + _PyScriptFile + "\"" + " " + "\"" + _SourcePath + "\"" + " " + "\"" + _DestinationPath + "\"" + " " + "\"" + _WCValue + "\"" + " " + "\"" + _SingleOrMulti + "\"";
            return RunPythonFile_cmdMode(commandText);
        }

        public static bool SaveasUsedImage_usingPy(string _SourcePath, string _DestinationPath)
        {
            string _PyScriptFile = Path.Combine(Settings.ApplicationFolder, "SaveAsImage.py");
            //string _PyScriptFile = @"E:\whiteClipping.py";
            string argv = string.Format("{0} {1} {2} ", _PyScriptFile, _SourcePath, _DestinationPath);
            return RunPythonFile_cmdMode(argv);
        }

        public static bool CameraParametersChanging(string cmd)
        {
            bool _retunStatus = false;
            ICameraDevice device = ServiceProvider.DeviceManager.SelectedCameraDevice;

            do { Thread.Sleep(100); } while (device.IsBusy == true); ;

            switch (cmd)
            {
                case "ModeNext":
                    device.Mode.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "ModePrev":
                    device.Mode.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "IsoNext":
                    device.IsoNumber.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "IsoPrev":
                    device.IsoNumber.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "ShutterSpeedNext":
                    device.ShutterSpeed.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "ShutterSpeedPrev":
                    device.ShutterSpeed.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "ApertureNext":
                    device.FNumber.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "AperturePrev":
                    device.FNumber.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "WBNext":
                    device.WhiteBalance.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "WBPrev":
                    device.WhiteBalance.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "ECompNext":
                    device.ExposureCompensation.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "ECompPrev":
                    device.ExposureCompensation.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "CompressionNext":
                    device.CompressionSetting.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "CompressionPrev":
                    device.CompressionSetting.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "EMeterNext":
                    device.ExposureMeteringMode.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "EMeterPrev":
                    device.ExposureMeteringMode.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                case "FocusNext":
                    device.FocusMode.NextValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;
                case "FocusrPrev":
                    device.FocusMode.PrevValue();
                    Thread.Sleep(100);
                    _retunStatus = true;
                    break;

                default:
                    Thread.Sleep(100);
                    break;
            }

            return _retunStatus;
        }

        public static bool GenerateSmallThumb(string filename, string destname) { return GenerateThumb(filename, destname, 0); }
        public static bool GenerateLargeThumb(string filename, string destname) { return GenerateThumb(filename, destname, 1); }

        private static bool GenerateThumb(string filename, string destname, int _LorS)
        {
            bool _returnStatus = false;

            if (File.Exists(destname))
            {

            }
            using (MagickImage image = new MagickImage(filename))
            {
                double dw = 1;
                if (_LorS == 1)
                {
                    dw = (double)LargeThumbSize / image.Width;
                }
                if (_LorS == 0)
                {
                    dw = (double)SmallThumbSize / image.Width;
                }

                image.FilterType = FilterType.Box;
                image.Thumbnail((int)(image.Width * dw), (int)(image.Height * dw));

                if (!ServiceProvider.Settings.DisableHardwareAccelerationNew)
                    image.UnsharpMask(1, 1, 0.5, 0.1);

                PhotoUtils.CreateFolder(destname);
                image.Write(destname);
                image.Dispose();

                _returnStatus = true;
            }
            return _returnStatus;
        }

        public static DependencyObject GetParentDependencyObjectFromVisualTree(DependencyObject startObject, Type type)
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

        public static void saveBitmap2File(Bitmap b, string fileName)
        {
            var formatFile = fileName;
            //formatFile = formatFile.Substring(formatFile.Length - 3);
            formatFile = System.IO.Path.GetExtension(formatFile).ToString().Replace(".", "").ToLower();
           
            switch (formatFile)
            {
                case "bmp":
                    b.Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    break;
                case "jepg":
                case "jpg":
                    b.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case "png":
                    b.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                    break;
                case "gif":
                    b.Save(fileName, System.Drawing.Imaging.ImageFormat.Gif);
                    break;
                case "tiff":
                    b.Save(fileName, System.Drawing.Imaging.ImageFormat.Tiff);
                    break;
            }
           
        }

        public static void compressimagesize(string target, Bitmap bitmap)
        {
            using (var image = (Image)bitmap.Clone())
            {
                double scaleFactor = 0.3;
                var imgnewwidth = (int)(image.Width * scaleFactor);
                var imgnewheight = (int)(image.Height * scaleFactor);
                var imgthumnail = new Bitmap(imgnewwidth, imgnewheight);
                var imgthumbgraph = Graphics.FromImage(imgthumnail);
                imgthumbgraph.CompositingQuality = CompositingQuality.Default;
                imgthumbgraph.SmoothingMode = SmoothingMode.HighQuality;
                imgthumbgraph.InterpolationMode = InterpolationMode.Default;
                var imageRectangle = new Rectangle(0, 0, imgnewwidth, imgnewheight);
                imgthumbgraph.DrawImage(image, imageRectangle);
                imgthumnail.Save(target, image.RawFormat);
                //return (Bitmap)image.Clone();
               
            }
        }
    }
}
