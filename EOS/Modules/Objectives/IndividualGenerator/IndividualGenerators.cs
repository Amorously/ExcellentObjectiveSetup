using EOS.BaseClasses;
using GameData;

namespace EOS.Modules.Objectives.IndividualGenerator
{
    public class IndividualGeneratorDefinition : BaseInstanceDefinition
    {
        public bool ForceAllowPowerCellInsertion { get; set; } = false;

        public List<WardenObjectiveEventData> EventsOnInsertCell { get; set; } = new();

        public Vec3 Position { get; set; } = new();

        public Vec3 Rotation { get; set; } = new();

        public bool RepositionCover { get; set; } = false;

        public bool HideCover { get; set; } = false;
    }
}