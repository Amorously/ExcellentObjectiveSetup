using System.Text.Json.Serialization;
using UnityEngine;

namespace EOS.Utils
{
    public class Vec4: Vec3
    {
        [JsonPropertyOrder(-9)]
        public float w { get; set; } = 0;

        public Vector4 ToVector4() => new(x, y, z, w);

        public static implicit operator Vector4(Vec4 v4) => new(v4.x, v4.y, v4.z, v4.w);

        public Vec4() { }
    }
}
