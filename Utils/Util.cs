using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;

namespace HilmerSVGTransform.Utils {
    public static class Util {
        public static Bitmap SetImageOpacity(Image Image, float Opacity) {
            try {
                Bitmap Bmp = new Bitmap(Image.Width, Image.Height);
                using (Graphics Gfx1 = Graphics.FromImage(Bmp)) {
                    ColorMatrix Matrix1 = new ColorMatrix();
                    Matrix1.Matrix33 = Opacity;
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(Matrix1, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    Gfx1.DrawImage(Image, new Rectangle(0, 0, Bmp.Width, Bmp.Height), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, attributes);
                }
                return Bmp;
            } catch {
                return null;
            }
        }
    }
}