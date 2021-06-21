using CameraControl.Core.Classes;
using CameraControl.Devices;
using DSLR_Tool_PC;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
//using static ;

namespace CameraControl.DSLRPCToolSub.ViewModels
{
    public partial class WatermarkProperties
    {
        private double x_Cordinate;
        private double y_Cordinate;
        public int x_location;
        public int y_location;
        DSLR_Tool_PC.ViewModels.Watermark waterMark = DSLR_Tool_PC.ViewModels.Watermark.GetInstance();
        private static WatermarkProperties _WatermarkProperties_inst = null;
        public static WatermarkProperties getInstance()
        {
            if (_WatermarkProperties_inst == null)
            {
                _WatermarkProperties_inst = new WatermarkProperties();

            }
            return _WatermarkProperties_inst;
        }
        MainWindowAdvanced __mainWindowAdvanced = null;
        public void ExecuteInti(object __this)
        {
            //__editLeftControl = (EditLeftControl)__this;
            __mainWindowAdvanced = (MainWindowAdvanced)__this;
            //photoEdit = (PhotoEdit)__this;
        }
        //
        //  Summary:
        //      Applies watermark on the filtered frame.
        //
        //  Parameters:
        //      Sourcefile path to apply watermark.
        //
        public static void ApplyWatermark(string sourcePath)
        {
            DSLR_Tool_PC.ViewModels.Watermark waterMarkProperties = DSLR_Tool_PC.ViewModels.Watermark.GetInstance();
            Bitmap wImage = new Bitmap(waterMarkProperties.ImagePath);
            //Bitmap watermarkImage = new Bitmap(wImage, waterMarkProperties.ImageWidth, waterMarkProperties.ImageHeight);
            
            Bitmap image = new Bitmap(sourcePath);
            //Adjust size of watermark image//
            Bitmap watermarkImage = new Bitmap(WatermarkProperties.getInstance().WatermarkResolutionCorrection(wImage, image));

            //Opacity correcting of watermark
            if (waterMarkProperties.IsOpacityApply) { watermarkImage = SetOpacity(watermarkImage, (float)waterMarkProperties.ImageOpacity1); }

            //Correction for x and y co-ordinate of watermark on final image.
            WatermarkProperties.getInstance().XandYcorrection();

            //store watermark on the final image
            Bitmap Finalimage = new Bitmap(WatermarkProperties.getInstance().WatermarkImage(image,watermarkImage,WatermarkProperties.getInstance().x_location,WatermarkProperties.getInstance().y_location));

            image.Dispose();image = null;
            string dst = sourcePath;
            if (File.Exists(sourcePath)) { File.Delete(sourcePath); }
            StaticClass.saveBitmap2File(Finalimage, dst);
            wImage.Dispose();
            Finalimage.Dispose();
            watermarkImage.Dispose();
            GC.Collect();
            Thread.Sleep(10);
        }

        public Bitmap WatermarkImage(Bitmap image, Bitmap watermark, int x, int y)
        {
            //int hratio = 
            try
            {
                using (Graphics imageGraphics = Graphics.FromImage(image))
                {
                    //watermark=new Bitmap(ResizeBitmap(watermark,))
                    watermark.SetResolution(imageGraphics.DpiX, imageGraphics.DpiY);
                    imageGraphics.DrawImage(watermark, x, y, watermark.Width, watermark.Height);
                }
            }catch(Exception e)
            {
                Log.Debug("Watermark apply Exception: ", e);
                //MessageBox.Show(e.ToString());
            }
            return image;
        }
        private static Bitmap SetOpacity(Bitmap input_bm, float opacity)
        {               
                Bitmap output_bm = new Bitmap(input_bm.Width, input_bm.Height);
                // Make an associated Graphics object.
                using (Graphics gr = Graphics.FromImage(output_bm))
                {
                    // Make a ColorMatrix with the opacity.
                    ColorMatrix color_matrix = new ColorMatrix();
                    color_matrix.Matrix33 = opacity;

                    // Make the ImageAttributes object.
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(color_matrix,ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    // Draw the input bitmap onto the Graphics object.
                    Rectangle rect = new Rectangle(0, 0, output_bm.Width, output_bm.Height);

                    gr.DrawImage(input_bm, rect,0, 0, input_bm.Width, input_bm.Height,GraphicsUnit.Pixel, attributes);
                }
                return output_bm;
        }
        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            using (Bitmap result = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.DrawImage(bmp, 0, 0, width, height);
                }
                return (Bitmap)result.Clone();
            }
        }

        public Bitmap WatermarkResolutionCorrection(Bitmap image,Bitmap bitmap)
        {
            double factorheight = (double)bitmap.Height/(double)__mainWindowAdvanced.EditFramePicEdit.ActualHeight;
            double factorwidth = (double)bitmap.Width / (double)__mainWindowAdvanced.EditFramePicEdit.ActualWidth;
            x_Cordinate = factorwidth;
            y_Cordinate = factorheight;
            double width =(double) waterMark.ImageWidth * factorwidth;
            width = width > bitmap.Width ? bitmap.Width : width;
            double height = (double)waterMark.ImageHeight * factorheight;
            height = height > bitmap.Height ? bitmap.Height : height;
            using (image)
            {
                return new Bitmap(image, (int) width,(int)height);
            }
        }

        public void XandYcorrection()
        {
            DSLR_Tool_PC.ViewModels.Watermark waterMarkProperties = DSLR_Tool_PC.ViewModels.Watermark.GetInstance();
            double x = (__mainWindowAdvanced.EditPicGrid.ActualWidth - __mainWindowAdvanced.EditFramePicEdit.ActualWidth);
            x = (double)(x / 2);

            double y = (__mainWindowAdvanced.EditPicGrid.ActualHeight - __mainWindowAdvanced.EditFramePicEdit.ActualHeight);
            y = (double)(y / 2);
            y = waterMarkProperties.LocationX - y;
            x = waterMarkProperties.LocationY - x;
            x_location = (int)Math.Ceiling(x * x_Cordinate);
            y_location = (int)Math.Ceiling(y * y_Cordinate);
        }
    }
}