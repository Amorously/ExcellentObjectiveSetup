using System.Text.Json.Serialization;
using UnityEngine;

namespace EOS.Utils
{
    public class Vec4: Vec3
    {
        [JsonPropertyOrder(-9)]
        public float w { get; set; } = 0;

        public Vector4 ToVector4() => new(x, y, z, w);

        public Vec4() { }
    }
}
