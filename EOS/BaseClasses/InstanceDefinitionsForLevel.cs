namespace EOS.BaseClasses
{
    public class InstanceDefinitionsForLevel<T> where T : BaseInstanceDefinition, new()
    {
        public uint MainLevelLayout { get; set; } = 0u;

        public List<T> Definitions { get; set; } = new() { new() };
    }
}
