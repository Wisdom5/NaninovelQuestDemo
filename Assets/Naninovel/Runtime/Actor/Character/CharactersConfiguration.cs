// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class CharactersConfiguration : OrthoActorManagerConfiguration<CharacterMetadata>
    {
        public const string DefaultPathPrefix = "Characters";
        public const string DefaultAvatarsPathPrefix = "CharacterAvatars";
        /// <summary>
        /// <see cref="ManagedTextRecord"/> category for the <see cref="CharacterMetadata.DisplayName"/> records.
        /// </summary>
        public const string DisplayNamesCategory = "CharacterNames";

        [Tooltip("Whether to evenly distribute characters by X-axis when adding a new character without a specified position.")]
        public bool AutoArrangeOnAdd = true;
        [Tooltip("Start (x) and end (y) positions (in 0.0 to 1.0 range) relative to scene width representing the range over which the characters are arranged.")]
        public Vector2 ArrangeRange = new Vector2(0, 1);
        [Tooltip("Metadata to use by default when creating character actors and custom metadata for the created actor ID doesn't exist.")]
        public CharacterMetadata DefaultMetadata = new CharacterMetadata();
        [Tooltip("Metadata to use when creating character actors with specific IDs.")]
        public CharacterMetadata.Map Metadata = new CharacterMetadata.Map();
        [Tooltip("Configuration of the resource loader used with character avatar texture resources.")]
        public ResourceLoaderConfiguration AvatarLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultAvatarsPathPrefix };
        [Tooltip("Named states (poses) shared between the characters; pose name can be used as appearance in [@char] commands to set enabled properties of the associated state.")]
        public List<CharacterMetadata.Pose> SharedPoses = new List<CharacterMetadata.Pose>();

        public override CharacterMetadata DefaultActorMetadata => DefaultMetadata;
        public override ActorMetadataMap<CharacterMetadata> ActorMetadataMap => Metadata;

        public CharactersConfiguration ()
        {
            DefaultEasing = EasingType.SmoothStep;
            ZOffset = 50f;
        }

        protected override ActorPose<TState> GetSharedPose<TState> (string poseName) => SharedPoses.FirstOrDefault(p => p.Name == poseName) as ActorPose<TState>;
    }
}
