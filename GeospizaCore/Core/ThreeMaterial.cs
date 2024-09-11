using System.Drawing;

namespace GeospizaManager.Core;

public class ThreeMaterial
{
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

  public Color Color { get; set; }
  public double Metalness { get; set; }
  public double Roughness { get; set; }
  public double Opacity { get; set; }
}