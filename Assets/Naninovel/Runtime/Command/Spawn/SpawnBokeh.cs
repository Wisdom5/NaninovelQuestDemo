// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel.Commands
{
    /// <summary>
    /// Simulates [depth of field](/guide/special-effects.html#depth-of-field-bokeh) (aka DOF, bokeh) effect,
    /// when only the object in focus stays sharp, while others are blurred.
    /// </summary>
    [CommandAlias("bokeh")]
    public class SpawnBokeh : SpawnEffect
    {
        /// <summary>
        /// Name of the game object to set focus for (optional). When set, the focus will always
        /// stay on the game object, while `dist` parameter will be ignored.
        /// </summary>
        [ParameterAlias("focus")]
        public StringParameter FocusObjectName;
        /// <summary>
        /// Distance (in units) from Naninovel camera to the focus point.
        /// Ignored when `focus` parameter is specified. Defaults to 10.
        /// </summary>
        [ParameterAlias("dist")]
        public DecimalParameter FocusDistance;
        /// <summary>
        /// Amount of blur to apply for the de-focused areas;
        /// also determines focus sensitivity. Defaults to 3.75.
        /// Set to 0 to disable (de-spawn) the effect.
        /// </summary>
        [ParameterAlias("power")]
        public DecimalParameter FocalLength;
        /// <summary>
        /// How long it will take the parameters to reach the target values, in seconds.
        /// Defaults to 1.0.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;

        protected override string Path => "DepthOfField";
        protected override bool DestroyWhen => Assigned(FocalLength) && FocalLength == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(FocusObjectName),
            ToSpawnParam(FocusDistance),
            ToSpawnParam(FocalLength),
            ToSpawnParam(Duration),
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(Duration)
        };
    }
}
