// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Applied to <see cref="NamedStringParameter"/> to associate it with a navigation endpoint
    /// (script name and label, eg path parameter of goto command).
    /// Used by bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class EndpointContextAttribute : ConstantContextAttribute
    {
        public EndpointContextAttribute (string paramId = null) : base("", -1, paramId) { }
    }
}
