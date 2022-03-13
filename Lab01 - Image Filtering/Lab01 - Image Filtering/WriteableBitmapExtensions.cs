using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Lab01___Image_Filtering
{
    public static class WritableBitmapExtensions
    {
        public static void SetPixelColor(this WriteableBitmap wbm, int x, int y, Color color)
        {
            if (y < 0 || x < 0 || y > wbm.PixelHeight - 1 || x > wbm.PixelWidth - 1)
                throw new Exception("SetPixelColor target out of bitmap bounds");

            //TODO: do I need this lock? if so, then I should disable the ui while locked
            //wbm.Lock();
            
            IntPtr pBackBuffer = wbm.BackBuffer;
            int stride = wbm.BackBufferStride;

            unsafe
            {
                var pBuffer = (byte*)pBackBuffer.ToPointer();
                int index = y * stride + x * 4;

                pBuffer[index] = color.B;
                pBuffer[index + 1] = color.G;
                pBuffer[index + 2] = color.R;
                pBuffer[index + 3] = color.A;

            }

            wbm.AddDirtyRect(new Int32Rect(x, y, 1, 1));

            //TODO: unlock if locked!
            //wbm.Unlock();
        }
        public static Color GetPixelColor(this WriteableBitmap wbm, int x, int y)
        {
            if (y < 0 || x < 0||y > wbm.PixelHeight - 1 || x > wbm.PixelWidth - 1)
                return Color.Empty;

            IntPtr pBackBuffer = wbm.BackBuffer;
            int stride = wbm.BackBufferStride;

            Color pixelColor;

            unsafe
            {
                var pBuffer = (byte*)pBackBuffer.ToPointer();
                int index = y * stride + x * 4;

                pixelColor = Color.FromArgb(pBuffer[index + 3], 
                                            pBuffer[index + 2], 
                                            pBuffer[index + 1], 
                                            pBuffer[index]);
            }

            return pixelColor;
        }
    }
}
