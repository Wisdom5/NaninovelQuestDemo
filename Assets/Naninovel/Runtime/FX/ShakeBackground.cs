// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="IBackgroundActor"/> or the main one.
    /// </summary>
    public class ShakeBackground : ShakeTransform
    {
        protected override Transform GetShakenTransform ()
        {
            var manager = Engine.GetService<IBackgroundManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? BackgroundsConfiguration.MainActorId : ObjectName;
            if (!manager.ActorExists(id))
                throw new Error($"Failed to shake background with `{id}` ID: actor not found.");
            return (manager.GetActor(id) as MonoBehaviourActor<BackgroundMetadata>)?.Transform;
        }
    }
}
