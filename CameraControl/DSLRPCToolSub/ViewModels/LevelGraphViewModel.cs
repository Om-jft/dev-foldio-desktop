using System;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using Accord.Imaging;
using Accord.Math;
using System.Windows.Media;
using Point = System.Windows.Point;
using System.Windows.Media.Imaging;
using CameraControl.Core.Classes;
using System.IO;
using System.Threading;
using CameraControl.Core;

namespace DSLR_Tool_PC.ViewModels
{
    public class LevelGraphViewModel : BaseFieldClass
    {
        private readonly object _Sliderlockobj = new object();
        private PointCollection _L = null;
        public PointCollection L { get { return _L; } set { if (_L != value) { _L = value; NotifyPropertyChanged("L"); } } }

        private PointCollection _R = null;
        public PointCollection R { get { return _R; } set { _R = value; NotifyPropertyChanged("R"); } }

        private PointCollection _G = null;
        public PointCollection G { get { return _G; } set { _G = value; NotifyPropertyChanged("G"); } }

        private PointCollection _B = null;
        public PointCollection B { get { return _B; } set { _B = value; NotifyPropertyChanged("B"); } }

        private string _ImagePath;
        public string ImagePath
        {
            get { return _ImagePath; }
            set { _ImagePath = value; Task.Factory.StartNew(DrawLevelGraph); }
        }
        private string _ImageBitmap;
        public string ImageBitmap
        {
            get { return _ImageBitmap; }
            set { _ImageBitmap = value; Task.Factory.StartNew(DrawLevelGraph); }
        }

        public void DrawLevelGraph()
        {
            try
            {
                if (ImagePath != null && ImagePath != "")
                {
                    using (Bitmap bmp = new Bitmap(ImagePath))
                    {
                        ImageStatisticsHSL hslStatistics = new ImageStatisticsHSL(bmp);
                        L = ConvertToPointCollection(hslStatistics.Luminance.Values);
                        ImageStatistics statistics = new ImageStatistics(bmp);
                        R = ConvertToPointCollection(statistics.Red.Values);
                        G = ConvertToPointCollection(statistics.Green.Values);
                        B = ConvertToPointCollection(statistics.Blue.Values);
                    }
                }
                else
                {
                    L = null;
                    R = null;
                    G = null;
                    B = null;
                }
            }
            catch (Exception) { }
        }

        public void DrawLevelGraphBitmap()
        {
            try
            {
                if (ServiceProvider.DeviceManager.SelectedCameraDevice == null) { return; }
                if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy) { return; }
                if (ImageBitmap == null) { return; }


                //lock (_Sliderlockobj)
                //    Monitor.PulseAll(_Sliderlockobj);
                //Task.Run(() =>
                //{4
                //    lock (_Sliderlockobj)
                //        if (!Monitor.Wait(_Sliderlockobj, 30))
                //        {
                //ImageStatisticsHSL hslStatistics = new ImageStatisticsHSL(ImageBitmap);
                //L = ConvertToPointCollection(hslStatistics.Luminance.Values);
                //ImageStatistics statistics = new ImageStatistics(ImageBitmap);
                //R = ConvertToPointCollection(statistics.Red.Values);
                //G = ConvertToPointCollection(statistics.Green.Values);
                //B = ConvertToPointCollection(statistics.Blue.Values);
                //}
                //});

            }
            catch (Exception) { }
        }

        private PointCollection ConvertToPointCollection(int[] values)
        {
            int max = values.Max();

            PointCollection points = new PointCollection();
            points.Add(new Point(0, max));
            for (int i = 0; i < values.Length; i++)
            {
                points.Add(new Point(i, max - values[i]));
            }
            points.Add(new Point(values.Length - 1, max));
            points.Freeze();
            return points;
        }
    }
}
