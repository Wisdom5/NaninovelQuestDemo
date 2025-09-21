// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playback of the currently played voice clip.
    /// </summary>
    public class StopVoice : AudioCommand
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            AudioManager.StopVoice();
            return UniTask.CompletedTask;
        }
    } 
}
