using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.TerminalPosition
{
    internal sealed class TerminalPositionOverrideManager: InstanceDefinitionManager<TerminalPosition>
    {
        public static TerminalPositionOverrideManager Current = new();

        protected override string DEFINITION_NAME => "TerminalPosition";
    }
}
