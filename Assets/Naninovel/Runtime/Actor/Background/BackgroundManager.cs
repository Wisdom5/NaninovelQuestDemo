// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel
{
    /// <inheritdoc cref="IBackgroundManager"/>
    [InitializeAtRuntime]
    public class BackgroundManager : OrthoActorManager<IBackgroundActor, BackgroundState, BackgroundMetadata, BackgroundsConfiguration>, IBackgroundManager
    {
        public BackgroundManager (BackgroundsConfiguration config, CameraConfiguration cameraConfig)
            : base(config, cameraConfig) { }
    }
}
