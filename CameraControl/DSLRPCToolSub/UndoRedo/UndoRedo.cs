using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using DSLR_Tool_PC.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CameraControl.DSLRPCToolSub.UndoRedo
{
    public class UndoRedo : IUndoRedo
    {
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
        }

        public Caretaker _Caretaker = new Caretaker();
        MementoOrganizer _MementoOriginator = null;

        public UndoRedo(int index, ImageDetails imageDetails)
        {
            _MementoOriginator = new MementoOrganizer(index,imageDetails);
        }        
        public void Undo(int level)
        {
            Memento memento = null;
            try
            {
                for (int i = 1; i <= level; i++)
                {
                    memento = _Caretaker.getUndoMemento();
                    __mainWindowAdvanced.ListBoxSnapshots.SelectedItem = __mainWindowAdvanced.ListBoxSnapshots.Items.GetItemAt(memento.ImageIndex);
                    __mainWindowAdvanced.__photoEditModel.applyStateFilter(memento.ImageIndex,memento.IDetails);
                    __mainWindowAdvanced.__photoEditModel.ResetAllControls();
                }
                if (memento != null)
                {
                    _MementoOriginator.setMemento(memento);
                }
            }catch(Exception ex) { Log.Debug("Undo Redo Exception: ", ex); }
        }

        public void Redo(int level)
        {
            try
            {
                Memento memento = null;
                for (int i = 1; i <= level; i++)
                {
                    memento = _Caretaker.getRedoMemento();
                    __mainWindowAdvanced.ListBoxSnapshots.SelectedItem = __mainWindowAdvanced.ListBoxSnapshots.Items.GetItemAt(memento.ImageIndex);
                    __mainWindowAdvanced.__photoEditModel.applyStateFilter(memento.ImageIndex, memento.IDetails);
                    __mainWindowAdvanced.__photoEditModel.ResetAllControls();
                }
                if (memento != null)
                {
                    _MementoOriginator.setMemento(memento);

                }
            }
            catch(Exception ex) { Log.Debug("Undo Redo Exception: ",ex); }
        }

        public void SetStateForUndoRedo(Memento memento)
        {
            try
            {
                _Caretaker.InsertMementoForUndoRedo(memento);
            }
            catch(Exception ex) { Log.Debug("Undo Redo: Exception: ", ex); }
        }

        public bool IsUndoPossible()
        {
            return _Caretaker.IsUndoPossible();

        }
        public bool IsRedoPossible()
        {
            return _Caretaker.IsRedoPossible();
        }
    }
}
