using System.Drawing;
using System.Text.Json.Serialization;

namespace GeospizaCore.Web;

/// <summary>
///     Class for displaying results in Three.js
/// </summary>
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

    [JsonPropertyName("color")] public Color Color { get; set; }

    [JsonPropertyName("metalness")] public double Metalness { get; set; }

    [JsonPropertyName("roughness")] public double Roughness { get; set; }

    [JsonPropertyName("opacity")] public double Opacity { get; set; }
}