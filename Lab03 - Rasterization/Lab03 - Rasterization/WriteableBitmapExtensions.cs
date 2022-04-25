using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Lab03___Rasterization
{
    public static class WritableBitmapExtensions
    {
        public static void SetPixelColor(this WriteableBitmap wbm, int x, int y, Color color)
        {
            if (y < 0 || x < 0 || y > wbm.PixelHeight - 1 || x > wbm.PixelWidth - 1)
                return;
                //throw new Exception("SetPixelColor target out of bitmap bounds");
            
            IntPtr pBackBuffer = wbm.BackBuffer;
            int stride = wbm.BackBufferStride;

            unsafe
            {
                var pBuffer = (byte*)pBackBuffer.ToPointer();
                int index = y * stride + x * 4;

                pBuffer[index + 0] = color.B;
                pBuffer[index + 1] = color.G;
                pBuffer[index + 2] = color.R;
                pBuffer[index + 3] = color.A;
            }

            wbm.AddDirtyRect(new Int32Rect(x, y, 1, 1));
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
                                             pBuffer[index + 0]);
            }

            return pixelColor;
        }

        public static void ApplyBrush(this WriteableBitmap wbm, int x, int y, uint thickness, Color color)
        {
            for (var i = (int)-thickness+1; i < thickness; i++)
                for (var j = (int)-thickness + 1; j < thickness; j++)
                {
                    if (Math.Abs(i) + Math.Abs(j) <= thickness)
                        wbm.SetPixelColor(x+i, y+j, color);
                }
        }
        public static void Clear(this WriteableBitmap wbm)
        {
            try
            {
                wbm.Lock();
                for (int x = 0; x < wbm.Width; x++)
                    for (int y = 0; y < wbm.Height; y++)
                        wbm.SetPixelColor(x, y, Color.FromArgb(255, 255, 255, 255));
            }
            finally
            {
                wbm.Unlock();
            }
        }

        public static WriteableBitmap DownSample(this WriteableBitmap wbm, int scale) // TODO: allow for more than 2x multipsampling
        {
            var downSampledWbm = new WriteableBitmap((int)wbm.PixelWidth / scale,
                                                    (int)wbm.PixelHeight / scale, 
                                                    96, 
                                                    96, 
                                                    PixelFormats.Bgr32, 
                                                    null);

            downSampledWbm.Clear();

            try
            {
                wbm.Lock();
                downSampledWbm.Lock();
                
                for (int x = 0; x < downSampledWbm.Width; x++)
                    for (int y = 0; y < downSampledWbm.Height; y++)
                    {
                        var col1 = wbm.GetPixelColor(2*x+0, 2*y+0);
                        var col2 = wbm.GetPixelColor(2*x+1, 2*y+0);
                        var col3 = wbm.GetPixelColor(2*x+0, 2*y+1);
                        var col4 = wbm.GetPixelColor(2*x+1, 2*y+1);

                        var avgR = (col1.R + col2.R + col3.R + col4.R) / 4;
                        var avgG = (col1.R + col2.R + col3.R + col4.R) / 4;
                        var avgB = (col1.R + col2.R + col3.R + col4.R) / 4;

                        downSampledWbm.SetPixelColor(x, y, Color.FromArgb(255, avgR, avgG, avgB));
                    }
            }
            finally
            {
                wbm.Unlock();
                downSampledWbm.Unlock();
            }

            return downSampledWbm;
        }
    }
}
