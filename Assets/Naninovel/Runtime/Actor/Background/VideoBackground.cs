// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(VideoClip), true)]
    public class VideoBackground : VideoActor<BackgroundMetadata>, IBackgroundActor
    {
        protected override string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().BgmGroupPath;

        private BackgroundMatcher matcher;

        public VideoBackground (string id, BackgroundMetadata metadata)
            : base(id, metadata) { }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            matcher = BackgroundMatcher.CreateFor(ActorMetadata, TransitionalRenderer);
        }

        public override void Dispose ()
        {
            base.Dispose();
            matcher?.Stop();
        }
    }
}
