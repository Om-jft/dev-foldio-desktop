using DSLR_Tool_PC.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CameraControl.DSLRPCToolSub.UndoRedo
{
    public class Memento
    {
        private int _imageIndex;
        public int ImageIndex
        {
            get { return _imageIndex; }
            set { _imageIndex = value; }
        }
        public ImageDetails IDetails { get; set; }

        public Memento(int imageIndex,ImageDetails imageDetailsObject)
        {
            this._imageIndex = imageIndex;
            this.IDetails = imageDetailsObject;
        }
    }
}
