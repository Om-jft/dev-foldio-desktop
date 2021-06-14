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

        private int _ImageIndex;

        private ImageDetails _imageDetails;

        public MementoOrganizer(int index, ImageDetails imageDetails)
        {
            _ImageIndex = index;
            _imageDetails = imageDetails;
        }

        public Memento getMemento()
        {
                return new Memento(_ImageIndex,_imageDetails);
        }

        public void setMemento(Memento memento)
        {
            _ImageIndex = memento.ImageIndex;
            _imageDetails = memento.IDetails;
        }
    }
}
