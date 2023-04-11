using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Retrogen.Mathematics;
using SDL2;

namespace Retrogen
{
    public enum RenderMode
    {
        Points,
        Wireframe,
        Solid,
        Textured,
    }

    public enum ShadingMode
    {
        Flat,
        Smooth,
    }

    public class Renderer
    {
        private IntPtr renderer;
        private IntPtr renderSurface;
        private object[] lockBuffer;
        private byte[] backBuffer;
        private float[] depthBuffer;
        private int width;
        private int height;
        public Color4 ClearColor { get; set; } = new(0, 0, 0, 1.0f);
        public RenderMode RenderMode { get; set; } = RenderMode.Textured;
        public ShadingMode ShadingMode { get; set; } = ShadingMode.Smooth;

        internal Renderer(IntPtr renderer, IntPtr renderSurface, int width, int height)
        {
            this.renderer = renderer;
            this.renderSurface = renderSurface;
            this.width = width;
            this.height = height;
            backBuffer = new byte[width * height * 4];
            depthBuffer = new float[width * height];

            lockBuffer = new object[width * height];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public void Clear()
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                backBuffer[index] = (byte)(ClearColor.Red * 255);
                backBuffer[index + 1] = (byte)(ClearColor.Green * 255);
                backBuffer[index + 2] = (byte)(ClearColor.Blue * 255);
                backBuffer[index + 3] = (byte)(ClearColor.Alpha * 255);
            }

            // Clearing Depth Buffer
            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        public void Present()
        {
            // Copy backBuffer into renderSurface
            unsafe
            {
                fixed (byte* pixelPtr = &backBuffer[0])
                {
                    SDL.SDL_UpdateTexture(renderSurface, IntPtr.Zero, (IntPtr)pixelPtr, width * 4);
                }
            }

            // Copy renderSurface into renderer buffer
            SDL.SDL_Rect sourceRect = new SDL.SDL_Rect { x = 0, y = 0, w = width, h = height };
            SDL.SDL_Rect destinationRect = new SDL.SDL_Rect { x = 0, y = 0, w = width, h = height };
            SDL.SDL_RenderCopy(renderer, renderSurface, ref sourceRect, ref destinationRect);

            // Present the renderer
            SDL.SDL_RenderPresent(renderer);
        }

