using System.Windows.Media.Imaging;

namespace Lab01___Image_Filtering
{
    public interface IFilter
    {
        WriteableBitmap Apply(WriteableBitmap inputImage);
    }

    public abstract class FunctionFilter : IFilter
    {
        public (int, int)[]? _filterFunction;

        protected FunctionFilter((int, int)[]? filterFunction)
        {
            _filterFunction = filterFunction;
        }
        
        public abstract WriteableBitmap Apply(WriteableBitmap inputImage);
    }

    public class Inversion : FunctionFilter
    {
        public Inversion() : base(new[] {(0,256), (256,0)})
        {
        }
        
        public override WriteableBitmap Apply(WriteableBitmap inputImage)
        {
            return null;
        }

        
    }
}