using System.Text.Json.Serialization;
using UnityEngine;

namespace EOS.Utils
{
    public class Vec3
    {
        [JsonPropertyOrder(-10)]
        public float x { get; set; }

        [JsonPropertyOrder(-10)]
        public float y { get; set; }

        [JsonPropertyOrder(-10)]
        public float z { get; set; }

        public Vector3 ToVector3() => new(x, y, z);

        public Quaternion ToQuaternion() => Quaternion.Euler(x, y, z);

        public static implicit operator Vector3(Vec3 v3) => new(v3.x, v3.y, v3.z);

        public static implicit operator Quaternion(Vec3 v3) => Quaternion.Euler(v3.x, v3.y, v3.z);

        public Vec3() { }
    }
}
