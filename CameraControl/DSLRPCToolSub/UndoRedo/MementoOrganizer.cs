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
        private WriteableBitmap _Image;
        private int _ImageIndex;
        private Bitmap _bitmapImage;
        
        public MementoOrganizer(WriteableBitmap image,int index,Bitmap bitmap)
        {
            _Image=image;
            _ImageIndex = index;
            if (bitmap != null) { _bitmapImage = new Bitmap(bitmap); }
        }

        public Memento getMemento()
        {
            WriteableBitmap imageInEditorCanvas = null;
            if (_Image == null) { return null; }
            else
            {
                imageInEditorCanvas = _Image.Clone();
                return new Memento(imageInEditorCanvas,_ImageIndex,_bitmapImage);
            }
        }

        public void setMemento(Memento memento)
        {
            _Image = memento.Image;
            _bitmapImage = new Bitmap(memento.ImageBitmap);
            _ImageIndex = memento.ImageIndex;
        }
    }
}
