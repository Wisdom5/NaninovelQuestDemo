// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a single line in a <see cref="Script"/>.
    /// </summary>
    [Serializable]
    public abstract class ScriptLine
    {
        /// <summary>
        /// Index of the line in naninovel script.
        /// </summary>
        public int LineIndex => lineIndex;
        /// <summary>
        /// Number of the line in naninovel script (index + 1).
        /// </summary>
        public int LineNumber => LineIndex + 1;
        /// <summary>
        /// Persistent hash code of the original text line.
        /// </summary>
        public string LineHash => lineHash;

        [SerializeField] private int lineIndex;
        [SerializeField] private string lineHash;

        protected ScriptLine (int lineIndex, string lineHash)
        {
            this.lineIndex = lineIndex;
            this.lineHash = lineHash;
        }
    }
}
