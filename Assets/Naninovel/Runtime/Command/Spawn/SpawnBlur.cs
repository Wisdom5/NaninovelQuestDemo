// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel.Commands
{
    /// <summary>
    /// Applies [blur effect](/guide/special-effects.html#blur) to supported actor:
    /// backgrounds and characters of sprite, layered, diced, Live2D, Spine, video and scene implementations.
    /// </summary>
    /// <remarks>
    /// The actor should have `IBlurable` interface implemented in order to support the effect.
    /// </remarks>
    [CommandAlias("blur")]
    public class SpawnBlur : SpawnEffect
    {
        /// <summary>
        /// ID of the actor to apply the effect for; in case multiple actors with the same ID found
        /// (eg, a character and a printer), will affect only the first found one.
        /// When not specified, applies to the main background.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ActorContext, ParameterDefaultValue(BackgroundsConfiguration.MainActorId)]
        public StringParameter ActorId = BackgroundsConfiguration.MainActorId;
        /// <summary>
        /// Intensity of the effect, in 0.0 to 1.0 range. Defaults to 0.5.
        /// Set to 0 to disable (de-spawn) the effect.
        /// </summary>
        [ParameterAlias("power")]
        public DecimalParameter Intensity;
        /// <summary>
        /// How long it will take the parameters to reach the target values, in seconds.
        /// Defaults to 1.0.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;

        protected override string Path => $"Blur#{ActorId}";
        protected override bool DestroyWhen => Assigned(Intensity) && Intensity == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(ActorId),
            ToSpawnParam(Intensity),
            ToSpawnParam(FadeDuration)
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
