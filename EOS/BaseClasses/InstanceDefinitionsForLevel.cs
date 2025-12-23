namespace EOS.BaseClasses
{
    public class InstanceDefinitionsForLevel<T> where T : BaseInstanceDefinition, new()
    {
        public uint MainLevelLayout { set; get; } = 0u;

        public List<T> Definitions { set; get; } = new() { new() };
    }
}
