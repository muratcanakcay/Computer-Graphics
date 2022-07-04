using System.Numerics;
using System.Windows.Media;

namespace Lab05___3DModelling;

public struct Phong
{
    public bool IsIlluminated { get; set; }
    public Vector3 Camera { get; set; }
    public Vector3 Light { get; set; }
    public Color ModelColor { get; set; }
    public float Ia => 1f;
    public float ka => 0.5f;
    public float ks => 0.75f;
    public float kd => 0.25f;
}