        // Project takes some 3D coordinates and transform them
        // in 2D coordinates using the transformation matrix
        // It also transform the same coordinates and the norma to the vertex 
        // in the 3D world
        public Vertex Project(Vertex vertex, Matrix transMat, Matrix world)
        {
            // transforming the coordinates into 2D space
            var point2d = Vector3.TransformCoordinate(vertex.Coordinates, transMat);
            // transforming the coordinates & the normal to the vertex in the 3D world
            var point3dWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);
            var normal3dWorld = Vector3.TransformCoordinate(vertex.Normal, world);

            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point2d.X * width + width / 2.0f;
            var y = -point2d.Y * height + height / 2.0f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld,
                TextureCoordinates = vertex.TextureCoordinates,
            };
        }

        // DrawPoint calls putPixel but does the clipping operation before
        public void DrawPoint(Vector3 point, Color4 color)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height)
            {
                // Drawing a point
                putPixel((int)point.X, (int)point.Y, point.Z, color);
            }
        }

        // Bresenham line algorithm 
        public void DrawLine(Vector3 point0, Vector3 point1, Color4 color)
        {
            int x0 = (int)point0.X;
            int y0 = (int)point0.Y;
            int x1 = (int)point1.X;
            int y1 = (int)point1.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = (x0 < x1) ? 1 : -1;
            var sy = (y0 < y1) ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                DrawPoint(new Vector3(x0, y0, 0), color);

                if ((x0 == x1) && (y0 == y1)) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color4 color, Texture? texture, RenderMode renderMode, ShadingMode shadingMode)
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            var p1 = v1.Coordinates;
            var p2 = v2.Coordinates;
            var p3 = v3.Coordinates;

            // normal face's vector is the average normal between each vertex's normal
            // computing also the center point of the face
            var vnFace = (v1.Normal + v2.Normal + v3.Normal) / 3;
            var centerPoint = (v1.WorldCoordinates + v2.WorldCoordinates + v3.WorldCoordinates) / 3;
            // Light position 
            var lightPos = new Vector3(0, 10, 10);
            // computing the cos of the angle between the light vector and the normal vector
            // it will return a value between 0 and 1 that will be used as the intensity of the color
            var ndotl = computeNDotL(centerPoint, vnFace, lightPos);

            // For smooth shading
            var nl1 = computeNDotL(v1.WorldCoordinates, v1.Normal, lightPos);
            var nl2 = computeNDotL(v2.WorldCoordinates, v2.Normal, lightPos);
            var nl3 = computeNDotL(v3.WorldCoordinates, v3.Normal, lightPos);

            var data = new ScanLineData { ndotla = ndotl };

            // computing lines' directions
            float dP1P2, dP1P3;

            // http://en.wikipedia.org/wiki/Slope
            // Computing slopes
            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            // First case where triangles are like that:
            // P1
            // -
            // -- 
            // - -
            // -  -
            // -   - P2
            // -  -
            // - -
            // -
            // P3
            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        if (shadingMode == ShadingMode.Smooth)
                        {
                            data.ndotla = nl1;
                            data.ndotlb = nl3;
                            data.ndotlc = nl1;
                            data.ndotld = nl2;
                        }

                        if (renderMode == RenderMode.Textured)
                        {
                            data.ua = v1.TextureCoordinates.X;
                            data.ub = v3.TextureCoordinates.X;
                            data.uc = v1.TextureCoordinates.X;
                            data.ud = v2.TextureCoordinates.X;

                            data.va = v1.TextureCoordinates.Y;
                            data.vb = v3.TextureCoordinates.Y;
                            data.vc = v1.TextureCoordinates.Y;
                            data.vd = v2.TextureCoordinates.Y;
                        }

                        processScanLine(data, v1, v3, v1, v2, color, texture, renderMode, shadingMode);
                    }
                    else
                    {
                        if (shadingMode == ShadingMode.Smooth)
                        {
                            data.ndotla = nl1;
                            data.ndotlb = nl3;
                            data.ndotlc = nl2;
                            data.ndotld = nl3;
                        }

                        if (renderMode == RenderMode.Textured)
                        {
                            data.ua = v1.TextureCoordinates.X;
                            data.ub = v3.TextureCoordinates.X;
                            data.uc = v2.TextureCoordinates.X;
                            data.ud = v3.TextureCoordinates.X;

                            data.va = v1.TextureCoordinates.Y;
                            data.vb = v3.TextureCoordinates.Y;
                            data.vc = v2.TextureCoordinates.Y;
                            data.vd = v3.TextureCoordinates.Y;
                        }
                        processScanLine(data, v1, v3, v2, v3, color, texture, renderMode, shadingMode);
                    }
                }
            }
            // First case where triangles are like that:
            //       P1
            //        -
            //       -- 
            //      - -
            //     -  -
            // P2 -   - 
            //     -  -
            //      - -
            //        -
            //       P3
            else
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        if (shadingMode == ShadingMode.Smooth)
                        {
                            data.ndotla = nl1;
                            data.ndotlb = nl2;
                            data.ndotlc = nl1;
                            data.ndotld = nl3;
                        }

                        if (renderMode == RenderMode.Textured)
                        {
                            data.ua = v1.TextureCoordinates.X;
                            data.ub = v2.TextureCoordinates.X;
                            data.uc = v1.TextureCoordinates.X;
                            data.ud = v3.TextureCoordinates.X;

                            data.va = v1.TextureCoordinates.Y;
                            data.vb = v2.TextureCoordinates.Y;
                            data.vc = v1.TextureCoordinates.Y;
                            data.vd = v3.TextureCoordinates.Y;
                        }
                        processScanLine(data, v1, v2, v1, v3, color, texture, renderMode, shadingMode);
                    }
                    else
                    {
                        if (shadingMode == ShadingMode.Smooth)
                        {
                            data.ndotla = nl2;
                            data.ndotlb = nl3;
                            data.ndotlc = nl1;
                            data.ndotld = nl3;
                        }

                        if (renderMode == RenderMode.Textured)
                        {
                            data.ua = v2.TextureCoordinates.X;
                            data.ub = v3.TextureCoordinates.X;
                            data.uc = v1.TextureCoordinates.X;
                            data.ud = v3.TextureCoordinates.X;

                            data.va = v2.TextureCoordinates.Y;
                            data.vb = v3.TextureCoordinates.Y;
                            data.vc = v1.TextureCoordinates.Y;
                            data.vd = v3.TextureCoordinates.Y;
                        }
                        processScanLine(data, v2, v3, v1, v3, color, texture, renderMode, shadingMode);
                    }
                }
            }
        }

        // The main method of the engine that re-compute each vertex projection during each frame
        public void Render(Camera camera, params Mesh?[]? meshes)
        {
            // To understand this part, please read the prerequisites resources
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)width / height, 0.01f, 1.0f);

            if (meshes == null) return;

            foreach (Mesh? mesh in meshes)
            {
                if (mesh == null) continue;
                // Beware to apply rotation before translation 
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) * Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                switch (RenderMode)
                {
                    case RenderMode.Points:
                        renderPoints(mesh, transformMatrix, worldMatrix, new Color4(1.0f, 1.0f, 0f, 1.0f));
                        break;
                    case RenderMode.Wireframe:
                        renderWireframe(mesh, transformMatrix, worldMatrix, new Color4(1.0f, 1.0f, 0f, 1.0f));
                        break;
                    case RenderMode.Solid:
                        renderSolid(mesh, transformMatrix, worldMatrix, null, RenderMode, ShadingMode);
                        break;
                    case RenderMode.Textured:
                        renderSolid(mesh, transformMatrix, worldMatrix, mesh.Textures, RenderMode, ShadingMode);
                        break;
                }
            }
        }

        private void renderSolid(Mesh mesh, Matrix transformMatrix, Matrix worldMatrix, Texture[]? textures, RenderMode renderMode, ShadingMode shadingMode)
        {
            Parallel.For(0, mesh.Faces.Length, faceIndex =>
            {
                var face = mesh.Faces[faceIndex];
                var vertexA = mesh.Vertices[face.A];
                var vertexB = mesh.Vertices[face.B];
                var vertexC = mesh.Vertices[face.C];

                var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                var color = 1.0f;

                var textureIndex = face.TextureIndex;
                Texture? texture = null;
                if (textures != null && textures.Length > 0)
                {
                    if (textureIndex >= 0 && textureIndex < textures.Length)
                    {
                        texture = textures[textureIndex];
                    }
                }

                DrawTriangle(pixelA, pixelB, pixelC, new Color4(color, color, color, 1), texture, renderMode, shadingMode);

                faceIndex++;
            });
        }

        private void renderWireframe(Mesh mesh, Matrix transformMatrix, Matrix worldMatrix, Color4 color)
        {
            foreach (var face in mesh.Faces)
            {
                var vertexA = mesh.Vertices[face.A];
                var vertexB = mesh.Vertices[face.B];
                var vertexC = mesh.Vertices[face.C];

                var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                var vecA = new Vector3(pixelA.Coordinates[0], pixelA.Coordinates[1], pixelA.Coordinates[2]);
                var vecB = new Vector3(pixelB.Coordinates[0], pixelB.Coordinates[1], pixelB.Coordinates[2]);
                var vecC = new Vector3(pixelC.Coordinates[0], pixelC.Coordinates[1], pixelC.Coordinates[2]);

                DrawLine(vecA, vecB, color);
                DrawLine(vecB, vecC, color);
                DrawLine(vecC, vecA, color);
            }
        }

        private void renderPoints(Mesh mesh, Matrix transformMatrix, Matrix worldMatrix, Color4 color)
        {
            foreach (var vertex in mesh.Vertices)
            {
                // First, we project the 3D coordinates into the 2D space
                var point = Project(vertex, transformMatrix, worldMatrix);
                // Then we can draw on screen
                var vec = new Vector3(point.Coordinates[0], point.Coordinates[1], point.Coordinates[2]);
                DrawPoint(vec, color);
            }
        }

        private void putPixel(int x, int y, float z, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * width);
            var index4 = index * 4;

            // Protecting our buffer against threads concurrencies
            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return; // Discard
                }

                depthBuffer[index] = z;

                backBuffer[index4] = (byte)(color.Red * 255);
                backBuffer[index4 + 1] = (byte)(color.Green * 255);
                backBuffer[index4 + 2] = (byte)(color.Blue * 255);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
            }
        }

        // drawing line between 2 points from left to right
        // papb -> pcpd
        // pa, pb, pc, pd must then be sorted before
        void processScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color4 color, Texture? texture, RenderMode render, ShadingMode shadingMode)
        {
            Vector3 pa = va.Coordinates;
            Vector3 pb = vb.Coordinates;
            Vector3 pc = vc.Coordinates;
            Vector3 pd = vd.Coordinates;

            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            var gradient1 = pa.Y != pb.Y ? (data.currentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (data.currentY - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)interpolate(pa.X, pb.X, gradient1);
            int ex = (int)interpolate(pc.X, pd.X, gradient2);

            // starting Z & ending Z
            float z1 = interpolate(pa.Z, pb.Z, gradient1);
            float z2 = interpolate(pc.Z, pd.Z, gradient2);

            // Interpolating normals on Y (For smooth shading)
            var snl = interpolate(data.ndotla, data.ndotlb, gradient1);
            var enl = interpolate(data.ndotlc, data.ndotld, gradient2);

            // Interpolating texture coordinates on Y
            var su = interpolate(data.ua, data.ub, gradient1);
            var eu = interpolate(data.uc, data.ud, gradient2);
            var sv = interpolate(data.va, data.vb, gradient1);
            var ev = interpolate(data.vc, data.vd, gradient2);


            // drawing a line from left (sx) to right (ex) 
            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = interpolate(z1, z2, gradient);
                
                // Interpolate shading
                float ndotl = 0.0f;
                if (shadingMode == ShadingMode.Flat)
                {
                    ndotl = data.ndotla;
                }
                else if (shadingMode == ShadingMode.Smooth)
                {
                    ndotl = interpolate(snl, enl, gradient);
                }

                // Interpolate texture coordinates on X
                var u = interpolate(su, eu, gradient);
                var v = interpolate(sv, ev, gradient);

                Color4 textureColor;

                if (texture != null && RenderMode == RenderMode.Textured)
                    textureColor = texture.MapBmp(u, v);
                else
                    textureColor = new Color4(1, 1, 1, 1);

                // changing the color value using the cosine of the angle
                // between the light vector and the normal vector
                // and the texture color
                DrawPoint(new Vector3(x, data.currentY, z), color * ndotl * textureColor);
            }
        }

        // Clamping values to keep them between 0 and 1
        private float clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        // Interpolating the value between 2 vertices 
        // min is the starting point, max the ending point
        // and gradient the % between the 2 points
        private float interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * clamp(gradient);
        }

        // Compute the cosine of the angle between the light vector and the normal vector
        // Returns a value between 0 and 1
        private float computeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            var lightDirection = lightPosition - vertex;

            normal.Normalize();
            lightDirection.Normalize();

            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }
    }
}
