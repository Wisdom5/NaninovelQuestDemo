// Copyright 2023 ReWaffle LLC. All rights reserved.

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [ScriptedImporter(version: 40, ext: "nani")]
    public class ScriptImporter : ScriptedImporter
    {
        private static ScriptRevisions revisions;
        private static ScriptsConfiguration config;

        private readonly ScriptTextIdentifier identifier = new ScriptTextIdentifier();
        private readonly ScriptAssetSerializer serializer = new ScriptAssetSerializer();

        public override void OnImportAsset (AssetImportContext context)
        {
            try
            {
                var bytes = File.ReadAllBytes(context.assetPath);
                var contents = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                PurgeBom(contents, context.assetPath);

                var name = Path.GetFileNameWithoutExtension(context.assetPath);
                var script = Script.FromText(name, contents, context.assetPath);
                IdentifyText(script, contents, context.assetPath);

                script.hideFlags = HideFlags.NotEditable;
                context.AddObjectToAsset("naniscript", script);
                context.SetMainObject(script);
            }
            catch (Exception e)
            {
                context.LogImportError($"Failed to import naniscript: {e}");
            }
        }

        // Unity auto adding BOM when creating script assets: https://git.io/fjVgY
        private void PurgeBom (string contents, string filePath)
        {
            if (contents.Length > 0 && contents[0] == '\uFEFF')
                File.WriteAllText(filePath, contents.Substring(1));
        }

        private void IdentifyText (Script script, string contents, string filePath)
        {
            // Unity 2022 is not allowing asset creation during import.
            if (!config || !revisions)
            {
                EditorApplication.delayCall += () => {
                    config = ProjectConfigurationProvider.LoadOrDefault<ScriptsConfiguration>();
                    revisions = ScriptRevisions.LoadOrDefault();
                    AssetDatabase.ImportAsset(filePath);
                };
                return;
            }

            if (!config.StableIdentification) return;
            var options = new ScriptTextIdentifier.Options(revisions.GetRevision(script.Name), filePath);
            var result = identifier.Identify(script, options);
            if (result.ModifiedLines.Count == 0) return;
            revisions.SetRevision(script.Name, result.Revision);
            // Unity 2022 is not allowing asset saving during import.
            EditorApplication.delayCall += () => ScriptRevisions.LoadOrDefault().SaveAsset();

            var lines = Parsing.ScriptParser.SplitText(contents);
            foreach (var modifiedLineIndex in result.ModifiedLines)
                if (lines.IsIndexValid(modifiedLineIndex) && script.Lines.IsIndexValid(modifiedLineIndex))
                    lines[modifiedLineIndex] = serializer.Serialize(script.Lines[modifiedLineIndex], script.TextMap);
                else Engine.Warn($"{EditorUtils.BuildAssetLink(script, modifiedLineIndex + 1)} failed to identify script text: incorrect line index.");

            var time = File.GetLastWriteTime(filePath);
            File.WriteAllText(filePath, string.Join("\n", lines));
            File.SetLastWriteTime(filePath, time); // Otherwise Unity 2022 warns about file change during import.
        }
    }
}
