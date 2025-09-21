// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;
using Naninovel.Metadata;
using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Provides script labels IDE metadata for autocompletion.
    /// </summary>
    public class LabelMetadataProvider : IMetadataProvider
    {
        public Project GetMetadata (MetadataOptions options)
        {
            if (!ShouldGenerate()) return new Project();
            options.NotifyProgress("Processing script labels...", 0);
            var scripts = LoadScripts();
            options.NotifyProgress("Processing script labels...", .75f);
            var constants = scripts.Select(CreateLabelConstant);
            return new Project { Constants = constants.ToArray() };
        }

        private bool ShouldGenerate ()
        {
            var config = ProjectConfigurationProvider.LoadOrDefault<EngineConfiguration>();
            return config.GenerateLabelMetadata;
        }

        private Script[] LoadScripts ()
        {
            return EditorResources.LoadOrDefault()
                .GetAllRecords(ScriptsConfiguration.DefaultPathPrefix)
                .Select(kv => AssetDatabase.GUIDToAssetPath(kv.Value))
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(AssetDatabase.LoadAssetAtPath<Script>)
                .Where(ObjectUtils.IsValid)
                .ToArray();
        }

        private Constant CreateLabelConstant (Script script)
        {
            var name = $"Labels/{script.Name}";
            var values = script.Lines
                .OfType<LabelScriptLine>()
                .Where(l => !string.IsNullOrWhiteSpace(l.LabelText))
                .Select(l => l.LabelText.Trim());
            return new Constant { Name = name, Values = values.ToArray() };
        }
    }
}
