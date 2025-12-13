namespace EOS.BaseClasses
{
    public class GenericDefinition<T> where T : new()
    {
        public uint ID { get; set; } = 0u;
        public T Definition { get; set; } = new();
    }
}
