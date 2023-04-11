
using Retrogen.Mathematics;
using Retrogen.ModelLoaders;
using SDL2;
using System.Runtime.InteropServices;

namespace Retrogen
{
    internal class Program
    {
        private const int Width = 1080;
        private const int Height = 720;

        SdlWindow window = null!;

        //private Mesh mesh = new("Cube", 8, 12);
        private Mesh? mesh;
        private Camera camera = new();

        public static void Main(string[] args)
        {
            var program = new Program();
            program.Run();
        }

        public void Run()
        {
            using (window = new SdlWindow("3D Software Renderer", Width, Height))
            {
                window.Run(onLoad, onUpdate, onRender, targetFps: 0);
            }
        }

        private void onLoad()
        {
            // Cube mesh
            //mesh.Vertices[0] = new Vector3(-1, 1, 1);
            //mesh.Vertices[1] = new Vector3(1, 1, 1);
            //mesh.Vertices[2] = new Vector3(-1, -1, 1);
            //mesh.Vertices[3] = new Vector3(1, -1, 1);
            //mesh.Vertices[4] = new Vector3(-1, 1, -1);
            //mesh.Vertices[5] = new Vector3(1, 1, -1);
            //mesh.Vertices[6] = new Vector3(1, -1, -1);
            //mesh.Vertices[7] = new Vector3(-1, -1, -1);

            //mesh.Faces[0] = new Face { A = 0, B = 1, C = 2 };
            //mesh.Faces[1] = new Face { A = 1, B = 2, C = 3 };
            //mesh.Faces[2] = new Face { A = 1, B = 3, C = 6 };
            //mesh.Faces[3] = new Face { A = 1, B = 5, C = 6 };
            //mesh.Faces[4] = new Face { A = 0, B = 1, C = 4 };
            //mesh.Faces[5] = new Face { A = 1, B = 4, C = 5 };

            //mesh.Faces[6] = new Face { A = 2, B = 3, C = 7 };
            //mesh.Faces[7] = new Face { A = 3, B = 6, C = 7 };
            //mesh.Faces[8] = new Face { A = 0, B = 2, C = 7 };
            //mesh.Faces[9] = new Face { A = 0, B = 4, C = 7 };
            //mesh.Faces[10] = new Face { A = 4, B = 5, C = 6 };
            //mesh.Faces[11] = new Face { A = 4, B = 6, C = 7 };

            
            mesh = ObjLoader.LoadObj("goblin.obj");

            camera.Position = new Vector3(0, 0, 10.0f);
        }

        private void onUpdate(float deltaTime)
        {
            window.Title = $"3D Software Renderer  |  Fps: {window.FPS}";

            // rotating slightly the mesh during each frame rendered
            if (mesh != null)
            {
                mesh.Rotation = new Vector3(mesh.Rotation.X, mesh.Rotation.Y + 0.001f * deltaTime, mesh.Rotation.Z);
            }

            // Process SDL events
            SDL.SDL_Event e;
            while (SDL.SDL_PollEvent(out e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    window.ShouldClose = true;
                }
            }

            // Get the state of the keyboard
            IntPtr keysPtr = SDL.SDL_GetKeyboardState(out int numkeys);
            byte[] keys = new byte[numkeys];
            Marshal.Copy(keysPtr, keys, 0, numkeys);

            // Check if a key is currently being held down and move the camera accordingly
            if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_A] == 1)
            {
                camera.Position -= new Vector3(0.01f * deltaTime, 0, 0);
            }
            if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_D] == 1)
            {
                camera.Position += new Vector3(0.01f * deltaTime, 0, 0);
            }
            if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_W] == 1)
            {
                camera.Position -= new Vector3(0, 0, 0.01f * deltaTime);
            }
            if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_S] == 1)
            {
                camera.Position += new Vector3(0, 0, 0.01f * deltaTime);
            }
        }

        private void onRender(Renderer renderer)
        {
            renderer.RenderMode = RenderMode.Textured;
            renderer.ShadingMode = ShadingMode.Smooth;

            renderer.ClearColor = new Color4(0.4f, 0.4f, 0.4f, 1.0f);

            renderer.Clear();

            // Doing the various matrix operations
            renderer.Render(camera, mesh);
            // Flushing the back buffer into the front buffer
            renderer.Present();
        }
    }
}