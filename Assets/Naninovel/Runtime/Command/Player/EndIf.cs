// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Closes an [@if] conditional execution block.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts#conditional-execution) guide.
    /// </summary>
    public class EndIf : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default) => UniTask.CompletedTask;
    }
}
