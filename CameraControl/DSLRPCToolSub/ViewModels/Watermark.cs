using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DSLR_Tool_PC.ViewModels
{
    public class Watermark : BaseFieldClass
    {
        private static Watermark uni_class_inst = null;
        public static Watermark GetInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new Watermark();
            return uni_class_inst;
        }

        string _ImageName = "";
        public string ImageName
        {
            get { return _ImageName; }
            set
            {
                _ImageName = value;
                NotifyPropertyChanged("ImageName");
            }
        }

        string _ImagePath = "";
        public string ImagePath
        {
            get { return _ImagePath; }
            set
            {
                _ImagePath = value;
                NotifyPropertyChanged("ImagePath");
                if (value != "") { _IsVisible = Visibility.Visible; } else { _IsVisible = Visibility.Collapsed; }
                NotifyPropertyChanged("IsVisible");
            }
        }

        int _ImageHeight = 0;
        public int ImageHeight
        {
            get { return _ImageHeight; }
            set
            {
                _ImageHeight = value;
                NotifyPropertyChanged("ImageHeight");
            }
        }

        int _ImageWidth = 0;
        public int ImageWidth
        {
            get { return _ImageWidth; }
            set
            {
                _ImageWidth = value;
                NotifyPropertyChanged("ImageWidth");
            }
        }

        int _LocationX = 0;
        public int LocationX
        {
            get { return _LocationX; }
            set
            {
                _LocationX = value;
                NotifyPropertyChanged("LocationX");
            }
        }

        int _LocationY = 0;
        public int LocationY
        {
            get { return _LocationY; }
            set
            {
                _LocationY = value;
                NotifyPropertyChanged("LocationY");
            }
        }

        decimal _ImageOpacity1 = 1.00M;
        public decimal ImageOpacity1
        {
            get { return _ImageOpacity1; }
            set
            {
                _ImageOpacity1 = value;
                NotifyPropertyChanged("ImageOpacity1");
            }
        }

        int _ImageOpacity100 = 100;
        public int ImageOpacity100
        {
            get { return _ImageOpacity100; }
            set
            {
                _ImageOpacity100 = value;
                NotifyPropertyChanged("ImageOpacity100");
                ImageOpacity1 = value / 100.00M;
            }
        }

        Visibility _IsVisible = Visibility.Collapsed;
        public Visibility IsVisible
        {
            get { return _IsVisible; }
        }

        public void Reset()
        {
            ImageName = "";
            ImagePath = "";
            ImageHeight = 0;
            ImageWidth = 0;
            LocationX = 0;
            LocationY = 0;
            ImageOpacity100 = 100;
        }
    }
}
