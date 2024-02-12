using System.Collections.Generic;
using Rhino.Geometry;
using Newtonsoft.Json;

namespace Geospiza.Lib
{
    public class MeshBody
    {
        public List<double> Vertices { get; set; } = new List<double>();
        public List<int> Indices { get; set; } = new List<int>();

        public MeshBody(Mesh mesh)
        {
            foreach (var vertex in mesh.Vertices)
            {
                Vertices.Add(vertex.X);
                Vertices.Add(vertex.Y);
                Vertices.Add(vertex.Z);
            }

            foreach (var face in mesh.Faces)
            {
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
            }
        }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

