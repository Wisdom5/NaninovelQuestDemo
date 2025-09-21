// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Applied to command parameters representing color values in hex format.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ColorContextAttribute : ParameterContextAttribute
    {
        public ColorContextAttribute (int index = -1, string paramId = null)
            : base(ValueContextType.Color, "", index, paramId) { }
    }
}
