namespace EOS.BaseClasses
{
    public class GenericExpeditionDefinition<T> where T : new()
    {
        public uint MainLevelLayout { get; set; } = 0u;

        public List<T> Definitions { get; set; } = new() { new() };
    }
}
