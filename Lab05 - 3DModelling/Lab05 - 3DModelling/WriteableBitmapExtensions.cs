using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using Color = System.Drawing.Color;
using Color = System.Windows.Media.Color;

namespace Lab05___3DModelling;

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
            int index = stride*y + 4*x;

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
            return Colors.Transparent;

        IntPtr pBackBuffer = wbm.BackBuffer;
        int stride = wbm.BackBufferStride;

        Color pixelColor;

        unsafe
        {
            var pBuffer = (byte*)pBackBuffer.ToPointer();
            int index = stride*y + 4*x;

            pixelColor = Color.FromArgb(pBuffer[index + 3], 
                                          pBuffer[index + 2], 
                                        pBuffer[index + 1], 
                                         pBuffer[index + 0]);
        }

        return pixelColor;
    }

    public static void SetPixels(this WriteableBitmap wbm, List<Pixel> meshPoints)
    {
        try
        {
            wbm.Lock();
            foreach (Pixel p in meshPoints)
            {
                int column = p.X;
                int row = p.Y;
                if (row >= 0 && column >= 0 && row < (int)wbm.Height - 1 && column < (int)wbm.Width - 1)
                {
                    unsafe
                    {
                        IntPtr pBackBuffer = wbm.BackBuffer;
                        pBackBuffer += row * wbm.BackBufferStride;
                        pBackBuffer += column * 4;
                        int color_data = 0;
                        color_data |= p.Color.A << 24;    // A
                        color_data |= p.Color.R << 16;    // R
                        color_data |= p.Color.G << 8;     // G
                        color_data |= p.Color.B << 0;     // B
                        *((int*)pBackBuffer) = color_data;
                    }

                    wbm.AddDirtyRect(new Int32Rect(column, row, 1, 1));
                }
            }
        }
        finally
        {
            wbm.Unlock();
        }
    }
}