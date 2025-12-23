using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.TerminalPosition
{
    public class TerminalPosition: BaseInstanceDefinition
    {
        public Vec3 Position { get; set; } = new();

        public Vec3 Rotation { get; set; } = new();

        public bool RepositionCover { get; set; } = false;
    }
}
