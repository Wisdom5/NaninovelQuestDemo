// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IChoiceHandlerActor"/> actors.
    /// </summary>
    public interface IChoiceHandlerManager : IActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>
    {
        /// <summary>
        /// Used by the service to load custom choice button prefabs.
        /// </summary>
        IResourceLoader<GameObject> ChoiceButtonLoader { get; }
    }
}
