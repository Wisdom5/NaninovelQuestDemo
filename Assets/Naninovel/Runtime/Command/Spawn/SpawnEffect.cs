// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Globalization;

namespace Naninovel.Commands
{
    /// <summary>
    /// Base class for FX spawn commands (@shake, @rain, @bokeh, etc).
    /// </summary>
    public abstract class SpawnEffect : Command, Command.IPreloadable
    {
        /// <summary>
        /// Resource path of the effect to spawn.
        /// </summary>
        protected abstract string Path { get; }
        /// <summary>
        /// Whether the effect should de-spawn.
        /// </summary>
        protected virtual bool DestroyWhen { get; } = false;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public virtual async UniTask PreloadResourcesAsync ()
        {
            await SpawnManager.HoldResourcesAsync(Path, this);
        }

        public virtual void ReleasePreloadedResources ()
        {
            SpawnManager.ReleaseResources(Path, this);
        }

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default) => DestroyWhen
            ? new DestroySpawned {
                Path = Path,
                Params = GetDestroyParameters(),
                Wait = Wait,
                ConditionalExpression = ConditionalExpression
            }.ExecuteAsync(asyncToken)
            : new Spawn {
                Path = Path,
                Params = GetSpawnParameters(),
                ScenePosition = GetScenePosition(),
                Position = GetPosition(),
                Rotation = GetRotation(),
                Scale = GetScale(),
                Wait = Wait,
                ConditionalExpression = ConditionalExpression
            }.ExecuteAsync(asyncToken);

        protected abstract StringListParameter GetSpawnParameters ();
        protected virtual StringListParameter GetDestroyParameters () => null;

        protected virtual DecimalListParameter GetScenePosition () => null;
        protected virtual DecimalListParameter GetPosition () => null;
        protected virtual DecimalListParameter GetRotation () => null;
        protected virtual DecimalListParameter GetScale () => null;

        protected virtual string ToSpawnParam (StringParameter param) => Assigned(param) ? param.Value : null;
        protected virtual string ToSpawnParam (IntegerParameter param) => Assigned(param) ? param.Value.ToString() : null;
        protected virtual string ToSpawnParam (DecimalParameter param) => Assigned(param) ? param.Value.ToString(CultureInfo.InvariantCulture) : null;
        protected virtual string ToSpawnParam (BooleanParameter param) => Assigned(param) ? param.Value.ToString() : null;
    }
}
