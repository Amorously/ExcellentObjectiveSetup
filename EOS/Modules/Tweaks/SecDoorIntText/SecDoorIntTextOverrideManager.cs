using AmorLib.Utils;
using EOS.BaseClasses;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        protected override void OnBuildDone()
        {
            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                var locks = def.Zone?.m_sourceGate.SpawnedDoor.TryCast<LG_SecurityDoor>()?.m_locks.TryCast<LG_SecurityDoor_Locks>();
                if (locks != null)
                {
                    ReplaceText(locks, def);
                }
            }
        }

        public void ReplaceText(LG_SecurityDoor_Locks locks, SecDoorIntTextDefinition? def = null)
        {
            if (def == null && !TryGetDefinition(locks, out def))
                return;

            if (def.ActiveTextOverrideWhitelist.Any() && !def.ActiveTextOverrideWhitelist.Contains(locks.m_lastStatus))
                return;

            locks.m_intCustomMessage.m_message = ReplaceText(locks.m_intCustomMessage.m_message);
            locks.m_intOpenDoor.InteractionMessage = ReplaceText(locks.m_intOpenDoor.InteractionMessage);

            string ReplaceText(string str)
            {
                StringBuilder sb = new();
                if (!string.IsNullOrEmpty(def.Prefix))
                {
                    sb.Append(def.Prefix).AppendLine();
                }

                string textToReplace = string.IsNullOrEmpty(def.TextToReplace) ? str : def.TextToReplace;
                sb.Append(textToReplace);

                if (!string.IsNullOrEmpty(def.Postfix))
                {
                    sb.AppendLine().Append(def.Postfix);
                }
                return sb.ToString();
            }
        }
    }
}
