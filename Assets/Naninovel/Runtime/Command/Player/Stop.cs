// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Stops the naninovel script execution.
    /// </summary>
    public class Stop : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Engine.GetService<IScriptPlayer>().Stop();

            return UniTask.CompletedTask;
        }
    } 
}
