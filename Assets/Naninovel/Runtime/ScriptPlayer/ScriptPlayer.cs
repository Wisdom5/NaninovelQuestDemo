// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptPlayer"/>
    [InitializeAtRuntime]
    public class ScriptPlayer : IStatefulService<SettingsStateMap>, IStatefulService<GlobalStateMap>, IStatefulService<GameStateMap>, IScriptPlayer
    {
        [Serializable]
        public class Settings
        {
            public PlayerSkipMode SkipMode;
        }

        [Serializable]
        public class GlobalState
        {
            public PlayedScriptRegister PlayedScriptRegister = new PlayedScriptRegister();
        }

        [Serializable]
        public class GameState
        {
            public bool Playing;
            public bool ExecutedPlayedCommand;
            public bool WaitingForInput;
            public List<PlaybackSpot> GosubReturnSpots;
        }

        public event Action<Script> OnPlay;
        public event Action<Script> OnStop;
        public event Action<Command> OnCommandExecutionStart;
        public event Action<Command> OnCommandExecutionFinish;
        public event Action<bool> OnSkip;
        public event Action<bool> OnAutoPlay;
        public event Action<bool> OnWaitingForInput;
        public event Action<float> OnPreloadProgress;

        public virtual ScriptPlayerConfiguration Configuration { get; }
        public virtual bool Playing => playRoutineCTS != null;
        public virtual bool Synchronizing => synchronizeTCS != null;
        public virtual bool SkipActive { get; private set; }
        public virtual bool AutoPlayActive { get; private set; }
        public virtual bool WaitingForInput { get; private set; }
        public virtual PlayerSkipMode SkipMode { get; set; }
        public virtual Script PlayedScript { get; private set; }
        public virtual Command PlayedCommand => Playlist?.GetCommandByIndex(PlayedIndex);
        public virtual PlaybackSpot PlaybackSpot => PlayedCommand?.PlaybackSpot ?? default;
        public virtual ScriptPlaylist Playlist { get; private set; }
        public virtual int PlayedIndex { get; private set; }
        public virtual Stack<PlaybackSpot> GosubReturnSpots { get; private set; }
        public virtual int PlayedCommandsCount => playedRegister.CountPlayed();

        private readonly ResourceProviderConfiguration providerConfig;
        private readonly List<Func<Command, UniTask>> preExecutionTasks = new List<Func<Command, UniTask>>();
        private readonly List<Func<Command, UniTask>> postExecutionTasks = new List<Func<Command, UniTask>>();
        private readonly Queue<Func<UniTask>> onSynchronizeTasks = new Queue<Func<UniTask>>();
        private readonly IInputManager inputManager;
        private readonly IScriptManager scriptManager;
        private readonly IStateManager stateManager;
        private int executedCommandsCount;
        private bool executedPlayedCommand;
        private bool shouldCompleteNextCommand;
        private PlayedScriptRegister playedRegister;
        private CancellationTokenSource playRoutineCTS;
        private CancellationTokenSource commandExecutionCTS;
        private CancellationTokenSource synchronizationCTS;
        private UniTaskCompletionSource waitForWaitForInputDisabledTCS;
        private UniTaskCompletionSource synchronizeTCS;
        private IInputSampler continueInput, skipInput, toggleSkipInput, autoPlayInput;

        public ScriptPlayer (ScriptPlayerConfiguration config, ResourceProviderConfiguration providerConfig, 
            IInputManager inputManager, IScriptManager scriptManager, IStateManager stateManager)
        {
            Configuration = config;
            this.providerConfig = providerConfig;
            this.inputManager = inputManager;
            this.scriptManager = scriptManager;
            this.stateManager = stateManager;

            GosubReturnSpots = new Stack<PlaybackSpot>();
            playedRegister = new PlayedScriptRegister();
            commandExecutionCTS = new CancellationTokenSource();
            synchronizationCTS = new CancellationTokenSource();
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            continueInput = inputManager.GetContinue();
            skipInput = inputManager.GetSkip();
            toggleSkipInput = inputManager.GetToggleSkip();
            autoPlayInput = inputManager.GetAutoPlay();

            if (continueInput != null)
            {
                continueInput.OnStart += DisableWaitingForInput;
                continueInput.OnStart += DisableSkip;
            }
            if (skipInput != null)
            {
                skipInput.OnStart += EnableSkip;
                skipInput.OnEnd += DisableSkip;
            }
            if (toggleSkipInput != null)
                toggleSkipInput.OnStart += ToggleSkip;
            if (autoPlayInput != null)
                autoPlayInput.OnStart += ToggleAutoPlay;

            if (Configuration.ShowDebugOnInit)
                UI.DebugInfoGUI.Toggle();

            return UniTask.CompletedTask;
        }

        public virtual void ResetService ()
        {
            Stop();
            CancelCommands();
            // Playlist?.ReleaseResources(); performed in StateManager; 
            // here it could be invoked after the actors are already destroyed.
            Playlist = null;
            PlayedIndex = -1;
            PlayedScript = null;
            executedPlayedCommand = false;
            shouldCompleteNextCommand = false;
            DisableWaitingForInput();
            DisableAutoPlay();
            DisableSkip();
        }

        public virtual void DestroyService ()
        {
            ResetService();

            commandExecutionCTS?.Dispose();
            synchronizationCTS?.Dispose();

            if (continueInput != null)
            {
                continueInput.OnStart -= DisableWaitingForInput;
                continueInput.OnStart -= DisableSkip;
            }
            if (skipInput != null)
            {
                skipInput.OnStart -= EnableSkip;
                skipInput.OnEnd -= DisableSkip;
            }
            if (toggleSkipInput != null)
                toggleSkipInput.OnStart -= ToggleSkip;
            if (autoPlayInput != null)
                autoPlayInput.OnStart -= ToggleAutoPlay;
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                SkipMode = SkipMode
            };
            stateMap.SetState(settings);
        }

        public virtual UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings {
                SkipMode = Configuration.DefaultSkipMode 
            };
            SkipMode = settings.SkipMode;
            return UniTask.CompletedTask;
        }

        public virtual void SaveServiceState (GlobalStateMap stateMap)
        {
            var globalState = new GlobalState {
                PlayedScriptRegister = playedRegister
            };
            stateMap.SetState(globalState);
        }

        public virtual UniTask LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            var state = stateMap.GetState<GlobalState>() ?? new GlobalState();
            playedRegister = state.PlayedScriptRegister;
            return UniTask.CompletedTask;
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var gameState = new GameState {
                Playing = Playing,
                ExecutedPlayedCommand = executedPlayedCommand,
                WaitingForInput = WaitingForInput,
                GosubReturnSpots = GosubReturnSpots.Count > 0 ? GosubReturnSpots.Reverse().ToList() : null // Stack is reversed on enum.
            };
            stateMap.PlaybackSpot = PlaybackSpot;
            stateMap.SetState(gameState);
        }

        public virtual async UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null) { Playlist?.ReleaseResources(); ResetService(); return; }

            // Force stop and cancel all running commands to prevent state mutation while loading other services.
            Stop(); CancelCommands();

            executedPlayedCommand = state.ExecutedPlayedCommand;

            if (state.Playing) // The playback is resumed (when necessary) after other services are loaded.
            {
                if (stateManager.RollbackInProgress) stateManager.OnRollbackFinished += PlayAfterRollback;
                else stateManager.OnGameLoadFinished += PlayAfterLoad;
            }

            if (state.GosubReturnSpots != null && state.GosubReturnSpots.Count > 0)
                GosubReturnSpots = new Stack<PlaybackSpot>(state.GosubReturnSpots);
            else GosubReturnSpots.Clear();

            if (string.IsNullOrEmpty(stateMap.PlaybackSpot.ScriptName)) LoadStoppedState();
            else await LoadPlayingStateAsync(stateMap.PlaybackSpot);

            void LoadStoppedState ()
            {
                Playlist?.ReleaseResources();
                Playlist = null;
                PlayedScript = null;
                PlayedIndex = 0;
            }

            async UniTask LoadPlayingStateAsync (PlaybackSpot spot)
            {
                if (Playlist == null || !PlayedScript || !stateMap.PlaybackSpot.ScriptName.EqualsFast(PlayedScript.Name))
                {
                    Playlist?.ReleaseResources();
                    PlayedScript = scriptManager.GetScript(stateMap.PlaybackSpot.ScriptName);
                    Playlist = new ScriptPlaylist(PlayedScript);
                }
                PlayedIndex = FindPlayableIndex(stateMap.PlaybackSpot);
                var endIndex = providerConfig.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                    Mathf.Min(PlayedIndex + providerConfig.DynamicPolicySteps, Playlist.Count - 1);
                await Playlist.PreloadResourcesAsync(PlayedIndex, endIndex, OnPreloadProgress.SafeInvoke);
            }

            void PlayAfterRollback ()
            {
                stateManager.OnRollbackFinished -= PlayAfterRollback;
                SetWaitingForInputEnabled(state.WaitingForInput);
                // Rollback snapshots are pushed before the currently played command is executed, so play it again.
                shouldCompleteNextCommand = true;
                Play();
            }

            void PlayAfterLoad (GameSaveLoadArgs _)
            {
                stateManager.OnGameLoadFinished -= PlayAfterLoad;
                SetWaitingForInputEnabled(state.WaitingForInput);
                // Game could be saved before or after the currently played command is executed.
                if (executedPlayedCommand)
                {
                    if (SelectNextCommand()) Play();
                }
                else Play();
            }
        }

        public virtual void AddPreExecutionTask (Func<Command, UniTask> task) => preExecutionTasks.Insert(0, task);

        public virtual void RemovePreExecutionTask (Func<Command, UniTask> task) => preExecutionTasks.Remove(task);

        public virtual void AddPostExecutionTask (Func<Command, UniTask> task) => postExecutionTasks.Insert(0, task);

        public virtual void RemovePostExecutionTask (Func<Command, UniTask> task) => postExecutionTasks.Remove(task);

        public virtual async UniTask PreloadAndPlayAsync (Script script, int startLineIndex = 0, int startInlineIndex = 0)
        {
            PlayedScript = script;
            await PreloadAndSetupPlaylistAsync();
            Play(ResolvePlaylistIndex());

            async UniTask PreloadAndSetupPlaylistAsync ()
            {
                var prevPlaylist = Playlist; // Release later to prevent re-loading resources used in both scripts.
                Playlist = new ScriptPlaylist(script);
                var startIndex = Playlist.GetIndexByLine(startLineIndex, startInlineIndex);
                if (startIndex < 0) throw new Error($"Failed to start playback: `{script.Name}` script can't be played at line #{startLineIndex}.{startInlineIndex}.");
                var endIndex = providerConfig.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                    Mathf.Min(startIndex + providerConfig.DynamicPolicySteps, Playlist.Count - 1);
                await Playlist.PreloadResourcesAsync(startIndex, endIndex, OnPreloadProgress.SafeInvoke);
                prevPlaylist?.ReleaseResources();
                if (Configuration.UnloadAssetsOnPlay)
                    await Resources.UnloadUnusedAssets();
            }

            int ResolvePlaylistIndex ()
            {
                if (startLineIndex > 0 || startInlineIndex > 0)
                {
                    var startCommand = Playlist.GetCommandAfterLine(startLineIndex, startInlineIndex);
                    if (startCommand is null) throw new Error($"Script player failed to start: no commands found in script `{PlayedScript.Name}` at line #{startLineIndex}.{startInlineIndex}.");
                    return Playlist.IndexOf(startCommand);
                }
                return 0;
            }
        }

        public virtual void Play (int? playlistIndex = null)
        {
            if (!PlayedScript || Playlist is null)
                throw new Error("Failed to start script playback: the script is not assigned.");

            if (Playing) Stop();

            if (playlistIndex.HasValue)
                PlayedIndex = playlistIndex.Value;

            if (Playlist.IsIndexValid(PlayedIndex) || SelectNextCommand())
            {
                playRoutineCTS = new CancellationTokenSource();
                var playRoutineCancellationToken = playRoutineCTS.Token;
                PlayRoutineAsync(playRoutineCancellationToken).Forget();
                if (!playRoutineCancellationToken.IsCancellationRequested)
                    OnPlay?.Invoke(PlayedScript);
            }
        }

        public virtual void Stop ()
        {
            playRoutineCTS?.Cancel();
            playRoutineCTS?.Dispose();
            playRoutineCTS = null;

            OnStop?.Invoke(PlayedScript);
        }

        public virtual async UniTask<bool> RewindAsync (int lineIndex)
        {
            if (PlayedCommand is null) throw new Error("Script player failed to rewind: played command is not valid.");

            var targetCommand = Playlist.GetCommandAfterLine(lineIndex, 0);
            if (targetCommand is null) throw new Error($"Script player failed to rewind: target line index ({lineIndex}) is not valid for `{PlayedScript.Name}` script.");

            var targetPlaylistIndex = Playlist.IndexOf(targetCommand);
            if (targetPlaylistIndex == PlayedIndex) return true;

            var wasWaitingInput = WaitingForInput;
            
            if (Playing) Stop();
            DisableAutoPlay();
            DisableSkip();
            DisableWaitingForInput();

            playRoutineCTS = new CancellationTokenSource();
            var cancellationToken = playRoutineCTS.Token;

            bool result;
            if (targetPlaylistIndex > PlayedIndex)
            {
                // In case were waiting input, the current command wasn't executed; execute it now.
                result = await FastForwardRoutineAsync(cancellationToken, targetPlaylistIndex, wasWaitingInput);
                Play();
            }
            else
            {
                var targetSpot = targetCommand.PlaybackSpot;
                result = await stateManager.RollbackAsync(s => s.PlaybackSpot == targetSpot);
            }

            return result;
        }

        public virtual void SetSkipEnabled (bool enable)
        {
            if (SkipActive == enable) return;
            if (enable && !GetSkipAllowed()) return;

            SkipActive = enable;
            Engine.Time.TimeScale = enable ? Configuration.SkipTimeScale : 1f;
            OnSkip?.Invoke(enable);

            if (enable && WaitingForInput)
            {
                stateManager.PeekRollbackStack()?.AllowPlayerRollback();
                SetWaitingForInputEnabled(false);
            }
            if (enable && AutoPlayActive) SetAutoPlayEnabled(false);
        }

        public virtual void SetAutoPlayEnabled (bool enable)
        {
            if (AutoPlayActive == enable) return;
            AutoPlayActive = enable;
            OnAutoPlay?.Invoke(enable);

            if (enable && WaitingForInput) SetWaitingForInputEnabled(false);
        }

        public virtual void SetWaitingForInputEnabled (bool enable)
        {
            if (WaitingForInput == enable) return;

            if (SkipActive && enable || (!enable && (continueInput.Active || AutoPlayActive)))
                stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            if (SkipActive && enable) return;

            WaitingForInput = enable;
            if (!enable)
            {
                waitForWaitForInputDisabledTCS?.TrySetResult();
                waitForWaitForInputDisabledTCS = null;
            }

            OnWaitingForInput?.Invoke(enable);
        }
        
        public async UniTask SynchronizeAndDoAsync (Func<UniTask> task)
        {
            onSynchronizeTasks.Enqueue(task);
            
            if (synchronizeTCS != null)
            {
                await synchronizeTCS.Task;
                return;
            }

            using (new InteractionBlocker())
            {
                synchronizationCTS.Cancel();
                synchronizeTCS = new UniTaskCompletionSource();

                await UniTask.WaitWhile(() => executedCommandsCount > 0);

                while (onSynchronizeTasks.Count > 0)
                    await onSynchronizeTasks.Dequeue()();

                synchronizationCTS.Dispose();
                synchronizationCTS = new CancellationTokenSource();
                synchronizeTCS.TrySetResult();
                synchronizeTCS = null;
            }
        }

        public bool HasPlayed (string scriptName, int? playlistIndex = null)
        {
            if (playlistIndex.HasValue) return playedRegister.IsIndexPlayed(scriptName, playlistIndex.Value);
            return playedRegister.IsScriptPlayed(scriptName);
        }

        /// <summary>
        /// In case synchronization is performed, will wait until it's completed;
        /// returns true in case provided token has requested cancellation.
        /// </summary>
        /// <remarks>This should be awaited after any async operation in the playback routine.</remarks>
        protected virtual async UniTask<bool> WaitSynchronizeAsync (AsyncToken asyncToken)
        {
            if (asyncToken.Canceled) return true;
            if (synchronizeTCS != null)
                await synchronizeTCS.Task;
            return asyncToken.Canceled;
        }

        protected virtual int FindPlayableIndex (PlaybackSpot spot)
        {
            var index = Playlist.IndexOf(spot);
            if (index >= 0) return index;
            
            if (Configuration.ResolveMode == PlayerResolveMode.Error)
                throw new Error($"Failed to play `{spot}`: the script has probably changed after the save was made.");
            
            if (Configuration.ResolveMode == PlayerResolveMode.Restart && Playlist.GetCommandAfterLine(0, -1) is Command firstCommand)
                return Playlist.IndexOf(firstCommand.PlaybackSpot);
            
            if (Playlist.GetCommandAfterLine(spot.LineIndex, -1) is Command nextCommand)
            {
                Engine.Warn($"Failed to play `{spot}`: the script has probably changed after the save was made." +
                            " Will play next command instead; expect undefined behaviour.");
                return Playlist.IndexOf(nextCommand.PlaybackSpot);
            }
            if (Playlist.GetCommandBeforeLine(spot.LineIndex, 0) is Command prevCommand)
            {
                Engine.Warn($"Failed to play `{spot}`: the script has probably changed after the save was made." +
                            " Will play previous command instead; expect undefined behaviour.");
                return Playlist.IndexOf(prevCommand.PlaybackSpot);
            }
            Engine.Warn($"Failed to play `{spot}`: neither the spot, nor playable commands after it were found.");
            
            throw new Error($"Failed to play `{spot}`: the script has no playable commands.");
        }

        private void EnableSkip () => SetSkipEnabled(true);
        private void DisableSkip () => SetSkipEnabled(false);
        private void ToggleSkip () => SetSkipEnabled(!SkipActive);
        private void EnableAutoPlay () => SetAutoPlayEnabled(true);
        private void DisableAutoPlay () => SetAutoPlayEnabled(false);
        private void ToggleAutoPlay () => SetAutoPlayEnabled(!AutoPlayActive);
        private void EnableWaitingForInput () => SetWaitingForInputEnabled(true);
        private void DisableWaitingForInput () => SetWaitingForInputEnabled(false);

        private bool GetSkipAllowed ()
        {
            if (SkipMode == PlayerSkipMode.Everything) return true;
            if (PlayedScript is null) return false;
            return HasPlayed(PlayedScript.Name, PlayedIndex + 1);
        }

        private async UniTask WaitForWaitForInputDisabledAsync ()
        {
            if (waitForWaitForInputDisabledTCS is null)
                waitForWaitForInputDisabledTCS = new UniTaskCompletionSource();
            await waitForWaitForInputDisabledTCS.Task;
        }

        private async UniTask WaitForInputInAutoPlayAsync ()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Configuration.MinAutoPlayDelay), true);
            while (AutoPlayActive && WaitingForInput && Engine.GetService<IAudioManager>()?.GetPlayedVoicePath() != null)
                await AsyncUtils.WaitEndOfFrameAsync();
            if (!AutoPlayActive) await WaitForWaitForInputDisabledAsync(); // In case auto play was disabled while waiting for delay.
        }

        private async UniTask ExecutePlayedCommandAsync (AsyncToken asyncToken)
        {
            if (PlayedCommand is null || !PlayedCommand.ShouldExecute) return;

            OnCommandExecutionStart?.Invoke(PlayedCommand);

            playedRegister.RegisterPlayedIndex(PlayedScript.Name, PlayedIndex);

            for (int i = preExecutionTasks.Count - 1; i >= 0; i--)
            {
                await preExecutionTasks[i](PlayedCommand);
                if (await WaitSynchronizeAsync(asyncToken)) return;
            }

            if (await WaitSynchronizeAsync(asyncToken)) return;

            var synchronizationToken = shouldCompleteNextCommand ? new CancellationToken(true) : synchronizationCTS.Token;
            shouldCompleteNextCommand = false;
            executedPlayedCommand = true;
            executedCommandsCount++;

            var shouldWait = Configuration.ShouldWait(PlayedCommand);
            if (Configuration.CompleteOnContinue && continueInput != null && shouldWait && !PlayedCommand.ForceWait)
            {
                var syncAndContinueCTS = LinkSynchronizationWithContinueInputTokens(synchronizationToken);
                var executionToken = new AsyncToken(commandExecutionCTS.Token, syncAndContinueCTS.Token);
                await ExecuteIgnoringCancellationAsync(PlayedCommand, executionToken);
                syncAndContinueCTS.Dispose();
            }
            else
            {
                var executionToken = new AsyncToken(commandExecutionCTS.Token, synchronizationToken);
                if (shouldWait) await ExecuteIgnoringCancellationAsync(PlayedCommand, executionToken);
                else ExecuteIgnoringCancellationAsync(PlayedCommand, executionToken).Forget();
            }
            if (await WaitSynchronizeAsync(asyncToken)) return;

            for (int i = postExecutionTasks.Count - 1; i >= 0; i--)
            {
                await postExecutionTasks[i](PlayedCommand);
                if (await WaitSynchronizeAsync(asyncToken)) return;
            }

            if (await WaitSynchronizeAsync(asyncToken)) return;

            if (providerConfig.ResourcePolicy == ResourcePolicy.Dynamic)
            {
                if (PlayedCommand is Command.IPreloadable playedPreloadableCmd)
                    playedPreloadableCmd.ReleasePreloadedResources();
                if (Playlist.GetCommandByIndex(PlayedIndex + providerConfig.DynamicPolicySteps) is Command.IPreloadable nextPreloadableCmd)
                    nextPreloadableCmd.PreloadResourcesAsync().Forget();
            }

            OnCommandExecutionFinish?.Invoke(PlayedCommand);
        }

        private CancellationTokenSource LinkSynchronizationWithContinueInputTokens (CancellationToken synchronizationToken)
        {
            var continueInputCT = continueInput.GetInputStartCancellationToken();
            var skipInputCT = skipInput?.GetInputStartCancellationToken() ?? default;
            var toggleSkipInputCT = toggleSkipInput?.GetInputStartCancellationToken() ?? default;
            return CancellationTokenSource.CreateLinkedTokenSource(synchronizationToken, continueInputCT, skipInputCT, toggleSkipInputCT);
        }

        private async UniTask ExecuteIgnoringCancellationAsync (Command command, AsyncToken asyncToken)
        {
            try { await PlayedCommand.ExecuteAsync(asyncToken); }
            catch (AsyncOperationCanceledException) { }
            executedCommandsCount--;
        }

        private async UniTask PlayRoutineAsync (AsyncToken asyncToken)
        {
            while (Engine.Initialized && Playing)
            {
                if (WaitingForInput)
                {
                    if (AutoPlayActive) 
                    { 
                        await UniTask.WhenAny(WaitForInputInAutoPlayAsync(), WaitForWaitForInputDisabledAsync()); 
                        if (await WaitSynchronizeAsync(asyncToken)) return;
                        DisableWaitingForInput(); 
                    }
                    else
                    {
                        await WaitForWaitForInputDisabledAsync();
                        if (await WaitSynchronizeAsync(asyncToken)) return;
                    }
                }

                await ExecutePlayedCommandAsync(asyncToken);
                if (await WaitSynchronizeAsync(asyncToken)) return;

                var nextActionAvailable = SelectNextCommand();
                if (!nextActionAvailable) break;

                if (SkipActive && !GetSkipAllowed()) SetSkipEnabled(false);
            }
        }

        private async UniTask<bool> FastForwardRoutineAsync (AsyncToken asyncToken, int targetPlaylistIndex, bool executePlayedCommand)
        {
            SetSkipEnabled(true);

            if (executePlayedCommand)
            {
                await ExecutePlayedCommandAsync(asyncToken);
                if (await WaitSynchronizeAsync(asyncToken)) return false;
            }

            var reachedLine = true;
            while (Engine.Initialized && Playing)
            {
                var nextCommandAvailable = SelectNextCommand();
                if (!nextCommandAvailable) { reachedLine = false; break; }

                if (PlayedIndex >= targetPlaylistIndex) { reachedLine = true; break; }

                await ExecutePlayedCommandAsync(asyncToken);
                if (await WaitSynchronizeAsync(asyncToken)) return false;
                SetSkipEnabled(true); // Force skip mode to be always active while fast-forwarding.

                if (asyncToken.Canceled) { reachedLine = false; break; }
            }

            SetSkipEnabled(false);
            return reachedLine;
        }

        /// <summary>
        /// Attempts to select next <see cref="Command"/> in the current <see cref="Playlist"/>.
        /// </summary>
        /// <returns>Whether next command is available and was selected.</returns>
        private bool SelectNextCommand ()
        {
            PlayedIndex++;
            if (Playlist.IsIndexValid(PlayedIndex))
            {
                executedPlayedCommand = false;
                return true;
            }

            // No commands left in the played script.
            Engine.Log($"Script '{PlayedScript.Name}' has finished playing, and there wasn't a follow-up goto command. " +
                        "Consider using stop command in case you wish to gracefully stop script execution.");
            Stop();
            return false;
        }

        /// <summary>
        /// Cancels all the asynchronously-running commands.
        /// </summary>
        /// <remarks>
        /// Be aware that this could lead to an inconsistent state; only use when the current engine state is going to be discarded 
        /// (eg, when preparing to load a game or perform state rollback).
        /// </remarks>
        private void CancelCommands ()
        {
            commandExecutionCTS.Cancel();
            commandExecutionCTS.Dispose();
            commandExecutionCTS = new CancellationTokenSource();
        }
    } 
}
