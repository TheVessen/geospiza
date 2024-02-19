using System.Collections.Generic;
using Geospiza.Comonents;
using Geospiza.Core;
using Rhino.Geometry;
using Newtonsoft.Json;

namespace Geospiza
{
    public class WebIndividual
    {
        public List<double> Vertices { get; set; } = new List<double>();
        public List<int> Indices { get; set; } = new List<int>();
        public  ThreeMaterial Material { get; set; }
        

        public WebIndividual(Mesh mesh, ThreeMaterial material)
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
            Material = material;
        }
        
        public object ToAnonymousObject()
        {
            return new
            {
                Vertices,
                Indices, 
                Material,
                Fitness = StateManager.Instance.FitnessComponent.FitnessValue
            };
        }
    }
}

