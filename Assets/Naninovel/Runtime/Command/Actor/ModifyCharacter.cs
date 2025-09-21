// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [character actor](/guide/characters).
    /// </summary>
    [CommandAlias("char")]
    [ActorContext(CharactersConfiguration.DefaultPathPrefix, paramId: nameof(Id))]
    [ConstantContext("Poses/Characters/{:Id??:IdAndAppearance[0]}+Poses/Characters/*", paramId: nameof(Pose))]
    public class ModifyCharacter : ModifyOrthoActor<ICharacterActor, CharacterState, CharacterMetadata, CharactersConfiguration, ICharacterManager>
    {
        /// <summary>
        /// ID of the character to modify (specify `*` to affect all visible characters) and an appearance (or [pose](/guide/characters#poses)) to set.
        /// When appearance is not provided, will use either a `Default` (is exists) or a random one.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ActorContext(CharactersConfiguration.DefaultPathPrefix, 0), AppearanceContext(1)]
        public NamedStringParameter IdAndAppearance;
        /// <summary>
        /// Look direction of the actor; supported values: left, right, center.
        /// </summary>
        [ParameterAlias("look"), ConstantContext(typeof(CharacterLookDirection))]
        public StringParameter LookDirection;
        /// <summary>
        /// Name (path) of the [avatar texture](/guide/characters#avatar-textures) to assign for the character.
        /// Use `none` to remove (un-assign) avatar texture from the character.
        /// </summary>
        [ParameterAlias("avatar")]
        public StringParameter AvatarTexturePath;

        protected override bool AllowPreload => !IdAndAppearance.DynamicValue;
        protected override string AssignedId => base.AssignedId ?? IdAndAppearance?.Name;
        protected override string AlternativeAppearance => IdAndAppearance?.NamedValue;
        protected virtual CharacterLookDirection? AssignedLookDirection => Assigned(LookDirection) ? ParseLookDirection(LookDirection) : PosedLookDirection;

        protected CharacterLookDirection? PosedLookDirection => GetPosed(nameof(CharacterState.LookDirection))?.LookDirection;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            await base.ExecuteAsync(asyncToken);

            if (!Assigned(AvatarTexturePath)) // Check if we can map current appearance to an avatar texture path.
            {
                var avatarPath = $"{AssignedId}/{AssignedAppearance}";
                if (ActorManager.AvatarTextureExists(avatarPath) && ActorManager.GetAvatarTexturePathFor(AssignedId) != avatarPath)
                    ActorManager.SetAvatarTexturePathFor(AssignedId, avatarPath);
                else // Check if a default avatar texture for the character exists and assign if it does.
                {
                    var defaultAvatarPath = $"{AssignedId}/Default";
                    if (ActorManager.AvatarTextureExists(defaultAvatarPath) && ActorManager.GetAvatarTexturePathFor(AssignedId) != defaultAvatarPath)
                        ActorManager.SetAvatarTexturePathFor(AssignedId, defaultAvatarPath);
                }
            }
            else // User provided specific avatar texture path, assigning it.
            {
                if (AvatarTexturePath?.Value.EqualsFastIgnoreCase("none") ?? false)
                    ActorManager.RemoveAvatarTextureFor(AssignedId);
                else ActorManager.SetAvatarTexturePathFor(AssignedId, AvatarTexturePath);
            }
        }

        protected override async UniTask ApplyModificationsAsync (ICharacterActor actor, EasingType easingType, AsyncToken asyncToken)
        {
            var arrange = ShouldAutoArrange(actor);
            var tasks = new List<UniTask>();
            var duration = actor.Visible ? AssignedDuration : 0;
            tasks.Add(base.ApplyModificationsAsync(actor, easingType, asyncToken));
            tasks.Add(ApplyLookDirectionModificationAsync(actor, duration, easingType, asyncToken));
            if (arrange) tasks.Add(ActorManager.ArrangeCharactersAsync(!AssignedLookDirection.HasValue, AssignedDuration, easingType, asyncToken));
            await UniTask.WhenAll(tasks);
        }

        protected virtual bool ShouldAutoArrange (ICharacterActor actor)
        {
            if (!ActorManager.Configuration.AutoArrangeOnAdd) return false;
            var addingActor = !actor.Visible && AssignedVisibility.HasValue && AssignedVisibility.Value;
            var positionAssigned = AssignedPosition != null;
            var renderedToTexture = ActorManager.Configuration.GetMetadataOrDefault(AssignedId).RenderTexture != null;
            return addingActor && !positionAssigned && !renderedToTexture;
        }

        protected virtual async UniTask ApplyLookDirectionModificationAsync (ICharacterActor actor, float duration, EasingType easingType, AsyncToken asyncToken)
        {
            if (!AssignedLookDirection.HasValue) return;
            if (Mathf.Approximately(duration, 0)) actor.LookDirection = AssignedLookDirection.Value;
            else await actor.ChangeLookDirectionAsync(AssignedLookDirection.Value, AssignedDuration, easingType, asyncToken);
        }

        protected virtual CharacterLookDirection? ParseLookDirection (string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value.EqualsFastIgnoreCase("right")) return CharacterLookDirection.Right;
            if (value.EqualsFastIgnoreCase("left")) return CharacterLookDirection.Left;
            if (value.EqualsFastIgnoreCase("center")) return CharacterLookDirection.Center;
            Err($"`{value}` is not a valid value for a character look direction; see API guide for `@char` command for the list of supported values.");
            return null;
        }
    }
}
