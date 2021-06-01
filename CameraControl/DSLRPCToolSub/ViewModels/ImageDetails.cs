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

        public bool IsZIPSelected { get; set; }

        public bool IsSelected { get; set; }

        public string Path_Orginal { get; set; }

        public DateTime CreationDateTime { get; set; }

        public string TimeModified { get; set; }
        public Bitmap Frame { get; set; }
        public Bitmap OGFrame { get; set; }
        public int rotateAngle { get; set; }
        public bool croppedImage { get; set; }

        public ImageDetails(ImageDetails image)
        {
            this.CreationDateTime = image.CreationDateTime;
            this.croppedImage = image.croppedImage;
            this.DateModified = image.DateModified;
            this.Description = image.Description;
            this.Extension = image.Extension;
            this.FileName = image.FileName;
            this.Frame = image.Frame;
            this.Height = image.Height;
            this.IsSelected = image.IsSelected;
            this.IsZIPSelected = image.IsZIPSelected;
            this.Name = image.Name;
            this.OGFrame = image.OGFrame;
            this.Path = image.Path;
            this.Path_Orginal = image.Path_Orginal;
            this.rotateAngle = image.rotateAngle;
            this.Size = image.Size;
            this.TimeModified = image.TimeModified;
            this.Width = image.Width;
        }
        public ImageDetails() { }
    }
}
