// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// When <see cref="ScriptsConfiguration.StableIdentification"/> enabled, stores
    /// largest text ID (revision) ever generated for each script to prevent collisions.
    /// </summary>
    [Serializable]
    public class ScriptRevisions : ScriptableObject
    {
        [Serializable]
        private class RevisionsMap : SerializableMap<string, int> { }

        [SerializeField] private RevisionsMap map = new RevisionsMap();

        /// <summary>
        /// Loads an existing asset from package data folder or creates a new default instance.
        /// </summary>
        public static ScriptRevisions LoadOrDefault ()
        {
            var fullPath = PathUtils.Combine(PackagePath.GeneratedDataPath, $"{nameof(ScriptRevisions)}.asset");
            var assetPath = PathUtils.AbsoluteToAssetPath(fullPath);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptRevisions>(assetPath);
            if (!obj)
            {
                if (File.Exists(fullPath)) throw new UnityException("Unity failed to load an existing asset. Try restarting the editor.");
                obj = CreateInstance<ScriptRevisions>();
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            return obj;
        }

        /// <summary>
        /// Returns last set revision for script with specified name or null when not not found.
        /// </summary>
        public int GetRevision (string scriptName)
        {
            return map.TryGetValue(scriptName, out var rev) ? rev : 0;
        }

        /// <summary>
        /// Sets revision for script with specified name.
        /// </summary>
        public void SetRevision (string scriptName, int revision)
        {
            map[scriptName] = revision;
        }

        /// <summary>
        /// Serializes the asset.
        /// </summary>
        public void SaveAsset ()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
