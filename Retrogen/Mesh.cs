using Retrogen.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retrogen
{
    public class Mesh
    {
        public string Name { get; set; }
        public Vertex[] Vertices { get; set; }
        public Face[] Faces { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Texture[] Textures { get; set; }

        public Mesh(string name, int verticesCount, int facesCount, Texture[] textures)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
            Textures = textures;
        }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }

        public void ComputeFacesNormals()
        {
            Parallel.For(0, Faces.Length, faceIndex =>
            {
                var face = Faces[faceIndex];
                var vertexA = Vertices[face.A];
                var vertexB = Vertices[face.B];
                var vertexC = Vertices[face.C];

                Faces[faceIndex].Normal = (vertexA.Normal + vertexB.Normal + vertexC.Normal) / 3.0f;
                Faces[faceIndex].Normal.Normalize();
            });

        }
    }
}
