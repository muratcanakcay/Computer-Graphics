using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Lab05___3DModelling;

public class Cuboid : Model
{
    public Cuboid(int w, int h, int d)
    {
        Width = w;
        Height = h;
        Depth = d;
        Vertices = new List<Point3d>() { }; 
    }

    public void CalculateVertices()
    {
        // Front Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, Depth, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(0, 0.5)
        });
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, 0, Depth, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, Depth, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(1/3d, 1d)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, Depth, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(0, 1d)
        });

        // Back Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, 0, 0, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(0, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, 0, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(1/3d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, 0, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, 0, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(0, 0.5)
        });
        


        // Right Face
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, 0, Depth, 1d),
            Normal = new Point4(1, 0, 0, 0),
            TextureMap = new Point(2/3d, 0.5)
        });
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, 0, 0, 1d),
            Normal = new Point4(1, 0, 0, 0),
            TextureMap = new Point(1d, 0.5)
        });
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, Height, 0, 1d),
            Normal = new Point4(1, 0, 0, 0),
            TextureMap = new Point(1d, 1d)
        });
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, Height, Depth, 1d),
            Normal = new Point4(1, 0, 0, 0),
            TextureMap = new Point(2/3d, 1d)
        });


        // Left Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, 0, 1d),
            Normal = new Point4(-1, 0, 0, 0),
            TextureMap = new Point(2/3d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, Depth, 1d),
            Normal = new Point4(-1, 0, 0, 0),
            TextureMap = new Point(1d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, Depth, 1d),
            Normal = new Point4(-1, 0, 0, 0),
            TextureMap = new Point(1d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, 0, 1d),
            Normal = new Point4(-1, 0, 0, 0),
            TextureMap = new Point(2/3d, 0.5)
        });


        // Top Face

        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, Depth, 1d),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, Depth, 1d),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(2/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, 0, 1d),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(2/3d, 1d)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, 0, 1d),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(1/3d, 1d)
        });

        // Bottom Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, 0, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(1/3d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, 0, 0, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(2/3d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, 0, Depth, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(2/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, Depth, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
    }

    public override void Draw(WriteableBitmap wbm, WriteableBitmap texture)
    {
        var drawingData = new List<Pixel>();
        
        // Front Face
        var frontTriangle1 = new Triangle(Vertices[0], Vertices[3], Vertices[2]);
        if (frontTriangle1.IsFacingCamera())
        {
            frontTriangle1.Fill(drawingData, texture);
            frontTriangle1.Draw(drawingData);
        }
        var frontTriangle2 = new Triangle(Vertices[0], Vertices[2], Vertices[1]);
        if (frontTriangle2.IsFacingCamera())
        {              
            frontTriangle2.Fill(drawingData, texture);
            frontTriangle2.Draw(drawingData);
        }

        // Back Face
        var backTriangle1 = new Triangle(Vertices[4], Vertices[7], Vertices[6]);
        if (backTriangle1.IsFacingCamera())
        {
            backTriangle1.Fill(drawingData, texture);
            backTriangle1.Draw(drawingData);
        }
        var backTriangle2 = new Triangle(Vertices[4], Vertices[6], Vertices[5]);
        if (backTriangle2.IsFacingCamera())
        {              
            backTriangle2.Fill(drawingData, texture);
            backTriangle2.Draw(drawingData);
        }

        // Right Face
        var rightTriangle1 = new Triangle(Vertices[8], Vertices[11], Vertices[10]);
        if (rightTriangle1.IsFacingCamera())
        {
            rightTriangle1.Fill(drawingData, texture);
            rightTriangle1.Draw(drawingData);
        }
        var rightTriangle2 = new Triangle(Vertices[8], Vertices[10], Vertices[9]);
        if (rightTriangle2.IsFacingCamera())
        {              
            rightTriangle2.Fill(drawingData, texture);
            rightTriangle2.Draw(drawingData);
        }

        // Right Face
        var leftTriangle1 = new Triangle(Vertices[12], Vertices[15], Vertices[14]);
        if (leftTriangle1.IsFacingCamera())
        {
            leftTriangle1.Fill(drawingData, texture);
            leftTriangle1.Draw(drawingData);
        }
        var leftTriangle2 = new Triangle(Vertices[12], Vertices[14], Vertices[13]);
        if (leftTriangle2.IsFacingCamera())
        {              
            leftTriangle2.Fill(drawingData, texture);
            leftTriangle2.Draw(drawingData);
        }

        // Top Face
        var topTriangle1 = new Triangle(Vertices[16], Vertices[19], Vertices[18]);
        if (topTriangle1.IsFacingCamera())
        {
            topTriangle1.Fill(drawingData, texture);
            topTriangle1.Draw(drawingData);
        }
        var topTriangle2 = new Triangle(Vertices[16], Vertices[18], Vertices[17]);
        if (topTriangle2.IsFacingCamera())
        {              
            topTriangle2.Fill(drawingData, texture);
            topTriangle2.Draw(drawingData);
        }

        // Bottom Face
        var bottomTriangle1 = new Triangle(Vertices[20], Vertices[23], Vertices[22]);
        if (bottomTriangle1.IsFacingCamera())
        {
            bottomTriangle1.Fill(drawingData, texture);
            bottomTriangle1.Draw(drawingData);
        }
        var bottomTriangle2 = new Triangle(Vertices[20], Vertices[22], Vertices[21]);
        if (bottomTriangle2.IsFacingCamera())
        {              
            bottomTriangle2.Fill(drawingData, texture);
            bottomTriangle2.Draw(drawingData);
        }

        wbm.SetPixels(drawingData);
        drawingData.Clear();
    }
}