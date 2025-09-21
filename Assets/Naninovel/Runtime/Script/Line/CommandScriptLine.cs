// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing a single <see cref="Command"/>.
    /// </summary>
    [Serializable]
    public class CommandScriptLine : ScriptLine
    {
        /// <summary>
        /// The command which this line represents.
        /// </summary>
        public Command Command => command;

        [SerializeReference] private Command command;

        public CommandScriptLine (Command command, int lineIndex, string lineHash)
            : base(lineIndex, lineHash)
        {
            this.command = command;
        }
    }
}
