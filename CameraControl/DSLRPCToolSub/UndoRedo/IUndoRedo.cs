using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.DSLRPCToolSub.UndoRedo
{
    public interface IUndoRedo
    {
        void Undo(int level);
        void Redo(int level);
        void SetStateForUndoRedo(Memento memento);

    }
}
