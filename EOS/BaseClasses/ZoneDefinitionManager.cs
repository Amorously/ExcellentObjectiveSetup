using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class ZoneDefinitionManager<TDef, TBase> : BaseManager<TBase>
        where TDef : GlobalBased, new()
        where TBase : ZoneDefinitionManager<TDef, TBase>
    {
        protected Dictionary<uint, ZoneDefinitionsForLevel<TDef>> ZoneDefinitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new ZoneDefinitionsForLevel<TDef>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<ZoneDefinitionsForLevel<TDef>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ZoneDefinitionsForLevel<TDef> conf = EOSJson.Deserialize<ZoneDefinitionsForLevel<TDef>>(content);
                AddDefinitions(conf);
            });
        }

        protected virtual void AddDefinitions(ZoneDefinitionsForLevel<TDef> definitions)
        {
            if (definitions == null) return;

            if (ZoneDefinitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            ZoneDefinitions[definitions.MainLevelLayout] = definitions;
        }

        public virtual IReadOnlyList<TDef> GetDefinitionsForLevel(uint mainLevelLayout)
        {
            return ZoneDefinitions.TryGetValue(mainLevelLayout, out var def) ? def.Definitions : new();
        }

        public virtual TDef? GetDefinition((int, int, int) globalIndex)
            => TryGetDefinition(globalIndex, out var definition) ? definition : null;

        public virtual bool TryGetDefinition((int, int, int) globalIndex, [MaybeNullWhen(false)] out TDef definition)
        {
            var (dim, layer, zone) = globalIndex;
            return TryGetDefinition(dim, layer, zone, out definition);
        }

        public virtual bool TryGetDefinition(int dim, int layer, int zone, [MaybeNullWhen(false)] out TDef definition)
        {
            definition = null;

            if (!ZoneDefinitions.TryGetValue(CurrentMainLevelLayout, out var layout))
                return false;

            var tuple = (dim, layer, zone);
            definition = layout.Definitions.Find(def => def.IntTuple == tuple);
            return definition != null;
        }

        protected void Sort(ZoneDefinitionsForLevel<TDef> levelDefs)
        {
            levelDefs.Definitions.Sort((u1, u2) => u1.IntTuple.CompareTo(u2.IntTuple));
        }
    }
}
