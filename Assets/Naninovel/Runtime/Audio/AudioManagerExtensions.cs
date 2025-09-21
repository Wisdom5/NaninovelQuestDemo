// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IAudioManager"/>.
    /// </summary>
    public static class AudioManagerExtensions
    {
        /// <summary>
        /// Checks whether a BGM track with the provided resource path is currently playing.
        /// </summary>
        /// <param name="path">Name (local path) of the audio resource.</param>
        public static bool IsBgmPlaying (this IAudioManager manager, string path)
        {
            return manager.GetPlayedBgmPaths().Contains(path);
        }

        /// <summary>
        /// Checks whether an SFX track with the provided resource path is currently playing.
        /// </summary>
        /// <param name="path">Name (local path) of the audio resource.</param>
        public static bool IsSfxPlaying (this IAudioManager manager, string path)
        {
            return manager.GetPlayedSfxPaths().Contains(path);
        }

        /// <summary>
        /// Checks whether a voice track with the provided resource path is currently playing.
        /// </summary>
        /// <param name="path">Name (local path) of the voice resource.</param>
        public static bool IsVoicePlaying (this IAudioManager manager, string path)
        {
            return manager.GetPlayedVoicePath() == path;
        }

        /// <summary>
        /// Plays voice clips with the provided resource paths in sequence.
        /// </summary>
        /// <param name="pathList">Names (local paths) of the voice resources.</param>
        /// <param name="volume">Volume of the voice playback.</param>
        /// <param name="group">Path of an <see cref="AudioMixerGroup"/> of the current <see cref="AudioMixer"/> to use when playing the voice.</param>
        /// <param name="authorId">ID of the author (character actor) of the played voices.</param>
        public static async UniTask PlayVoiceSequenceAsync (this IAudioManager manager, IReadOnlyCollection<string> pathList,
            float volume = 1f, string group = default, string authorId = default, AsyncToken asyncToken = default)
        {
            foreach (var path in pathList)
            {
                await manager.PlayVoiceAsync(path, volume, group, authorId, asyncToken);
                await UniTask.WaitWhile(() => IsVoicePlaying(manager, path) && asyncToken.EnsureNotCanceled());
            }
        }
    }
}
