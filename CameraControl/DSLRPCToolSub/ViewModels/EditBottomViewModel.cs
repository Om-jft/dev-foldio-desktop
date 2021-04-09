using DSLR_Tool_PC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSLR_Tool_PC.ViewModels
{
    public class EditBottomViewModel : DSLR_Tool_PC.ViewModels.BaseFieldClass
    {
        private static EditBottomViewModel _editBottomViewModel = null;
        public static EditBottomViewModel eb_Instance()
        {
            if (_editBottomViewModel == null) { _editBottomViewModel = new EditBottomViewModel(); }
            return _editBottomViewModel;
        }

        public List<ImageDetails> imageFiles = new List<ImageDetails>();
        public List<ImageDetails> SelectedFolderFiles
        {
            get { return imageFiles; }
            set
            {
                imageFiles = value;
                NotifyPropertyChanged("SelectedFolderFiles");
            }
        }
    }
}
