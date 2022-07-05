using System.Numerics;
using System.Windows.Media;

namespace Lab05___3DModelling;

public struct Phong
{
    public bool IsIlluminated { get; set; } = false;
    public Vector3 Camera { get; set; }
    public Vector3 Light { get; set; }
    public Color ModelColor { get; set; }
    public float LightIntensity { get; set; }
    public float ka { get; set; } = 0.2f;
    public float ks { get; set; }
    public float kd { get; set; }
    public int n { get; set; } = 20;
    public Phong(bool isIlluminated, Vector3 camera, Vector3 light, Color modelColor, float lightIntensity = 1f, float kD = 0.25f, float kS = 0.75f)
    {
        IsIlluminated = isIlluminated;
        Camera = camera;
        Light = light;
        ModelColor= modelColor;
        LightIntensity = lightIntensity;
        kd = kD;
        ks = kS;
    }    
}