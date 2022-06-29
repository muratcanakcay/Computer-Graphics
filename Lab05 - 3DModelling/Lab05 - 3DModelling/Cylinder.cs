using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Lab05___3DModelling;

public class Cylinder : Model
{
    public Cylinder(int n, int h, int r)
    {
        N = n;
        Height = h;
        Radius = r;
        Vertices = new List<Point3d>() { }; 
    }

    public void CalculateVertices()
    {
        //0...n       - top base
        //n+1...3n    - sides
        //3n+1...4n+1 - bottom base

        //Top base
        Vertices.Add(new Point3d //t0
        {
            Global  = new Point4(0, Height, 0, 1),
            Normal = new Point4(0, 1, 0, 0),
            TextureMap = new Point(0.25, 0.25)
        });   

        for(var i = 0; i < N; i++) //t1 - tn
        {
            var x = Radius * Math.Cos((2 * Math.PI * i) / N);
            var z = Radius * Math.Sin((2 * Math.PI * i) / N);
            var xTex = 0.25*(1 + Math.Cos(2*Math.PI*i/N));
            var yTex = 0.25*(1 + Math.Sin(2*Math.PI*i/N));

            Vertices.Add(new Point3d
            {
                Global  = new Point4(x, Height, z, 1),
                Normal = new Point4(0, 1, 0, 0),
                TextureMap = new Point(xTex, yTex)
            });
        }
        
        //Sides
        for (var i = 1; i <= N; i++) // t(n+1) - t(2n)
        {
            var x = Radius * Math.Cos((2 * Math.PI * i) / N);
            var z = Radius * Math.Sin((2 * Math.PI * i) / N);

            Vertices.Add(new Point3d
            {
                Global  = new Point4(x, Height, z, 1),
                Normal = new Point4(x / Radius, 0, z / Radius, 0),
                TextureMap = new Point((i-1d)/N, 1)
            });
        }
        
        for (var i = 1; i <= N; i++) // t(2n+1) - t(3n)
        {
            var x = Radius * Math.Cos((2 * Math.PI * i) / N);
            var z = Radius * Math.Sin((2 * Math.PI * i) / N);

            Vertices.Add(new Point3d
            {
                Global  = new Point4(x, 0, z, 1),
                Normal = new Point4(x / Radius, 0, z / Radius, 0),
                TextureMap = new Point((i-1)/(double)N, 0.5)
            });
        }
        
        //Bottom base
        for (var i = 0; i < N; i++) // t(3n +1) - t(4n)
        {
            var x = Radius * Math.Cos((2 * Math.PI * i) / N);
            var z = Radius * Math.Sin((2 * Math.PI * i) / N);
            var xTex = 0.25*(3 + Math.Cos(2*Math.PI*(i+3*N)/N));
            var yTex = 0.25*(1 + Math.Sin(2*Math.PI*(i+3*N)/N));
            
            Vertices.Add(new Point3d
            {
                Global  = new Point4(x, 0, z, 1),
                Normal = new Point4(0, -1, 0, 0),
                TextureMap = new Point(xTex, yTex)
            });
        }
        
        Vertices.Add(new Point3d   // t4n+1
        {
            Global  = new Point4(0, 0, 0, 1),
            Normal = new Point4(0, -1, 0, 0),
            TextureMap = new Point(0.75, 0.25)
        });
    }

    public override void Draw(WriteableBitmap wbm, WriteableBitmap texture)
    {
        var drawingData = new List<Pixel>();
        
        // Cylinder Top Base 
        for (var i = 0; i < N - 1; i++)
        {
            var topBaseTriangle = new Triangle(Vertices[0], Vertices[i+2], Vertices[i+1]);
            if (topBaseTriangle.IsFacingCamera())
            {
                topBaseTriangle.Fill(drawingData, texture);
                topBaseTriangle.Draw(drawingData);
            }
        }

        // Cylinder Top Base last triangle
        var lastTopBaseTriangle = new Triangle(Vertices[0], Vertices[1], Vertices[N]);
        if (lastTopBaseTriangle.IsFacingCamera())
        {
            lastTopBaseTriangle.Fill(drawingData, texture);
            lastTopBaseTriangle.Draw(drawingData);
        }

        // Cylinder Side with edge on top base
        for (var i = N; i <= 2 * N - 2; i++)
        {
            var topSideTriangle = new Triangle(Vertices[i+1], Vertices[i+2], Vertices[i+1+N]);
            if (topSideTriangle.IsFacingCamera())
            {
                topSideTriangle.Fill(drawingData, texture);
                topSideTriangle.Draw(drawingData);
            }
        }

        // Cylinder Side with edge on top base last triangle
        var lastTopSideTriangle = new Triangle(Vertices[2*N], 
                                                new Point3d
                                                {
                                                    Projected = Vertices[N+1].Projected,
                                                    Global = Vertices[N+1].Global,
                                                    Normal = Vertices[N+1].Normal,
                                                    TextureMap = new Point(1,1)
                                                }, 
                                                Vertices[3*N]);
        if (lastTopSideTriangle.IsFacingCamera())
        {
            lastTopSideTriangle.Fill(drawingData, texture);
            lastTopSideTriangle.Draw(drawingData);
        }

        // Cylinder Side with edge on bottom base
        for (var i = 2 * N; i <= 3 * N - 2; i++)
        {
            var bottomSideTriangle = new Triangle(Vertices[i+1], Vertices[i+2-N], Vertices[i+2]);
            if (bottomSideTriangle.IsFacingCamera())
            {
                bottomSideTriangle.Fill(drawingData, texture);
                bottomSideTriangle.Draw(drawingData);
            }
        }

        // Cylinder Side with edge on bottom base last triangle
        var lastBottomSideTriangle = new Triangle(new Point3d
                                                {
                                                    Projected = Vertices[2*N+1].Projected,
                                                    Global = Vertices[2*N+1].Global,
                                                    Normal = Vertices[2*N+1].Normal,
                                                    TextureMap = new Point(1,0.5)
                                                },
                                                Vertices[3*N],
                                                new Point3d
                                                {
                                                    Projected = Vertices[N+1].Projected,
                                                    Global = Vertices[N+1].Global,
                                                    Normal = Vertices[N+1].Normal,
                                                    TextureMap = new Point(1,1)
                                                });
        if (lastBottomSideTriangle.IsFacingCamera())
        {
            lastBottomSideTriangle.Fill(drawingData, texture);
            lastBottomSideTriangle.Draw(drawingData);
        }

        // Cylinder Bottom Base 
        for (var i = 3 * N; i <= 4 * N - 2; i++)
        {
            var bottomBaseTriangle = new Triangle(Vertices[4*N+1], Vertices[i+1], Vertices[i+2]);
            if (bottomBaseTriangle.IsFacingCamera())
            {
                bottomBaseTriangle.Fill(drawingData, texture);
                bottomBaseTriangle.Draw(drawingData);
            }
        }
        
        // Cylinder Bottom Base last triangle
        var lastBottomBaseTriangle = new Triangle(Vertices[4*N+1], Vertices[4*N], Vertices[3*N+1]);
        if (lastBottomBaseTriangle.IsFacingCamera())
        {
            lastBottomBaseTriangle.Fill(drawingData, texture);
            lastBottomBaseTriangle.Draw(drawingData);
        }

        wbm.SetPixels(drawingData);
        drawingData.Clear();
    }
}