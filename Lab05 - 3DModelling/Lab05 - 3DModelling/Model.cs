using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Lab05___3DModelling;

public interface IMeshable
{
    public int M { get; set; }
    public int N { get; set; }
    public int Radius { get; set; }
    public int Height { get; set; }
    public List<Point3d> Vertices { get; set; }  
    void ClearVertices();
    void Draw(WriteableBitmap wbm, WriteableBitmap texture);
}

public abstract class Model : IMeshable
{
    public int M { get; set; }
    public int N { get; set; }
    public int Radius { get; set; }
    public int Height { get; set; }
    public List<Point3d> Vertices { get; set; } = new();

    public void ClearVertices()
    {
        Vertices.Clear();
    }
    public abstract void Draw(WriteableBitmap wbm, WriteableBitmap texture);
}