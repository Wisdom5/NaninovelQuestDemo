// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate resources with a specific path prefix.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ResourceContextAttribute : ParameterContextAttribute
    {
        /// <param name="pathPrefix">Resource path prefix to associate with the parameter.</param>
        public ResourceContextAttribute (string pathPrefix, int index = -1, string paramId = null)
            : base(ValueContextType.Resource, pathPrefix, index, paramId) { }
    }
}
