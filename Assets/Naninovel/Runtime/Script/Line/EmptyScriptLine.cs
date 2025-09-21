// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    [Serializable]
    public class EmptyScriptLine : ScriptLine
    {
        public EmptyScriptLine (int lineIndex)
            : base(lineIndex, string.Empty) { }
    }
}
