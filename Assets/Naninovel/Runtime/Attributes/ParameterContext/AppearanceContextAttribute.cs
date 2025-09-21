// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter to associate appearance records. Command should contains a parameter with <see cref="ActorContextAttribute"/>.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AppearanceContextAttribute : ParameterContextAttribute
    {
        /// <param name="actorId">When value of actor context parameter is not found, will use this default one.</param>
        public AppearanceContextAttribute (int index = -1, string paramId = null, string actorId = null)
            : base(ValueContextType.Appearance, actorId, index, paramId) { }
    }
}
