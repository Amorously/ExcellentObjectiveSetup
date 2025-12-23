namespace EOS.BaseClasses
{
    public class ZoneDefinitionsForLevel<T> where T : GlobalBased, new()
    {
        public uint MainLevelLayout { set; get; } = 0u;

        public List<T> Definitions { set; get; } = new() { new() };
    }
}
