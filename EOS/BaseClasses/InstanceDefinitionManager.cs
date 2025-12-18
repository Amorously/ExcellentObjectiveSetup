using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class InstanceDefinitionManager<TDef, TBase> : BaseManager<TBase>
        where TDef : BaseInstanceDefinition, new()
        where TBase : InstanceDefinitionManager<TDef, TBase>
    {
        protected Dictionary<uint, InstanceDefinitionsForLevel<TDef>> InstanceDefinitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new InstanceDefinitionsForLevel<TDef>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<InstanceDefinitionsForLevel<TDef>>(content);
                AddDefinitions(conf);
            }
        }

        protected virtual void AddDefinitions(InstanceDefinitionsForLevel<TDef> definitions)
        {
            if (definitions == null) return;

            if (InstanceDefinitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            InstanceDefinitions[definitions.MainLevelLayout] = definitions;
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                InstanceDefinitionsForLevel<TDef> conf = EOSJson.Deserialize<InstanceDefinitionsForLevel<TDef>>(content);
                AddDefinitions(conf);
            });
        }

        public virtual List<TDef> GetDefinitionsForLevel(uint mainLevelLayout)
        {
            return InstanceDefinitions.TryGetValue(mainLevelLayout, out var def) ? def.Definitions : new();
        }

        public virtual TDef? GetDefinition((int, int, int) globalIndex, uint instanceIndex) 
            => TryGetDefinition(globalIndex, instanceIndex, out var definition) ? definition : null;        

        public virtual bool TryGetDefinition((int, int, int) globalIndex, uint instanceIndex, [MaybeNullWhen(false)] out TDef definition)
        {
            var (dim, layer, zone) = globalIndex;
            return TryGetDefinition(dim, layer, zone, instanceIndex, out definition);
        }

        public virtual bool TryGetDefinition(int dim, int layer, int zone, uint instanceIndex, [MaybeNullWhen(false)] out TDef definition)
        {
            definition = null;

            if (!InstanceDefinitions.TryGetValue(CurrentMainLevelLayout, out var layout))
                return false;

            var tuple = (dim, layer, zone);
            definition = layout.Definitions.Find(def => def.IntTuple == tuple && def.InstanceIndex == instanceIndex);
            return definition != null;
        }

        protected void Sort(InstanceDefinitionsForLevel<TDef> levelDefs)
        {
            levelDefs.Definitions.Sort((u1, u2) =>
            {
                int cmp = u1.IntTuple.CompareTo(u2.IntTuple);
                return cmp != 0 ? cmp : u1.InstanceIndex.CompareTo(u2.InstanceIndex);
            });
        }        
    }
}
