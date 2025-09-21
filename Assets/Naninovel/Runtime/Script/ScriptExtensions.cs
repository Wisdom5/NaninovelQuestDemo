// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IScriptManager"/> and associated types.
    /// </summary>
    public static class ScriptExtensions
    {
        /// <summary>
        /// Attempts to get script with specified name (local resource path); returns true when successful. 
        /// </summary>
        public static bool TryGetScript (this IScriptManager manager, string scriptName, out Script script)
        {
            return script = manager.HasScript(scriptName) ? manager.GetScript(scriptName) : null;
        }
    }
}
