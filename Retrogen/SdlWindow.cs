using Retrogen.Mathematics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retrogen
{
    /// <summary>
    /// Wraps away the window creation, renderer creation, fps cap stuff and pixel drawing.
    /// </summary>
    public class SdlWindow : IDisposable
    {
        private class TimerState
        {
            public uint Interval;
            public SdlWindow? Window;
        }

        private IntPtr window;
        private IntPtr renderer;
        private IntPtr renderSurface;
        private string title = string.Empty;

        public bool ShouldClose { get; set; } = false;
        public int Width { get; }
        public int Height { get; }
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                SDL.SDL_SetWindowTitle(window, title);
            }
        }
        public int FPS { get; private set; }

        /// <summary>
        /// The SDL window used to render the content
        /// </summary>
        /// <param name="title"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public SdlWindow(string title = "Retrogen Window", int width = 800, int height = 600)
        {
            Width = width;
            Height = height;
            Title = title;

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                throw new InvalidOperationException($"SDL could not initialize! SDL Error: {SDL.SDL_GetError()}");
            }

            window = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Window could not be created! SDL Error: {SDL.SDL_GetError()}");
            }

            renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
            if (renderer == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Renderer could not be created! SDL Error: {SDL.SDL_GetError()}");
            }

            renderSurface = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ABGR8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
            if (renderSurface == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Renderer surface could not be created! SDL Error: {SDL.SDL_GetError()}");
            }

            
        }

        /// <summary>
        /// Run the current window.
        /// </summary>
        /// <param name="drawCallback">The draw method that will be called targetFps times per second</param>
        /// <param name="targetFps">Maximum fps the draw callback will be called. Default is 60 fps</param>
        public void Run(Action loadCallback, Action<float> updateCallback, Action<Renderer> drawCallback, int targetFps = 60)
        {
            ShouldClose = false;
            var rendererInstance = new Renderer(renderer, renderSurface, Width, Height);

            int targetFrameTime = targetFps > 0 ? 1000 / targetFps : 0; // target time per frame in milliseconds
            uint lastFrameTime = SDL.SDL_GetTicks();
            uint lastUpdateTime = lastFrameTime;
            uint frameTimeSum = 0;
            uint frameCount = 0;

            loadCallback();

            while (!ShouldClose)
            {
                uint currentTime = SDL.SDL_GetTicks();
                uint deltaTime = currentTime - lastFrameTime;
                uint frameTimeElapsed = currentTime - lastUpdateTime;

                if (targetFps <= 0 || frameTimeElapsed >= targetFrameTime)
                {
                    lastFrameTime = currentTime;
                    lastUpdateTime = currentTime;

                    // Call the update callback
                    updateCallback(deltaTime);

                    // Call the draw callback
                    drawCallback(rendererInstance);

                    // Update the FPS variable
                    frameTimeSum += deltaTime;
                    frameCount++;
                    FPS = (int)(frameCount * 1000f / frameTimeSum);
                }
            }
        }

        public void Dispose()
        {
            SDL.SDL_DestroyTexture(renderSurface);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }
}
