namespace EOS.BaseClasses
{
    public class ZoneDefinitionsForLevel<T> where T : GlobalBased, new()
    {
        public uint MainLevelLayout { get; set; } = 0u;

        public List<T> Definitions { get; set; } = new() { new() };
    }
}
