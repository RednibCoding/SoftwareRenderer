using Retrogen.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retrogen
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public float NearClipDistance { get; set; } = 0.1f;
        public float FarClipDistance { get; set; } = 100.0f;
    }
    
}
