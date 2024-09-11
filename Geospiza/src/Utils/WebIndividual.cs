using System.Collections.Generic;
using GeospizaManager.Core;
using Rhino.Geometry;

namespace Geospiza;

public class WebIndividual
{
  public WebIndividual(Mesh mesh, ThreeMaterial material)
  {
    foreach (var vertex in mesh.Vertices)
    {
      Vertices.Add(vertex.X);
      Vertices.Add(vertex.Y);
      Vertices.Add(vertex.Z);
    }

    foreach (var face in mesh.Faces)
      if (face.IsQuad)
      {
        // First triangle
        Indices.Add(face.A);
        Indices.Add(face.B);
        Indices.Add(face.C);

        // Second triangle
        Indices.Add(face.C);
        Indices.Add(face.D);
        Indices.Add(face.A);
      }
      else if (face.IsTriangle)
      {
        Indices.Add(face.A);
        Indices.Add(face.B);
        Indices.Add(face.C);
      }

    Material = material;
  }

  public List<double> Vertices { get; set; } = new();
  public List<int> Indices { get; set; } = new();
  public ThreeMaterial Material { get; set; }

  public object ToAnonymousObject()
  {
    return new
    {
      Vertices,
      Indices,
      Material
    };
  }
}