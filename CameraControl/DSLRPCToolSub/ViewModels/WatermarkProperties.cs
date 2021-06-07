using CameraControl.Core.Classes;
using CameraControl.Devices;
using DSLR_Tool_PC;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using static DSLR_Tool_PC.ViewModels.Watermark;

namespace CameraControl.DSLRPCToolSub.ViewModels
{
    public class WatermarkProperties
    {
        DSLR_Tool_PC.ViewModels.Watermark waterMarkProperties = GetInstance();

        public static string watermarkTempDirectory()
        {
            var watermarkTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            Directory.CreateDirectory(watermarkTempDir);

            return watermarkTempDir.ToString();

        }


        public static string ApplyWatermark(string sourcePath)
        {
            DSLR_Tool_PC.ViewModels.Watermark waterMarkProperties = GetInstance();

            Bitmap wImage = new Bitmap(waterMarkProperties.ImagePath);
            Bitmap watermarkImage = new Bitmap(wImage, waterMarkProperties.ImageWidth, waterMarkProperties.ImageHeight);
            watermarkImage = SetOpacity(watermarkImage, (float)waterMarkProperties.ImageOpacity1);
            Bitmap image = new Bitmap(sourcePath);
            image = WatermarkImage(image, watermarkImage, waterMarkProperties.LocationX, waterMarkProperties.LocationY);
            string tempfolder = Path.Combine(Settings.ApplicationTempFolder, "og_" + Path.GetRandomFileName());
            if (!Directory.Exists(tempfolder))
                Directory.CreateDirectory(tempfolder);
            string fname = Path.Combine(tempfolder, Path.GetFileName(sourcePath));
            StaticClass.saveBitmap2File(image, fname);
            wImage.Dispose();
            watermarkImage.Dispose();
            image.Dispose();
            return fname;

        }

        public static Bitmap WatermarkImage(Bitmap image, Bitmap watermark, int x, int y)
        {
            try
            {
                using (Graphics imageGraphics = Graphics.FromImage(image))
                {
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
            // Make the new bitmap.
            
                Bitmap output_bm = new Bitmap(input_bm.Width, input_bm.Height);
                // Make an associated Graphics object.
                using (Graphics gr = Graphics.FromImage(output_bm))
                {
                    // Make a ColorMatrix with the opacity.
                    ColorMatrix color_matrix = new ColorMatrix();
                    color_matrix.Matrix33 = opacity;

                    // Make the ImageAttributes object.
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(color_matrix,
                        ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    // Draw the input bitmap onto the Graphics object.
                    Rectangle rect = new Rectangle(0, 0,
                        output_bm.Width, output_bm.Height);

                    gr.DrawImage(input_bm, rect,
                        0, 0, input_bm.Width, input_bm.Height,
                        GraphicsUnit.Pixel, attributes);
                }
                return output_bm;

            
        }
    }
}