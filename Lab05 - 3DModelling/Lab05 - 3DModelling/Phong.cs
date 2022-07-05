using System.Numerics;
using System.Windows.Media;

namespace Lab05___3DModelling;

public struct Phong
{
    public bool IsIlluminated { get; set; } = false;
    public Vector3 Camera { get; set; }
    public Vector3 Light { get; set; }
    public Color ModelColor { get; set; }
    public float Ia { get; set; }
    public float ka { get; set; } = 0.5f;
    public float ks { get; set; } = 0.75f;
    public float kd { get; set; } = 0.25f;
    public Phong(bool isIlluminated, Vector3 camera, Vector3 light, Color modelColor, float ia = 1f)
    {
        IsIlluminated = isIlluminated;
        Camera = camera;
        Light = light;
        ModelColor= modelColor;
        Ia = ia;
    }    
}