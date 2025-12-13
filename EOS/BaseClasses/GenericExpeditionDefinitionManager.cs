using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class GenericExpeditionDefinitionManager<T> : BaseManager where T: new()
    {
        protected Dictionary<uint, GenericExpeditionDefinition<T>> Definitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new GenericExpeditionDefinition<T>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<GenericExpeditionDefinition<T>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                GenericExpeditionDefinition<T> conf = EOSJson.Deserialize<GenericExpeditionDefinition<T>>(content);
                AddDefinitions(conf);
            });
        }
        
        protected virtual void AddDefinitions(GenericExpeditionDefinition<T> definitions)
        {
            if (definitions == null) return;

            if (Definitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            Definitions[definitions.MainLevelLayout] = definitions;
        }

        public GenericExpeditionDefinition<T>? GetDefinition(uint id)
        {
            return TryGetDefinition(id, out var def) ? def : null;
        }

        public bool TryGetDefinition(uint id, [MaybeNullWhen(false)] out GenericExpeditionDefinition<T> definition)
        {
            if (Definitions.ContainsKey(id))
            {
                definition = Definitions[id];
                return true;
            }
            definition = null;
            return false;
        }
    }
}
