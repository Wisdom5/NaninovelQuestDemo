// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores engine version and build number.
    /// </summary>
    public class EngineVersion : ScriptableObject
    {
        /// <summary>
        /// Version identifier of the engine release.
        /// </summary>
        public string Version => engineVersion;
        /// <summary>
        /// Version identifier of the patch/hotfix release.
        /// </summary>
        public string Patch => patchVersion;
        /// <summary>
        /// Date and time the release was built.
        /// </summary>
        public string Build => buildDate;

        [SerializeField] private string engineVersion = string.Empty;
        [SerializeField] private string patchVersion = "0";
        [SerializeField, ReadOnly] private string buildDate = string.Empty;

        public static EngineVersion LoadFromResources ()
        {
            const string assetPath = nameof(EngineVersion);
            return Engine.LoadInternalResource<EngineVersion>(assetPath);
        }
    }
}
