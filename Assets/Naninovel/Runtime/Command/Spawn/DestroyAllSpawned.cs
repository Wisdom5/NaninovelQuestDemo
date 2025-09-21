// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Destroys all the objects spawned with [@spawn] command.
    /// Equal to invoking [@despawn] for all the currently spawned objects.
    /// </summary>
    [CommandAlias("despawnAll")]
    public class DestroyAllSpawned : Command
    {
        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var spawned = SpawnManager.GetAllSpawned();
            var tasks = spawned.Select(s => new DestroySpawned { Path = s.Path }.ExecuteAsync(asyncToken));
            await UniTask.WhenAll(tasks);
        }
    }
}
