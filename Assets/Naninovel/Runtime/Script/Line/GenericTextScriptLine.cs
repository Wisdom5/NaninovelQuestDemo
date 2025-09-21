// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing text to print.
    /// </summary>
    [Serializable]
    public class GenericTextScriptLine : ScriptLine
    {
        /// <summary>
        /// A list of <see cref="Command"/> contained by this line.
        /// </summary>
        public IReadOnlyList<Command> InlinedCommands => inlinedCommands;

        [SerializeReference] private List<Command> inlinedCommands;

        public GenericTextScriptLine (IEnumerable<Command> inlinedCommands, int lineIndex, string lineHash)
            : base(lineIndex, lineHash)
        {
            this.inlinedCommands = inlinedCommands.ToList();
        }
    }
}
