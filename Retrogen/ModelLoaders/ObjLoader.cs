using Retrogen.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retrogen.ModelLoaders
{
    public static class ObjLoader
    {
        public static Mesh LoadObj(string objPath)
        {
            var objString = File.ReadAllText(objPath);
            Dictionary<string, int> materialNameToTextureIndex;
            Texture[] textures = loadTextures(objString, out materialNameToTextureIndex);
            Mesh mesh = parseObj(objString, materialNameToTextureIndex, textures);

            return mesh;
        }

        private static Mesh parseObj(string objString, Dictionary<string, int> materialNameToTextureIndex, Texture[] textures)
        {
            // Parse the .obj file
            var lines = objString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var vertices = new List<Vertex>();
            var tempNormals = new List<Vector3>();
            var tempTextureCoordinates = new List<Vector2>();
            var faces = new List<Face>();
            string meshName = "default";
            int currentTextureIndex = -1;

            foreach (string line in lines)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length == 0)
                {
                    continue;
                }

                string keyword = tokens[0];

                switch (keyword)
                {
                    case "o":
                        meshName = tokens[1];
                        break;

                    case "v":
                        float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(tokens[3], CultureInfo.InvariantCulture);
                        vertices.Add(new Vertex { Coordinates = new Vector3(x, y, z) });
                        break;

                    case "vn":
                        float nx = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                        float ny = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                        float nz = float.Parse(tokens[3], CultureInfo.InvariantCulture);
                        tempNormals.Add(new Vector3(nx, ny, nz));
                        break;

                    case "vt":
                        float u = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                        float v = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                        tempTextureCoordinates.Add(new Vector2(u, v));
                        break;

                    case "usemtl":
                        string materialName = $"{tokens[1]}.bmp";
                        if (materialNameToTextureIndex.TryGetValue(materialName, out int textureIndex))
                        {
                            currentTextureIndex = textureIndex;
                        }
                        break;

                    case "f":
                        Face face = new Face();
                        face.TextureIndex = currentTextureIndex;

                        for (int i = 1; i <= 3; i++)
                        {
                            string[] vertexData = tokens[i].Split('/');
                            int vertexIndex = int.Parse(vertexData[0]) - 1;

                            if (i == 1) face.A = vertexIndex;
                            if (i == 2) face.B = vertexIndex;
                            if (i == 3) face.C = vertexIndex;

                            if (vertexData.Length > 1 && !string.IsNullOrEmpty(vertexData[1]))
                            {
                                int textureCoordIndex = int.Parse(vertexData[1]) - 1;
                                vertices[vertexIndex] = new Vertex
                                {
                                    Coordinates = vertices[vertexIndex].Coordinates,
                                    Normal = vertices[vertexIndex].Normal,
                                    WorldCoordinates = vertices[vertexIndex].WorldCoordinates,
                                    TextureCoordinates = tempTextureCoordinates[textureCoordIndex]
                                };
                            }

                            if (vertexData.Length > 2 && !string.IsNullOrEmpty(vertexData[2]))
                            {
                                int normalIndex = int.Parse(vertexData[2]) - 1;
                                vertices[vertexIndex] = new Vertex
                                {
                                    Coordinates = vertices[vertexIndex].Coordinates,
                                    Normal = tempNormals[normalIndex],
                                    WorldCoordinates = vertices[vertexIndex].WorldCoordinates,
                                    TextureCoordinates = vertices[vertexIndex].TextureCoordinates
                                };
                            }
                        }

                        faces.Add(face);
                        break;
                }
            }

            Mesh mesh = new Mesh(meshName, vertices.Count, faces.Count, textures);
            mesh.Vertices = vertices.ToArray();
            mesh.Faces = faces.ToArray();

            return mesh;
        }

        private static string readMaterialLibraryFilename(string objString)
        {
            // Read material library filename from the .obj file
            var lines = objString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.StartsWith("mtllib"))
                {
                    return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                }
            }
            return "";
        }

        private static Texture[] loadTextures(string objString, out Dictionary<string, int> materialNameToTextureIndex)
        {
            var lines = objString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var textureFileNames = new HashSet<string>();

            foreach (string line in lines)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length == 0)
                {
                    continue;
                }

                string keyword = tokens[0];

                if (keyword == "usemtl")
                {
                    string textureFileName = $"{tokens[1]}.bmp";
                    textureFileNames.Add(textureFileName);
                }
            }

            Texture[] textures = new Texture[textureFileNames.Count];
            int textureIndex = 0;
            materialNameToTextureIndex = new Dictionary<string, int>();

            foreach (string textureFileName in textureFileNames)
            {
                textures[textureIndex] = new Texture(textureFileName);
                materialNameToTextureIndex[textureFileName] = textureIndex;
                textureIndex++;
            }

            return textures;
        }

    }
}
