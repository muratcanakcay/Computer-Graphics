using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{public interface IEffect
    {
        WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }
}