using System.Numerics;
using System.Windows.Media;

namespace Lab05___3DModelling;

public struct Phong
{
    public bool IsIlluminated { get; set; } = false;
    public bool DrawMesh { get; set; } = true;
    public Vector3 Camera { get; set; }
    public Vector3 Light { get; set; }
    public Color ModelColor { get; set; }
    public float LightIntensity { get; set; }
    public float ka { get; set; } = 0.2f;
    public float ks { get; set; }
    public float kd { get; set; }
    public int n { get; set; } = 20;
    public Phong(bool drawMesh, bool isIlluminated, Vector3 camera, Vector3 light, Color modelColor, float kA = 0.2f, float kD = 0.25f, float kS = 0.75f, int _n = 20, float lightIntensity = 1f)
    {
        DrawMesh = drawMesh;
        IsIlluminated = isIlluminated;
        Camera = camera;
        Light = light;
        ModelColor= modelColor;
        LightIntensity = lightIntensity;
        ka = kA;
        kd = kD;
        ks = kS;
        n = _n;
    }    
}