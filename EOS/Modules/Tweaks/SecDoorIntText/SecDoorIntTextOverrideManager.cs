using AmorLib.Utils;
using EOS.BaseClasses;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public sealed class SecDoorIntTextOverrideManager : ZoneDefinitionManager<SecDoorIntTextDefinition, SecDoorIntTextOverrideManager>
    {
        protected override string DEFINITION_NAME => "SecDoorIntText";

        public bool TryGetDefinition(LG_SecurityDoor_Locks locks, [MaybeNullWhen(false)] out SecDoorIntTextDefinition def)
        {
            var tuple = locks.m_door?.Gate?.m_linksTo?.m_zone?.ToIntTuple() ?? (-1, -1, -1);
            return TryGetDefinition(tuple, out def);
        }
    }
}
