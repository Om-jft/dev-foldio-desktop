﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    }
}