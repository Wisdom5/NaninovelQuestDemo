// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Implementation is able to gradually reveal text.
    /// </summary>
    public interface IRevealableText
    {
        /// <summary>
        /// Full text to be revealed.
        /// </summary>
        string Text { get; set; }
        /// <summary>
        /// Base text color.
        /// </summary>
        Color TextColor { get; set; }
        /// <summary>
        /// Progress (in 0.0 to 1.0 range) of the <see cref="Text"/> reveal process.
        /// </summary>
        float RevealProgress { get; set; }

        /// <summary>
        /// Reveals next (relative to the current <see cref="RevealProgress"/>)
        /// <paramref name="count"/> <see cref="Text"/> characters (formatting tags don't count)
        /// over <paramref name="duration"/>, in seconds.
        /// </summary>
        /// <param name="count">Number of characters to reveal; expected to reveal all chars simultaneously.</param>
        /// <param name="duration">Duration of the reveal process, in seconds.</param>
        /// <param name="token">The reveal process should be canceled or completed ASAP when requested.</param>
        void RevealNextChars (int count, float duration, AsyncToken token);
        /// <summary>
        /// Returns position (in world space) of the last revealed <see cref="Text"/> character.
        /// Bottom-right point of the character is expected (bottom-left when RTL).
        /// </summary>
        Vector2 GetLastRevealedCharPosition ();
        /// <summary>
        /// Returns last revealed visible (excluding formatting tags) <see cref="Text"/> character.
        /// </summary>
        char GetLastRevealedChar ();
    }
}
