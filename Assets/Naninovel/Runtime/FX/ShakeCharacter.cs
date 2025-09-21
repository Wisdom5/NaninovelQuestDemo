// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ICharacterActor"/> with provided name or a random visible one.
    /// </summary>
    public class ShakeCharacter : ShakeTransform
    {
        [SerializeField] private bool preventPositiveYOffset = true;

        protected override Transform GetShakenTransform ()
        {
            var manager = Engine.GetService<ICharacterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? manager.GetAllActors().FirstOrDefault(a => a.Visible)?.Id : ObjectName;
            if (id is null || !manager.ActorExists(id)) 
                throw new Error($"Failed to shake character with `{id}` ID: actor not found.");
            return (manager.GetActor(id) as MonoBehaviourActor<CharacterMetadata>)?.Transform;
        }

        protected override async UniTask ShakeSequenceAsync (AsyncToken asyncToken)
        {
            if (!preventPositiveYOffset)
            {
                await base.ShakeSequenceAsync(asyncToken);
                return;
            }

            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);

            await MoveAsync(InitialPos - amplitude * .5f, duration * .5f, asyncToken);
            await MoveAsync(InitialPos, duration * .5f, asyncToken);
        }
    }
}
