using EOS.BaseClasses;
using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Expedition
{
    public sealed class ExpeditionDefinitionManager : BaseManager
    {
        protected override string DEFINITION_NAME => "ExtraExpeditionSettings";
        
        public static ExpeditionDefinitionManager Current { get; private set; } = new();
        
        private readonly Dictionary<uint, ExpeditionDefinition> _definitions = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new ExpeditionDefinition()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<ExpeditionDefinition>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e) 
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                var conf = EOSJson.Deserialize<ExpeditionDefinition>(content);
                AddDefinitions(conf);
            });
        }

        private void AddDefinitions(ExpeditionDefinition definitions)
        {
            if (definitions == null) return;

            if (_definitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            _definitions[definitions.MainLevelLayout] = definitions;
        }

        public bool TryGetDefinition(uint mainLevelLayout, [MaybeNullWhen(false)] out ExpeditionDefinition definition)
        {
            return _definitions.TryGetValue(mainLevelLayout, out definition);
        }
    }
}
