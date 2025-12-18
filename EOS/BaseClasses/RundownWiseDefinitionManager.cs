using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class RundownwiseDefinitionManager<TDef, TBase> : BaseManager<TBase>
        where TDef : new()
        where TBase : RundownwiseDefinitionManager<TDef, TBase>
    {
        public const int INVALID_RUNDOWN_ID = -1;        
        public const int APPLY_TO_ALL_RUNDOWN_ID = 0;

        protected Dictionary<int, RundownWiseDefinition<TDef>> RundownDefinitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new RundownWiseDefinition<TDef>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<RundownWiseDefinition<TDef>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                RundownWiseDefinition<TDef> conf = EOSJson.Deserialize<RundownWiseDefinition<TDef>>(content);
                AddDefinitions(conf);
            });
        }

        protected virtual void AddDefinitions(RundownWiseDefinition<TDef> definitions)
        {
            if (definitions == null || definitions.RundownID == INVALID_RUNDOWN_ID) return;

            if (RundownDefinitions.ContainsKey(definitions.RundownID))
            {
                EOSLogger.Log($"Replaced RundownID: '{definitions.RundownID}' ({APPLY_TO_ALL_RUNDOWN_ID} means 'apply to all rundowns')");
            }

            RundownDefinitions[definitions.RundownID] = definitions;
        }

        public RundownWiseDefinition<TDef>? GetDefinition(int id)
        {
            return TryGetDefinition(id, out var def) ? def : null;
        }

        public bool TryGetDefinition(int ID, [MaybeNullWhen(false)] out RundownWiseDefinition<TDef> definition)
        {
            if (RundownDefinitions.ContainsKey(ID))
            {
                definition = RundownDefinitions[ID];
                return true;
            }
            definition = null;
            return false;
        }
    }
}
