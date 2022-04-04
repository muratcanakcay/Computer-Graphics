using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{
    public abstract class ColorQuantization : IEffect
    {
        public abstract WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }

    public class KMeans: ColorQuantization
    {
        public int K { get; set; }

        public KMeans(int k)  
        {
            this.K = k;
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);
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
            return "K-Means";
        }
    }
}
