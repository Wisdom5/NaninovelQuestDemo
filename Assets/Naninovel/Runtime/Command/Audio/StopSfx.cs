// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing an SFX (sound effect) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When sound effect track name (SfxPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    public class StopSfx : AudioCommand
    {
        /// <summary>
        /// Path to the sound effect to stop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter SfxPath;
        /// <summary>
        /// Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).
        /// </summary>
        [ParameterAlias("fade"), ParameterDefaultValue("0.35")]
        public DecimalParameter FadeOutDuration;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var duration = Assigned(FadeOutDuration) ? FadeOutDuration.Value : AudioManager.Configuration.DefaultFadeDuration;
            if (Assigned(SfxPath)) await AudioManager.StopSfxAsync(SfxPath, duration, asyncToken);
            else await AudioManager.StopAllSfxAsync(duration, asyncToken);
        }
    }
}
