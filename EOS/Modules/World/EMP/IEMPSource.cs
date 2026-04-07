using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public interface IEMPSource
    {
        Vector3 Position { get; }
        float Range { get; }
        ItemToDisable ItemToDisable { get; }
        bool IsActive { get; }
        bool InRange(Vector3 point);
    }
}
