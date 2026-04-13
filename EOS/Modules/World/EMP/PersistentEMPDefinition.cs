namespace EOS.Modules.World.EMP
{
    public readonly record struct ItemToDisable(bool BioTracker, bool PlayerHUD, bool PlayerFlash, bool EnvLight, bool GunSight, bool Sentry, bool Map);

    public class PersistentEMPDefinition
    {
        public uint pEMPIndex { get; set; } = 0u;

        public Vec3 Position { get; set; } = new();

        public float Range { get; set; } = 0f;

        public ItemToDisable ItemToDisable { get; set; } = new(true, true, true, true, true, true, true);
    }
}
