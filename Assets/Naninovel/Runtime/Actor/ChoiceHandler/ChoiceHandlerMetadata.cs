// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to construct and initialize a <see cref="IChoiceHandlerActor"/>.
    /// </summary>
    [System.Serializable]
    public class ChoiceHandlerMetadata : ActorMetadata
    {
        [System.Serializable]
        public class Map : ActorMetadataMap<ChoiceHandlerMetadata> { }

        [Tooltip("Whether to wait until the choice handler UI is completely hidden before proceeding when a choice is picked.")]
        public bool WaitHideOnChoice;

        public ChoiceHandlerMetadata ()
        {
            Implementation = typeof(UIChoiceHandler).AssemblyQualifiedName;
            Loader = new ResourceLoaderConfiguration { PathPrefix = ChoiceHandlersConfiguration.DefaultPathPrefix };
        }

        public override ActorPose<TState> GetPose<TState> (string poseName) => null;
    }
}
