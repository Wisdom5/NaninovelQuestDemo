// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing a text marker used to navigate within the script.
    /// </summary>
    [System.Serializable]
    public class LabelScriptLine : ScriptLine
    {
        /// <summary>
        /// Text contents of the label.
        /// </summary>
        public string LabelText => labelText;

        [SerializeField] private string labelText;

        public LabelScriptLine (string labelText, int lineIndex, string lineHash)
            : base(lineIndex, lineHash)
        {
            this.labelText = labelText;
        }
    }
}
