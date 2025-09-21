// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="Script"/> assets.
    /// </summary>
    public interface IScriptManager : IEngineService<ScriptsConfiguration>
    {
        /// <summary>
        /// Available scenario scripts.
        /// </summary>
        /// <remarks>
        /// All scripts are expected to be loaded on service initialization.
        /// </remarks>
        IReadOnlyCollection<Script> Scripts { get; }
        /// <summary>
        /// Available external scenario scripts (community modding feature).
        /// </summary>
        /// <remarks>
        /// All external scripts are expected to be loaded on service initialization
        /// when <see cref="ScriptsConfiguration.EnableCommunityModding"/> is enabled.
        /// </remarks>
        IReadOnlyCollection<Script> ExternalScripts { get; }
        /// <summary>
        /// Total number of commands existing in all the available scenario scripts.
        /// Only valid when <see cref="ScriptsConfiguration.CountTotalCommands"/> is enabled.
        /// </summary>
        int TotalCommandsCount { get; }

        /// <summary>
        /// Checks whether script with the specified name (local resource path) is available.
        /// </summary>
        bool HasScript (string scriptName);
        /// <summary>
        /// Returns script with specified name (local resource path); throws when not found.
        /// </summary>
        Script GetScript (string scriptName);
    }
}
