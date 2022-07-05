using System;
using System.Collections;
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

    public override void CalculateVertices()
    {
        // Front Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, -Depth, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(0, 0.5)
        });
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, 0, -Depth, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, -Depth, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(1/3d, 1d)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, -Depth, 1d),
            Normal = new Point4(0, 0, -1, 0),
            TextureMap = new Point(0, 1d)
        });

        // Back Face
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, 0, 0, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(0, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, 0, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(1/3d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, 0, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, 0, 1d),
            Normal = new Point4(0, 0, 1, 0),
            TextureMap = new Point(0, 0.5)
        });
        


        // Right Face
        Vertices.Add(new Point3d
        {
            Global = new Point4(Width, 0, -Depth, 1d),
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
            Global = new Point4(Width, Height, -Depth, 1d),
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
            Global = new Point4(0, 0, -Depth, 1d),
            Normal = new Point4(-1, 0, 0, 0),
            TextureMap = new Point(1d, 0)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, Height, -Depth, 1d),
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
            Global = new Point4(0, Height, -Depth, 1d),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(Width, Height, -Depth, 1d),
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
            Global = new Point4(Width, 0, -Depth, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(2/3d, 0.5)
        });
        Vertices.Add(new Point3d 
        {
            Global = new Point4(0, 0, -Depth, 1d),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(1/3d, 0.5)
        });
    }

    protected override List<Triangle> CalculateTriangles()
    {
        var triangles = new List<Triangle>();
        
        // Front Face
        var frontTriangle1 = new Triangle(Vertices[0], Vertices[3], Vertices[2]);
        if (frontTriangle1.IsFacingCamera())
            triangles.Add(frontTriangle1);
        
        var frontTriangle2 = new Triangle(Vertices[0], Vertices[2], Vertices[1]);
        if (frontTriangle2.IsFacingCamera())
            triangles.Add(frontTriangle2);
        
        // Back Face
        var backTriangle1 = new Triangle(Vertices[4], Vertices[7], Vertices[6]);
        if (backTriangle1.IsFacingCamera())
            triangles.Add(backTriangle1);
        
        var backTriangle2 = new Triangle(Vertices[4], Vertices[6], Vertices[5]);
        if (backTriangle2.IsFacingCamera())
            triangles.Add(backTriangle2);
        

        // Right Face
        var rightTriangle1 = new Triangle(Vertices[8], Vertices[11], Vertices[10]);
        if (rightTriangle1.IsFacingCamera())
            triangles.Add(rightTriangle1);
        
        var rightTriangle2 = new Triangle(Vertices[8], Vertices[10], Vertices[9]);
        if (rightTriangle2.IsFacingCamera())              
            triangles.Add(rightTriangle2);
        

        // Left Face
        var leftTriangle1 = new Triangle(Vertices[12], Vertices[15], Vertices[14]);
        if (leftTriangle1.IsFacingCamera())
            triangles.Add(leftTriangle1);
        
        var leftTriangle2 = new Triangle(Vertices[12], Vertices[14], Vertices[13]);
        if (leftTriangle2.IsFacingCamera())              
            triangles.Add(leftTriangle2);
        

        // Top Face
        var topTriangle1 = new Triangle(Vertices[16], Vertices[19], Vertices[18]);
        if (topTriangle1.IsFacingCamera())
            triangles.Add(topTriangle1);
        
        var topTriangle2 = new Triangle(Vertices[16], Vertices[18], Vertices[17]);
        if (topTriangle2.IsFacingCamera())
            triangles.Add(topTriangle2);
        

        // Bottom Face
        var bottomTriangle1 = new Triangle(Vertices[20], Vertices[23], Vertices[22]);
        if (bottomTriangle1.IsFacingCamera())
            triangles.Add(bottomTriangle1);
        
        var bottomTriangle2 = new Triangle(Vertices[20], Vertices[22], Vertices[21]);
        if (bottomTriangle2.IsFacingCamera())              
            triangles.Add(bottomTriangle2);

        return triangles;
    }
}