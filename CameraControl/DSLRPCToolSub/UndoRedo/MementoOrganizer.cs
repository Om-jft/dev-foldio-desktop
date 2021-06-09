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
    public class MementoOrganizer
    {
        //private WriteableBitmap _Image;
        private int _ImageIndex;
        private Bitmap _bitmapImage;

        //public MementoOrganizer(WriteableBitmap image,int index,Bitmap bitmap)
        public MementoOrganizer(int index, Bitmap bitmap)
        {
            //_Image=image;
            _ImageIndex = index;
            if (bitmap != null) { _bitmapImage = (Bitmap)bitmap.Clone(); }
        }

        public Memento getMemento()
        {
            if (_bitmapImage == null) { return null; }
            else
            {
                return new Memento(_ImageIndex,_bitmapImage);
            }
        }

        public void setMemento(Memento memento)
        {
            //_Image = memento.Image;
            _bitmapImage = (Bitmap)memento.ImageBitmap.Clone();
            _ImageIndex = memento.ImageIndex;
        }
    }
}
