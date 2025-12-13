using EOS.JSON;
using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class RundownWiseDefinitionManager<T> : BaseManager where T: new()
    {
        public const int INVALID_RUNDOWN_ID = -1;        
        public const int APPLY_TO_ALL_RUNDOWN_ID = 0;

        protected Dictionary<int, RundownWiseDefinition<T>> Definitions { get; set; } = new();

        protected override void ReadFiles()
        {
            File.WriteAllText(Path.Combine(DEFINITION_PATH, "Template.json"), EOSJson.Serialize(new RundownWiseDefinition<T>()));

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                var conf = EOSJson.Deserialize<RundownWiseDefinition<T>>(content);
                AddDefinitions(conf);
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            EOSLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                RundownWiseDefinition<T> conf = EOSJson.Deserialize<RundownWiseDefinition<T>>(content);
                AddDefinitions(conf);
            });
        }

        protected virtual void AddDefinitions(RundownWiseDefinition<T> definitions)
        {
            if (definitions == null || definitions.RundownID == INVALID_RUNDOWN_ID) return;

            if (Definitions.ContainsKey(definitions.RundownID))
            {
                EOSLogger.Log($"Replaced RundownID: '{definitions.RundownID}' ({APPLY_TO_ALL_RUNDOWN_ID} means 'apply to all rundowns')");
            }

            Definitions[definitions.RundownID] = definitions;
        }

        public RundownWiseDefinition<T>? GetDefinition(int id)
        {
            return TryGetDefinition(id, out var def) ? def : null;
        }

        public bool TryGetDefinition(int ID, [MaybeNullWhen(false)] out RundownWiseDefinition<T> definition)
        {
            if (Definitions.ContainsKey(ID))
            {
                definition = Definitions[ID];
                return true;
            }
            definition = null;
            return false;
        }
    }
}
