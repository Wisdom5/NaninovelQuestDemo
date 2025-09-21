// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing a BGM (background music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When music track name (BgmPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    public class StopBgm : AudioCommand
    {
        /// <summary>
        /// Path to the music track to stop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter BgmPath;
        /// <summary>
        /// Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).
        /// </summary>
        [ParameterAlias("fade"), ParameterDefaultValue("0.35")]
        public DecimalParameter FadeOutDuration;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var duration = Assigned(FadeOutDuration) ? FadeOutDuration.Value : AudioManager.Configuration.DefaultFadeDuration;
            if (Assigned(BgmPath)) await AudioManager.StopBgmAsync(BgmPath, duration, asyncToken);
            else await AudioManager.StopAllBgmAsync(duration, asyncToken);
        }
    } 
}
