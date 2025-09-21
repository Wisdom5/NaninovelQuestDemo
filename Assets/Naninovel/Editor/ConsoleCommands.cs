// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel
{
    /// <summary>
    /// Provides implementations of the built-in debug console commands (editor-only).
    /// </summary>
    public static class ConsoleCommands
    {
        [ConsoleCommand]
        public static async void Reload ()
        {
            await HotReloadService.ReloadPlayedScriptAsync();
        }
    }
}
