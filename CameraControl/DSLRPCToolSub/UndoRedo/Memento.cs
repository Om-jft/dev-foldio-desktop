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
        //private WriteableBitmap _image;
        //public WriteableBitmap Image
        //{
        //    get { return _image; }
        //}
        private int _imageIndex;
        public int ImageIndex
        {
            get { return _imageIndex; }
        }
        private Bitmap _imageBitmap;
        public Bitmap ImageBitmap
        {
            get { return new Bitmap(_imageBitmap); }
        }
        public Memento(int imageIndex,Bitmap bitmapImage)
        {
            //this._image = image;
            this._imageIndex = imageIndex;
            this._imageBitmap = new Bitmap(bitmapImage);
        }
    }
}
