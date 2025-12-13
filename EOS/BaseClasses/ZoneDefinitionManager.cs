using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class ZoneDefinitionManager<T> : BaseManager where T : GlobalBased, new()
    {
        protected Dictionary<uint, ZoneDefinitionsForLevel<T>> Definitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new ZoneDefinitionsForLevel<T>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<ZoneDefinitionsForLevel<T>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ZoneDefinitionsForLevel<T> conf = EOSJson.Deserialize<ZoneDefinitionsForLevel<T>>(content);
                AddDefinitions(conf);
            });
        }

        protected virtual void AddDefinitions(ZoneDefinitionsForLevel<T> definitions)
        {
            if (definitions == null) return;

            if (Definitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            Definitions[definitions.MainLevelLayout] = definitions;
        }

        public virtual List<T> GetDefinitionsForLevel(uint mainLevelLayout)
        {
            return Definitions.TryGetValue(mainLevelLayout, out var def) ? def.Definitions : new();
        }

        public virtual T? GetDefinition(int dim, int layer, int zone)
            => TryGetDefinition(dim, layer, zone, out var definition) ? definition : null;

        public virtual T? GetDefinition((int, int, int) globalIndex)
            => TryGetDefinition(globalIndex, out var definition) ? definition : null;

        public virtual bool TryGetDefinition((int, int, int) globalIndex, [MaybeNullWhen(false)] out T definition)
            => TryGetDefinition(globalIndex, out definition);

        public virtual bool TryGetDefinition(int dim, int layer, int zone, [MaybeNullWhen(false)] out T definition)
        {
            definition = null;

            if (!Definitions.TryGetValue(CurrentMainLevelLayout, out var layout))
                return false;

            var tuple = (dim, layer, zone);
            definition = layout.Definitions.Find(def => def.IntTuple == tuple);
            return definition != null;
        }

        protected void Sort(ZoneDefinitionsForLevel<T> levelDefs)
        {
            levelDefs.Definitions.Sort((u1, u2) => u1.IntTuple.CompareTo(u2.IntTuple));
        }
    }
}
