// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel.Commands
{
    /// <summary>
    /// Spawns particle system simulating [rain](/guide/special-effects.html#rain).
    /// </summary>
    [CommandAlias("rain")]
    public class SpawnRain : SpawnLocalizedEffect
    {
        /// <summary>
        /// The intensity of the rain (particles spawn rate per second); defaults to 500.
        /// Set to 0 to disable (de-spawn) the effect.
        /// </summary>
        [ParameterAlias("power")]
        public DecimalParameter Intensity;
        /// <summary>
        /// The particle system will gradually grow the spawn rate
        /// to the target level over the specified time, in seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        /// <summary>
        /// Multiplier to the horizontal speed of the particles.
        /// Use to change angle of the rain drops.
        /// </summary>
        [ParameterAlias("xSpeed")]
        public DecimalParameter XVelocity;
        /// <summary>
        /// Multiplier to the vertical speed of the particles.
        /// </summary>
        [ParameterAlias("ySpeed")]
        public DecimalParameter YVelocity;

        protected override string Path => "Rain";
        protected override bool DestroyWhen => Assigned(Intensity) && Intensity == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Intensity),
            ToSpawnParam(FadeDuration),
            ToSpawnParam(XVelocity),
            ToSpawnParam(YVelocity)
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
