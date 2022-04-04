using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{
    public abstract class Dithering: IEffect
    {
        public abstract WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }

    public class RandomDithering: Dithering
    {
        public int K { get; set; }

        public RandomDithering(int k)
        {
            this.K = k;
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;
            var rng = new Random();
            var intervalLength = 255f / (K - 1);

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);
                        var randomThreshold = (int)Math.Round(rng.NextDouble() * intervalLength);
                        var posR = (int)Math.Truncate(1f * oldColor.R / intervalLength) + 1;
                        var posG = (int)Math.Truncate(1f * oldColor.G / intervalLength) + 1;
                        var posB = (int)Math.Truncate(1f * oldColor.B / intervalLength) + 1;
                        

                        if (oldColor.R == oldColor.B && oldColor.B == oldColor.G) // grey pixel
                        {
                            int greyIntensity;

                            if (oldColor.R > randomThreshold)
                            {
                                greyIntensity = posR == K ? 255 : Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255);
                            }
                            else
                            {
                                greyIntensity = posR == 1 ? 0 : Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255);
                            }

                            var newColor = Color.FromArgb(oldColor.A,
                                greyIntensity,
                                greyIntensity,
                                greyIntensity);

                            clone.SetPixelColor(x, y, newColor);
                        }
                        else
                        {
                            int newR;
                            int newG;
                            int newB;
                            
                            newR = oldColor.R > randomThreshold ? 
                                newR = posR == K ? 
                                    255 : 
                                    Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255) : 
                                newR = posR == 1 ? 
                                    0 : 
                                    Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255);
                            
                            newG = oldColor.G > randomThreshold ? 
                                newG = posG == K ? 
                                    255 : 
                                    Math.Clamp((int)Math.Round(posG * intervalLength), 0, 255) : 
                                newG = posG == 1 ? 
                                    0 : 
                                    Math.Clamp((int)Math.Round(posG * intervalLength), 0, 255);
                            
                            newB = oldColor.B > randomThreshold ? 
                                newB = posB == K ? 
                                    255 : 
                                    Math.Clamp((int)Math.Round(posB * intervalLength), 0, 255) : 
                                newB = posB == 1 ? 
                                    0 : 
                                    Math.Clamp((int)Math.Round(posB * intervalLength), 0, 255);

                            var newColor = Color.FromArgb(oldColor.A,
                                newR,
                                newG,
                                newB);

                            clone.SetPixelColor(x, y, newColor);
                        }
                    }
                }
            }
            finally
            {
                wbm.Unlock();
                clone.Unlock();
            }

            return clone;
        }

        public override string ToString()
        {
            return "Random Dithering";
        }
    }
}