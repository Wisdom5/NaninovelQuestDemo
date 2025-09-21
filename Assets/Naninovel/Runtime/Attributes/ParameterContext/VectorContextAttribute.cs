// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Applied to command parameters representing fixed-length array of named components (eg, xyz components of position).
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class VectorContextAttribute : ParameterContextAttribute
    {
        /// <param name="components">Components of the array, seperated by comma, eg "X,Y,Z".</param>
        public VectorContextAttribute (string components, int index = -1, string paramId = null)
            : base(ValueContextType.Vector, components, index, paramId) { }
    }
}
