// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;
using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Handles script hot reload feature.
    /// </summary>
    public static class HotReloadService
    {
        private static ScriptsConfiguration configuration;
        private static IScriptPlayer player;
        private static IStateManager stateManager;
        private static string[] playedLineHashes;

        /// <summary>
        /// Performs hot-reload of the currently played script.
        /// </summary>
        public static async UniTask ReloadPlayedScriptAsync ()
        {
            if (player?.Playlist is null || player.Playlist.Count == 0 || !player.PlayedScript)
            {
                Engine.Err("Failed to perform hot reload: script player is not available or no script is currently played.");
                return;
            }

            var lastPlayedLineIndex = (player.PlayedCommand ?? player.Playlist.Last()).PlaybackSpot.LineIndex;

            // Find the first modified line in the updated script (before the played line).
            var rollbackIndex = -1;
            for (int i = 0; i < lastPlayedLineIndex; i++)
            {
                if (!player.PlayedScript.Lines.IsIndexValid(i)) // The updated script ends before the currently played line.
                {
                    rollbackIndex = player.Playlist.GetCommandBeforeLine(i - 1, 0)?.PlaybackSpot.LineIndex ?? 0;
                    break;
                }

                if (playedLineHashes?.IsIndexValid(i) ?? false)
                {
                    var oldLineHash = playedLineHashes[i];
                    var newLine = player.PlayedScript.Lines[i];
                    if (oldLineHash.EqualsFast(newLine.LineHash)) continue;
                }

                rollbackIndex = player.Playlist.GetCommandBeforeLine(i, 0)?.PlaybackSpot.LineIndex ?? 0;
                break;
            }

            if (rollbackIndex > -1) // Script has changed before the played line.
            {
                // Resetting will make playlist to update (re-load) on play.
                player.ResetService();
                // Rollback to the line before the first modified one.
                await stateManager.RollbackAsync(s => s.PlaybackSpot.LineIndex == rollbackIndex);
                UpdateLineHashes(player.PlayedScript);
            }
            else // Script has changed after the played line.
            {
                // Update the playlist and continue playing from the last played line.
                var playlist = new ScriptPlaylist(player.PlayedScript);
                var playlistIndex = player.Playlist.FindIndex(c => c.PlaybackSpot.LineIndex == lastPlayedLineIndex);
                if (playlistIndex < 0) playlistIndex = 0;
                await playlist.PreloadResourcesAsync(playlistIndex, playlist.Count - 1);
                typeof(ScriptPlayer).GetProperty(nameof(ScriptPlayer.Playlist))?.SetValue(player, playlist, null);
                player.Play(playlistIndex);
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize ()
        {
            ScriptFileWatcher.OnModified += HandleScriptModifiedAsync;
            Engine.OnInitializationFinished += HandleEngineInitialized;

            void HandleEngineInitialized ()
            {
                if (!(Engine.Behaviour is RuntimeBehaviour)) return;

                if (configuration is null)
                    configuration = ProjectConfigurationProvider.LoadOrDefault<ScriptsConfiguration>();

                player = Engine.GetService<IScriptPlayer>();
                stateManager = Engine.GetService<IStateManager>();
                player.OnPlay += UpdateLineHashes;
            }
        }

        private static void UpdateLineHashes (Script script)
        {
            playedLineHashes = script.Lines.Select(l => l.LineHash).ToArray();
        }

        private static async void HandleScriptModifiedAsync (string assetPath)
        {
            if (!Engine.Initialized || !(Engine.Behaviour is RuntimeBehaviour) || !configuration.HotReloadScripts ||
                !ObjectUtils.IsValid(player.PlayedScript) || player.Playlist?.Count == 0) return;

            var scriptAsset = AssetDatabase.LoadAssetAtPath<Script>(assetPath);
            if (!ObjectUtils.IsValid(scriptAsset)) return;

            if (player.PlayedScript.Name != scriptAsset.Name) return;

            await ReloadPlayedScriptAsync();
        }
    }
}
