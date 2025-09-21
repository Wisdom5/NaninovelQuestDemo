// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel.Commands
{
    /// <summary>
    /// Spawns particle system simulating [sun shafts](/guide/special-effects.html#sun-shafts) aka god rays.
    /// </summary>
    [CommandAlias("sun")]
    public class SpawnSun : SpawnLocalizedEffect
    {
        /// <summary>
        /// The intensity of the rays (opacity), in 0.0 to 1.0 range; default is 0.85.
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

        protected override string Path => "SunShafts";
        protected override bool DestroyWhen => Assigned(Intensity) && Intensity == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Intensity),
            ToSpawnParam(FadeDuration)
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
