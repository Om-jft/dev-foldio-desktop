using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DSLR_Tool_PC.ViewModels
{
    public class ImageDetails
    {
        /// <summary>
        /// A name for the image, not the file name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description for the image.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Full path such as c:\path\to\image.png
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The image file name such as image.png
        /// </summary>
        public string FileName { get; set; }

        
        /// <summary>
        /// The file name extension: bmp, gif, jpg, png, tiff, etc...
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// The image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The image width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The file size of the image.
        /// </summary>
        public long Size { get; set; }

        public string DateModified { get; set; }

        private bool _isZipSelected=true;
        public bool IsZIPSelected { get { return _isZipSelected; } set { _isZipSelected = value; } }

        public bool IsSelected { get; set; }

        public string Path_Orginal { get; set; }

        public DateTime CreationDateTime { get; set; }

        public string TimeModified { get; set; }
        public Bitmap Frame { get; set; }
        public Bitmap OGFrame { get; set; }
        public int rotateAngle { get; set; }
        public bool croppedImage { get; set; }
        public int crop_X { get; set; }
        public int crop_Y { get; set; }
        public int crop_W { get; set; }
        public int crop_H { get; set; }
        public int resizeW { get; set; }
        public int resizeH { get; set; }
        public string folderName { get; set; }
        public bool IsEdited { get; set; }
        
        public int BackgroundFilter { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }
        public int Saturation { get; set; }
        public int WhiteBalance { get; set; }
        public int WhiteClipping { get; set; }
        public ImageDetails(ImageDetails image)
        {
            this.CreationDateTime = image.CreationDateTime;
            this.croppedImage = image.croppedImage;
            this.DateModified = image.DateModified;
            this.Description = image.Description;
            this.Extension = image.Extension;
            this.FileName = image.FileName;
            //this.Frame = image.Frame;
            this.Height = image.Height;
            this.IsSelected = image.IsSelected;
            this.IsZIPSelected = image.IsZIPSelected;
            this.Name = image.Name;
            //this.OGFrame = image.OGFrame;
            this.Path = image.Path;
            this.Path_Orginal = image.Path_Orginal;
            this.rotateAngle = image.rotateAngle;
            this.Size = image.Size;
            this.TimeModified = image.TimeModified;
            this.Width = image.Width;
            this.crop_H = image.crop_H;
            this.crop_W = image.crop_W;
            this.crop_X = image.crop_X;
            this.crop_Y = image.crop_Y;
            this.resizeH = image.resizeH;
            this.resizeW = image.resizeW;
            this.folderName = image.folderName;
            this.IsEdited = image.IsEdited;
            this.BackgroundFilter = image.BackgroundFilter;
            this.Brightness = image.Brightness;
            this.Contrast = image.Contrast;
            this.Saturation = image.Saturation;
            this.WhiteClipping = image.WhiteClipping;
            this.WhiteBalance = image.WhiteBalance;
        }
        public ImageDetails() { }
    }
}
