using System;
using System.Collections.Generic;
using System.Windows;

namespace Lab05___3DModelling;

public class Sphere : Model
{
    public Sphere(int m, int n, int r)
    {
        M = m;
        N = n;
        Radius = r;
    }

    public override void CalculateVertices()
    {
        //0        - north pole
        //1..mn    - sides
        //mn+1     - south pole

        // north pole
        Vertices.Add(new Point3d //t0
        {
            Global = new Point4(0, Radius, 0, 1d),
            Normal = new Point4(0, 1d, 0, 0),
            TextureMap = new Point(0.5, 1d)
        });

        // sides t1...t(mn+1)

        for (var i = 0; i < N; i++)
        {
            for (var j = 0; j < M; j++)
            {
                var xdivR = Math.Cos((2*Math.PI*j)/M)*Math.Sin((Math.PI*(i + 1))/(N + 1));
                var ydivR = Math.Cos((Math.PI*(i + 1))/(N + 1));
                var zdivR = Math.Sin((2*Math.PI*j)/M)*Math.Sin((Math.PI*(i + 1))/(N + 1));

                Vertices.Add(new Point3d 
                {
                    Global = new Point4(Radius*xdivR, Radius*ydivR, Radius*zdivR, 1),
                    Normal = new Point4(xdivR, ydivR, zdivR, 0),
                    TextureMap = new Point((double)j/M, 1 - (i+1)/(N+1d))
                });
            }
        }

        // south pole
        Vertices.Add(new Point3d //t(mn+2)
        {
            Global = new Point4(0, -Radius, 0, 1d),
            Normal = new Point4(0, -1d, 0, 0),
            TextureMap = new Point(0.5, 0)
        });
    }

    protected override List<Triangle> CalculateTriangles()
    {
        var triangles = new List<Triangle>();
        
        // Sphere Top Lid
        for (var i = 0; i < M - 1; i++)
        {
            var topLidTriangle = new Triangle(Vertices[0], Vertices[i+2], Vertices[i+1]);
            if (topLidTriangle.IsFacingCamera())
            {
                triangles.Add(topLidTriangle);
            }
        }

        // Sphere Top Lid last triangle
        var lastTopLidTriangle = new Triangle(Vertices[0],
                                            new Point3d
                                            {
                                                Projected = Vertices[1].Projected,
                                                Global = Vertices[1].Global,
                                                Normal = Vertices[1].Normal,
                                                TextureMap = new Point(1d, 1-1d/N)
                                            },
                                            Vertices[M]);
        
        if (lastTopLidTriangle.IsFacingCamera())
        {
            triangles.Add(lastTopLidTriangle);
        }

        // Sphere Bottom Lid
        for (var i = 0; i < M - 1; i++)
        {
            var bottomLidTriangle = new Triangle(Vertices[M*N + 1], Vertices[(N-1)*M + i + 1], Vertices[(N-1)*M + i + 2]);
            if (bottomLidTriangle.IsFacingCamera())
            {
                triangles.Add(bottomLidTriangle);
            }
        }

        // Sphere Bottom Lid last triangle
        var lastBottomLidTriangle = new Triangle(Vertices[M*N + 1],
                                                Vertices[M*N],
                                                new Point3d
                                                {
                                                    Projected = Vertices[(N-1)*M + 1].Projected,
                                                    Global = Vertices[(N-1)*M + 1].Global,
                                                    Normal = Vertices[(N-1)*M + 1].Normal,
                                                    TextureMap = new Point(1d, 1d/(N+1))
                                                });
        
        if (lastBottomLidTriangle.IsFacingCamera())
        {
            triangles.Add(lastBottomLidTriangle);
        }

        // Rings making up the strips
        for (var i = 0; i < N - 1; i++)
        {
            // upper strip triangles
            for (var j = 1; j < M; j++)
            {
                var upperStripTriangle = new Triangle(Vertices[i*M + j], Vertices[i*M + j + 1], Vertices[(i+1)*M + j + 1]);
                if (upperStripTriangle.IsFacingCamera())
                {
                    triangles.Add(upperStripTriangle);
                }
            }

            // last upper strip triangle
            var lastUpperStripTriangle = new Triangle(Vertices[(i+1)*M], 
                                                        new Point3d
                                                        {
                                                            Projected = Vertices[i*M + 1].Projected,
                                                            Global = Vertices[i*M + 1].Global,
                                                            Normal = Vertices[i*M + 1].Normal,
                                                            TextureMap = new Point { X = 1d, Y = Vertices[i*M + 1].TextureMap.Y}
                                                        },
                                                        new Point3d
                                                        {
                                                            Projected = Vertices[(i+1)*M + 1].Projected,
                                                            Global = Vertices[(i+1)*M + 1].Global,
                                                            Normal = Vertices[(i+1)*M + 1].Normal,
                                                            TextureMap = new Point { X = 1d, Y = Vertices[(i+1)*M + 1].TextureMap.Y}
                                                        });
            if (lastUpperStripTriangle.IsFacingCamera())
            {
                triangles.Add(lastUpperStripTriangle);
            }

            // lower strip triangles
            for (var j = 1; j < M; j++)
            {
                var lowerStripTriangle = new Triangle(Vertices[(i*M) + j], Vertices[(i+1)*M + j + 1], Vertices[(i+1)*M + j]);
                if (lowerStripTriangle.IsFacingCamera())
                {
                    triangles.Add(lowerStripTriangle);
                }
            }

            // last lower strip triangle
            var lastLowerStripTriangle = new Triangle(Vertices[(i+1)*M], 
                                                        new Point3d
                                                        {
                                                            Projected = Vertices[(i+1)*M + 1].Projected,
                                                            Global = Vertices[(i+1)*M + 1].Global,
                                                            Normal = Vertices[(i+1)*M + 1].Normal,
                                                            TextureMap = new Point { X = 1d, Y = Vertices[(i+1)*M + 1].TextureMap.Y}
                                                        },
                                                        Vertices[(i+2)*M]);
            if (lastLowerStripTriangle.IsFacingCamera())
            {
                triangles.Add(lastLowerStripTriangle);
            }
        }

        return triangles;
    }
}