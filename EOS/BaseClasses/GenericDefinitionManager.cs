using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class GenericDefinitionManager<TDef, TBase> : BaseManager<TBase>
        where TDef : new()
        where TBase : GenericDefinitionManager<TDef, TBase>
    {
        protected Dictionary<uint, GenericDefinition<TDef>> GenericDefinitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new GenericDefinition<TDef>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<GenericDefinition<TDef>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                var conf = EOSJson.Deserialize<GenericDefinition<TDef>>(content);
                AddDefinitions(conf);
            });
        }
        
        protected virtual void AddDefinitions(GenericDefinition<TDef> definition)
        {
            if (definition == null) return;

            if (GenericDefinitions.ContainsKey(definition.ID))
            {
                EOSLogger.Log("Replaced ID {0}", definition.ID);
            }

            GenericDefinitions[definition.ID] = definition;
        }
        
        public GenericDefinition<TDef>? GetDefinition(uint id)
        {
            return TryGetDefinition(id, out var def) ? def: null;
        }
        
        public bool TryGetDefinition(uint id, [MaybeNullWhen(false)] out GenericDefinition<TDef> definition)
        {
            if (GenericDefinitions.ContainsKey(id))
            {
                definition = GenericDefinitions[id];
                return true;
            }
            definition = null;
            return false;
        }        
    }
}
