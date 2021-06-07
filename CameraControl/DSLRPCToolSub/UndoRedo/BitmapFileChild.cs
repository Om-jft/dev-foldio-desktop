using CameraControl.Core.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.DSLRPCToolSub.UndoRedo
{
    public class BitmapFileChild:BitmapFile
    {
        private UndoRedo _UnDoObject;
        public UndoRedo UnDoObject
        {
            get { return _UnDoObject; }
            set
            {
                _UnDoObject = value;

            }
        }
    }
}
