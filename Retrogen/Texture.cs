using Retrogen.Mathematics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Retrogen
{
    // Only 24-bit bmp images are supported
    public class Texture
    {
        private byte[] buffer;
        private int width;
        private int height;

        // Working with a fix sized texture (512x512, 1024x1024, etc.).
        public Texture(string filename)
        {
            loadBmp(filename);
        }

        // Takes the U & V coordinates
        // and return the corresponding pixel color in the texture
        public Color4 MapBmp(float tu, float tv)
        {
            // Image is not loaded yet
            if (buffer == null)
            {
                return Color4.White;
            }
            // using a % operator to cycle/repeat the texture if needed
            int u = Math.Abs((int)(tu * width) % width);
            //int v = Math.Abs((int)(tv * height) % height);
            int v = Math.Abs((int)((1 - tv) * height) % height); // Flip the V coordinate

            int pos = (u + v * width) * 3;
            byte b = buffer[pos];
            byte g = buffer[pos + 1];
            byte r = buffer[pos + 2];

            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
        }

        private void loadBmp(string filename)
        {
            IntPtr surfacePtr = SDL.SDL_LoadBMP(filename);

            if (surfacePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to load image: {SDL.SDL_GetError()}");
            }

            var surface = Marshal.PtrToStructure<SDL.SDL_Surface>(surfacePtr);
            width = surface.w;
            height = surface.h;

            int dataSize = width * height * 3;
            var data = new byte[dataSize];
            Marshal.Copy(surface.pixels, data, 0, dataSize);

            buffer = data;

            SDL.SDL_FreeSurface(surfacePtr);
        }
    }
}
