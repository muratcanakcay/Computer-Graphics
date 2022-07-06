using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Lab05___3DModelling
{ public interface IMeshable
    {
        public int M { get; set; }
        public int N { get; set; }
        public int Radius { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public List<Point3d> Vertices { get; set; }  
        void ClearVertices();
        void Draw(WriteableBitmap wbm, WriteableBitmap? texture, Phong lightAttributes);
        void CalculateVertices();
    }
    public abstract class Model : IMeshable
    {
        public int M { get; set; }
        public int N { get; set; }
        public int Radius { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public List<Point3d> Vertices { get; set; } = new();
        
        public void ClearVertices()
        {
            Vertices.Clear();
        }
        
        public abstract void CalculateVertices();
        
        protected abstract List<Triangle> CalculateTriangles();
        
        public void Draw(WriteableBitmap wbm, WriteableBitmap? texture, Phong lightAttributes)
        {
            var drawingData = new List<Pixel>();
            var triangles = CalculateTriangles();

            foreach (var triangle in triangles)
                triangle.Fill(drawingData, texture, lightAttributes);

            if (lightAttributes.DrawMesh)
            {
                foreach (var triangle in triangles)
                    triangle.Draw(drawingData);
            }

            wbm.SetPixelColors(drawingData);
            drawingData.Clear();
            triangles.Clear();
        }
    }
}