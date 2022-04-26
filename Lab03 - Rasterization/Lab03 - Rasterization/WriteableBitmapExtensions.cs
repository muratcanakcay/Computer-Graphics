﻿using System;
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

        public static void ApplyBrush(this WriteableBitmap wbm, int x, int y, int thickness, Color color)
        { // TODO: revise the brush/thickness implementation here. let thickness=2 be a brush with 2 pixel width. Take the thickness from GUI and use it as Thickness = thicknessFromGUI*2 - 1 in shape
            for (var i = 1-thickness; i < thickness; i++)
                for (var j = 1-thickness; j < thickness; j++)
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
                for (var x = 0; x < wbm.Width; x++)
                    for (var y = 0; y < wbm.Height; y++)
                        wbm.SetPixelColor(x, y, Color.FromArgb(255, 255, 255, 255));
            }
            finally
            {
                wbm.Unlock();
            }
        }

        public static WriteableBitmap DownSample(this WriteableBitmap wbm, int SSAA)
        {
            var downSampledWbm = new WriteableBitmap((int)wbm.PixelWidth / SSAA,
                                                    (int)wbm.PixelHeight / SSAA, 
                                                    96, 
                                                    96, 
                                                    PixelFormats.Bgr32, 
                                                    null);

            downSampledWbm.Clear();

            try
            {
                wbm.Lock();
                downSampledWbm.Lock();

                for (var x = 0; x < downSampledWbm.Width; x++)
                {
                    for (var y = 0; y < downSampledWbm.Height; y++)
                    {
                        var sumR = 0;
                        var sumG = 0;
                        var sumB = 0;

                        for (var i = 0; i < SSAA; i++)
                        {
                            for (var j = 0; j < SSAA; j++)
                            {
                                var color = wbm.GetPixelColor(SSAA * x + i, SSAA * y + j);
                                sumR += color.R;
                                sumG += color.G;
                                sumB += color.B;
                            }
                        }

                        var avgR = sumR / (SSAA * SSAA);
                        var avgG = sumG / (SSAA * SSAA);
                        var avgB = sumB / (SSAA * SSAA);

                        downSampledWbm.SetPixelColor(x, y, Color.FromArgb(255, avgR, avgG, avgB));
                    }
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
