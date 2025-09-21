// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Applies [shake effect](/guide/special-effects.html#shake)
    /// for the actor with the specified ID or main camera.
    /// </summary>
    [CommandAlias("shake")]
    public class SpawnShake : SpawnEffect
    {
        /// <summary>
        /// ID of the actor to shake. In case multiple actors with the same ID found
        /// (eg, a character and a printer), will affect only the first found one.
        /// To shake main camera, use "Camera" keyword.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ActorContext]
        public StringParameter ActorId;
        /// <summary>
        /// The number of shake iterations. When set to 0, will loop until stopped with -1.
        /// </summary>
        [ParameterAlias("count")]
        public IntegerParameter ShakeCount;
        /// <summary>
        /// The base duration of each shake iteration, in seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter ShakeDuration;
        /// <summary>
        /// The randomizer modifier applied to the base duration of the effect.
        /// </summary>
        [ParameterAlias("deltaTime")]
        public DecimalParameter DurationVariation;
        /// <summary>
        /// The base displacement amplitude of each shake iteration, in units.
        /// </summary>
        [ParameterAlias("power")]
        public DecimalParameter ShakeAmplitude;
        /// <summary>
        /// The randomized modifier applied to the base displacement amplitude.
        /// </summary>
        [ParameterAlias("deltaPower")]
        public DecimalParameter AmplitudeVariation;
        /// <summary>
        /// Whether to displace the actor horizontally (by x-axis).
        /// </summary>
        [ParameterAlias("hor")]
        public BooleanParameter ShakeHorizontally;
        /// <summary>
        /// Whether to displace the actor vertically (by y-axis).
        /// </summary>
        [ParameterAlias("ver")]
        public BooleanParameter ShakeVertically;

        protected override string Path => ResolvePath();
        protected override bool DestroyWhen => Assigned(ShakeCount) && ShakeCount == -1;

        private const string cameraId = "Camera";

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(ActorId),
            ToSpawnParam(ShakeCount),
            ToSpawnParam(ShakeDuration),
            ToSpawnParam(DurationVariation),
            ToSpawnParam(ShakeAmplitude),
            ToSpawnParam(AmplitudeVariation),
            ToSpawnParam(ShakeHorizontally),
            ToSpawnParam(ShakeVertically)
        };

        protected virtual string ResolvePath ()
        {
            if (ActorId == cameraId) return "ShakeCamera";
            var manager = Engine.FindAllServices<IActorManager>(c => c.ActorExists(ActorId)).FirstOrDefault();
            if (manager is ICharacterManager) return $"ShakeCharacter#{ActorId}";
            if (manager is IBackgroundManager) return $"ShakeBackground#{ActorId}";
            return $"ShakePrinter#{ActorId}";
            // Can't throw here, as the actor may not be available (eg, pre-loading with dynamic policy).
        }
    }
}
