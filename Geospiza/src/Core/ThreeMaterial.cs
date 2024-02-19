using System.Drawing;

namespace Geospiza.Core;

public class ThreeMaterial
{
    public Color Color { get; set; }
    public double Metalness { get; set; }
    public double Roughness { get; set; }
    public double Opacity { get; set; }
    
    public ThreeMaterial(Color color, double metalness, double roughness, double opacity)
    {
        Color = color;
        Metalness = metalness;
        Roughness = roughness;
        Opacity = opacity;
    }
    
    public ThreeMaterial()
    {
        Color = Color.White;
        Metalness = 0.0;
        Roughness = 0.5;
        Opacity = 1.0;
    }
}