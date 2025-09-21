// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Naninovel
{
    /// <summary>
    /// Represents a list of <see cref="Command"/> based on the contents of a <see cref="Script"/>.
    /// </summary>
    public class ScriptPlaylist : IReadOnlyList<Command>
    {
        /// <summary>
        /// Name of the script from which the contained commands were extracted.
        /// </summary>
        public readonly string ScriptName;
        /// <summary>
        /// Number of commands in the playlist.
        /// </summary>
        public int Count => commands.Count;

        private readonly List<Command> commands = new List<Command>();

        /// <summary>
        /// Creates new instance from the provided commands collection.
        /// </summary>
        public ScriptPlaylist (string scriptName, IEnumerable<Command> commands)
        {
            ScriptName = scriptName;
            this.commands.AddRange(commands);
        }

        /// <summary>
        /// Creates new instance from the provided script.
        /// </summary>
        public ScriptPlaylist (Script script)
        {
            ScriptName = script.Name;
            commands.AddRange(script.ExtractCommands());
        }

        public Command this [int index] => commands[index];
        public IEnumerator<Command> GetEnumerator () => commands.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
        public Command Find (Predicate<Command> predicate) => commands.Find(predicate);
        public int FindIndex (Predicate<Command> predicate) => commands.FindIndex(predicate);
        public List<Command> GetRange (int index, int count) => commands.GetRange(index, count);
        public bool IsIndexValid (int index) => commands.IsIndexValid(index);

        /// <summary>
        /// Preloads and holds all the resources required to execute
        /// <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public async UniTask PreloadResourcesAsync () => await PreloadResourcesAsync(0, Count - 1);

        /// <summary>
        /// Preloads and holds resources required to execute
        /// <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public async UniTask PreloadResourcesAsync (int startCommandIndex, int endCommandIndex, Action<float> onProgress = default)
        {
            if (Count == 0) return;

            if (!IsIndexValid(startCommandIndex) || !IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
                throw new Error($"Failed to preload `{ScriptName}` script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");

            onProgress?.Invoke(0);
            var count = endCommandIndex + 1 - startCommandIndex;
            var commandsToHold = GetRange(startCommandIndex, count).OfType<Command.IPreloadable>().ToArray();
            var heldCommands = 0;
            await UniTask.WhenAll(commandsToHold.Select(PreloadCommand));

            async UniTask PreloadCommand (Command.IPreloadable command)
            {
                await command.PreloadResourcesAsync();
                onProgress?.Invoke(++heldCommands / (float)commandsToHold.Length);
            }
        }

        /// <summary>
        /// Releases all the held resources required to execute
        /// <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public void ReleaseResources () => ReleaseResources(0, commands.Count - 1);

        /// <summary>
        /// Releases all the held resources required to execute
        /// <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public void ReleaseResources (int startCommandIndex, int endCommandIndex)
        {
            if (Count == 0) return;

            if (!IsIndexValid(startCommandIndex) || !IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
                throw new Error($"Failed to unload `{ScriptName}` script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");

            var commandsToRelease = GetRange(startCommandIndex, (endCommandIndex + 1) - startCommandIndex).OfType<Command.IPreloadable>();
            foreach (var cmd in commandsToRelease)
                cmd.ReleasePreloadedResources();
        }

        /// <summary>
        /// Returns a <see cref="Command"/> at the provided playlist/playback index; null if not found.
        /// </summary>
        public Command GetCommandByIndex (int index) =>
            IsIndexValid(index) ? commands[index] : null;

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/>
        /// with provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandByLine (int lineIndex, int inlineIndex) =>
            Find(a => a.PlaybackSpot.LineIndex == lineIndex && a.PlaybackSpot.InlineIndex == inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/>
        /// located at or after the provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandAfterLine (int lineIndex, int inlineIndex) =>
            commands.FirstOrDefault(a => a.PlaybackSpot.LineIndex >= lineIndex && a.PlaybackSpot.InlineIndex >= inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/>
        /// located at or before the provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandBeforeLine (int lineIndex, int inlineIndex) =>
            commands.LastOrDefault(a => a.PlaybackSpot.LineIndex <= lineIndex && a.PlaybackSpot.InlineIndex <= inlineIndex);

        /// <summary>
        /// Returns first command in the list or null when the list is empty.
        /// </summary>
        public Command GetFirstCommand () => commands.FirstOrDefault();

        /// <summary>
        /// Returns last command in the list or null when the list is empty.
        /// </summary>
        public Command GetLastCommand () => commands.LastOrDefault();

        /// <summary>
        /// Finds index of a contained command with the provided playback spot or -1 when not found.
        /// </summary>
        public int IndexOf (PlaybackSpot spot) => FindIndex(c => c.PlaybackSpot == spot);

        /// <summary>
        /// Finds playback (command) index at or after specified line and inline indexes or -1 when not found.
        /// </summary>
        public int GetIndexByLine (int lineIndex, int inlineIndex)
        {
            var startCommand = GetCommandAfterLine(lineIndex, inlineIndex);
            return startCommand != null ? this.IndexOf(startCommand) : -1;
        }
    }
}
