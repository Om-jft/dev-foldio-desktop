
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSLR_Tool_PC.ViewModels
{
    public class PathUpdate : BaseFieldClass
    {
        private static PathUpdate uni_class_inst = null;
        public static PathUpdate getInstance()
        {
            if (uni_class_inst == null)
                uni_class_inst = new PathUpdate();

            return uni_class_inst;
        }

        public ImageDetails __SelectedImageDetails;

        private string _pathimg;
        public string PathImg
        {
            get { return _pathimg; }
            set
            {
                if (_pathimg != value)
                {
                    _pathimg = value;
                    NotifyPropertyChanged("PathImg");
                }
                EditLevelGraphVM.ImagePath = _pathimg;//.Substring(8);
            }
        }

        private byte[] _editImageByte;
        public byte[] EditImageByte
        {
            get { return _editImageByte; }
            set
            {
                if (_editImageByte != value)
                {
                    _editImageByte = value;
                    NotifyPropertyChanged("EditImageByte");
                }

            }
        }

        private LevelGraphViewModel _EditLevelGraphVM = new LevelGraphViewModel();
        public LevelGraphViewModel EditLevelGraphVM
        {
            get { return _EditLevelGraphVM; }
            set { _EditLevelGraphVM = value; }
        }


        private string _pathCaptureimg;
        public string PathCaptureImg
        {
            get { return _pathCaptureimg; }
            set
            {
                if (_pathCaptureimg != value)
                {
                    _pathCaptureimg = value;
                }
                CaptureLevelGraphVM.ImagePath = _pathCaptureimg;//.Substring(8);
            }
        }

        private LevelGraphViewModel _captureLevelGraphVM = new LevelGraphViewModel();
        public LevelGraphViewModel CaptureLevelGraphVM
        {
            get { return _captureLevelGraphVM; }
            set { _captureLevelGraphVM = value; }
        }

    }
}
