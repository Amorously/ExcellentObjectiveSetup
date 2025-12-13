namespace EOS.Modules.Expedition.Gears
{
    public enum Mode { ALLOW, DISALLOW }

    public class ExpeditionGearsDefinition
    {
        public Mode Mode { get; set; } = Mode.DISALLOW;
        public List<uint> GearIds { get; set; } = new() { 0u };
    }
}