using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class GenericExpeditionDefinitionManager<TDef, TBase> : BaseManager<TBase>
        where TDef : new()
        where TBase : GenericExpeditionDefinitionManager<TDef, TBase>
    {
        protected Dictionary<uint, GenericExpeditionDefinition<TDef>> GenericExpDefinitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new GenericExpeditionDefinition<TDef>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<GenericExpeditionDefinition<TDef>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                GenericExpeditionDefinition<TDef> conf = EOSJson.Deserialize<GenericExpeditionDefinition<TDef>>(content);
                AddDefinitions(conf);
            });
        }
        
        protected virtual void AddDefinitions(GenericExpeditionDefinition<TDef> definitions)
        {
            if (definitions == null) return;

            if (GenericExpDefinitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            GenericExpDefinitions[definitions.MainLevelLayout] = definitions;
        }

        public GenericExpeditionDefinition<TDef>? GetDefinition(uint id)
        {
            return TryGetDefinition(id, out var def) ? def : null;
        }

        public bool TryGetDefinition(uint id, [MaybeNullWhen(false)] out GenericExpeditionDefinition<TDef> definition)
        {
            if (GenericExpDefinitions.ContainsKey(id))
            {
                definition = GenericExpDefinitions[id];
                return true;
            }
            definition = null;
            return false;
        }
    }
}
