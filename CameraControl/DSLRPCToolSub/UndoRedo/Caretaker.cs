using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraControl.DSLRPCToolSub.UndoRedo
{
    public class Caretaker
    {
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
        }
        private static Caretaker _Caretaker_inst = null;
        public static Caretaker GetInstance()
        {
            if (_Caretaker_inst == null)
            {
                _Caretaker_inst = new Caretaker();

            }
            return _Caretaker_inst;
        }
        private Stack<Memento> UndoStack = new Stack<Memento>();
        private Stack<Memento> RedoStack = new Stack<Memento>();

        public Memento getUndoMemento()
        {
            if (UndoStack.Count >= 2)
            {
                RedoStack.Push(UndoStack.Pop());
                return UndoStack.Peek();
            }
            else
                return null;
        }
        public Memento getRedoMemento()
        {
            if (RedoStack.Count != 0)
            {
                Memento m = RedoStack.Pop();
                UndoStack.Push(m);
                return m;
            }
            else
                return null;
        }
        public void InsertMementoForUndoRedo(Memento memento)
        {
            if (memento != null)
            {
                UndoStack.Push(memento);
                RedoStack.Clear();
            }
        }
        public bool IsUndoPossible()
        {
            if (UndoStack.Count >= 2)
            {
                return true;
            }
            else
                return false;

        }
        public bool IsRedoPossible()
        {
            if (RedoStack.Count != 0)
            {
                return true;
            }
            else
                return false;
        }
    }
}
