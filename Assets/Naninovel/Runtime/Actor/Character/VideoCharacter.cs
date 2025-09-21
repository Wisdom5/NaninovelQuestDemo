// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(VideoClip), true)]
    public class VideoCharacter : VideoActor<CharacterMetadata>, ICharacterActor
    {
        protected override string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().VoiceGroupPath;

        public CharacterLookDirection LookDirection
        {
            get => TransitionalRenderer.GetLookDirection(ActorMetadata.BakedLookDirection);
            set => TransitionalRenderer.SetLookDirection(value, ActorMetadata.BakedLookDirection);
        }

        public VideoCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata) { }

        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration,
            EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.ChangeLookDirectionAsync(lookDirection,
                ActorMetadata.BakedLookDirection, duration, easingType, asyncToken);
        }
    }
}
