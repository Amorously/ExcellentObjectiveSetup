namespace EOS.BaseClasses
{
    public class RundownWiseDefinition<T> where T : new()
    {
        public int RundownID { get; set; } = -1; // RundownWiseDefinitionManager.INVALID_RUNDOWN_ID
        public List<T> Definitions { get; set; } = new() { new() };
    }
}