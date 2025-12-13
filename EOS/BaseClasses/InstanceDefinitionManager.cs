using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class InstanceDefinitionManager<T> : BaseManager where T : BaseInstanceDefinition, new()
    { 
        protected Dictionary<uint, InstanceDefinitionsForLevel<T>> Definitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new InstanceDefinitionsForLevel<T>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<InstanceDefinitionsForLevel<T>>(content);
                AddDefinitions(conf);
            }
        }

        protected virtual void AddDefinitions(InstanceDefinitionsForLevel<T> definitions)
        {
            if (definitions == null) return;

            if (Definitions.ContainsKey(definitions.MainLevelLayout))
            {
                EOSLogger.Log("Replaced MainLevelLayout {0}", definitions.MainLevelLayout);
            }

            Definitions[definitions.MainLevelLayout] = definitions;
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                InstanceDefinitionsForLevel<T> conf = EOSJson.Deserialize<InstanceDefinitionsForLevel<T>>(content);
                AddDefinitions(conf);
            });
        }

        public virtual List<T> GetDefinitionsForLevel(uint mainLevelLayout)
        {
            return Definitions.TryGetValue(mainLevelLayout, out var def) ? def.Definitions : new();
        }

        public virtual T? GetDefinition(int dim, int layer, int zone, uint instanceIndex) 
            => TryGetDefinition(dim, layer, zone, instanceIndex, out var definition) ? definition : null;
        
        public virtual T? GetDefinition((int, int, int) globalIndex, uint instanceIndex) 
            => TryGetDefinition(globalIndex, instanceIndex, out var definition) ? definition : null;        

        public virtual bool TryGetDefinition((int, int, int) globalIndex, uint instanceIndex, [MaybeNullWhen(false)] out T definition) 
            => TryGetDefinition(globalIndex, instanceIndex, out definition);

        public virtual bool TryGetDefinition(int dim, int layer, int zone, uint instanceIndex, [MaybeNullWhen(false)] out T definition)
        {
            definition = null;

            if (!Definitions.TryGetValue(CurrentMainLevelLayout, out var layout))
                return false;

            var tuple = (dim, layer, zone);
            definition = layout.Definitions.Find(def => def.IntTuple == tuple && def.InstanceIndex == instanceIndex);
            return definition != null;
        }

        protected void Sort(InstanceDefinitionsForLevel<T> levelDefs)
        {
            levelDefs.Definitions.Sort((u1, u2) =>
            {
                int cmp = u1.IntTuple.CompareTo(u2.IntTuple);
                return cmp != 0 ? cmp : u1.InstanceIndex.CompareTo(u2.InstanceIndex);
            });
        }        
    }
}
