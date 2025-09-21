// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to create <see cref="Script"/> asset from text string.
    /// </summary>
    public interface IScriptParser
    {
        /// <summary>
        /// Creates a new script instance by parsing the provided script text.
        /// </summary>
        /// <param name="scriptName">Name of the script asset.</param>
        /// <param name="scriptText">The script text to parse.</param>
        /// <param name="options">Optional configuration of the parse behaviour.</param>
        Script ParseText (string scriptName, string scriptText, ParseOptions options = default);
    }
}
