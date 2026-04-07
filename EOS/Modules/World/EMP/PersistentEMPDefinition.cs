namespace EOS.Modules.World.EMP
{
    public readonly record struct ItemToDisable
    (
        bool BioTracker = true,
        bool PlayerHUD = true,
        bool PlayerFlash = true,
        bool EnvLight = true,
        bool GunSight = true,
        bool Sentry = true,
        bool Map = true
    );

    public class PersistentEMPDefinition
    {
        public uint pEMPIndex { get; set; } = 0u;

        public Vec3 Position { get; set; } = new();

        public float Range { get; set; } = 0f;

        public ItemToDisable ItemToDisable { get; set; } = new();
    }
}
