// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ISpawnManager"/>
    [InitializeAtRuntime]
    public class SpawnManager : IStatefulService<GameStateMap>, ISpawnManager
    {
        [Serializable]
        public class GameState
        {
            public List<SpawnedObjectState> SpawnedObjects;
        }

        public virtual SpawnConfiguration Configuration { get; }

        private readonly Dictionary<string, SpawnedObject> spawnedMap = new Dictionary<string, SpawnedObject>();
        private readonly IResourceProviderManager providersManager;
        private ResourceLoader<GameObject> loader;
        private GameObject container;

        public SpawnManager (SpawnConfiguration config, IResourceProviderManager providersManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            loader = Configuration.Loader.CreateFor<GameObject>(providersManager);
            container = Engine.CreateObject("Spawn");
            return UniTask.CompletedTask;
        }

        public virtual void ResetService ()
        {
            DestroyAllSpawned();
        }

        public virtual void DestroyService ()
        {
            DestroyAllSpawned();
            loader?.ReleaseAll(this);
            ObjectUtils.DestroyOrImmediate(container);
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                SpawnedObjects = spawnedMap.Values
                    .Select(o => new SpawnedObjectState(o)).ToList()
            };
            stateMap.SetState(state);
        }

        public virtual async UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state?.SpawnedObjects?.Count > 0) await LoadObjectsAsync();
            else if (spawnedMap.Count > 0) DestroyAllSpawned();

            async UniTask LoadObjectsAsync ()
            {
                var tasks = new List<UniTask>();
                var toDestroy = spawnedMap.Values.ToList();
                foreach (var objState in state.SpawnedObjects)
                    if (IsSpawned(objState.Path)) UpdateObject(objState);
                    else tasks.Add(SpawnObjectAsync(objState));
                foreach (var obj in toDestroy)
                    DestroySpawned(obj.Path);
                await UniTask.WhenAll(tasks);

                async UniTask SpawnObjectAsync (SpawnedObjectState objState)
                {
                    var spawned = await SpawnAsync(objState.Path);
                    objState.ApplyTo(spawned);
                    spawned.AwaitSpawnAsync().Forget();
                }

                void UpdateObject (SpawnedObjectState objState)
                {
                    var spawned = GetSpawned(objState.Path);
                    toDestroy.Remove(spawned);
                    objState.ApplyTo(spawned);
                    spawned.AwaitSpawnAsync().Forget();
                }
            }
        }

        public virtual async UniTask HoldResourcesAsync (string path, object holder)
        {
            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            await loader.LoadAndHoldAsync(resourcePath, holder);
        }

        public virtual void ReleaseResources (string path, object holder)
        {
            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            if (!loader.IsLoaded(resourcePath)) return;

            loader.Release(resourcePath, holder, false);
            if (loader.CountHolders(resourcePath) == 0)
            {
                if (IsSpawned(path))
                    DestroySpawned(path);
                loader.Release(resourcePath, holder);
            }
        }

        public virtual async UniTask<SpawnedObject> SpawnAsync (string path, AsyncToken asyncToken = default)
        {
            if (IsSpawned(path)) throw new Error($"Object `{path}` is already spawned and can't be spawned again before it's destroyed.");

            var resourcePath = SpawnConfiguration.ProcessInputPath(path, out _);
            var prefabResource = await loader.LoadAndHoldAsync(resourcePath, this);
            asyncToken.ThrowIfCanceled();
            if (!prefabResource.Valid) throw new Error($"Failed to load `{path}` spawn resource.");

            var gameObject = Engine.Instantiate(prefabResource.Object, path, parent: container.transform);
            var spawnedObject = new SpawnedObject(path, gameObject);
            spawnedMap[path] = spawnedObject;
            return spawnedObject;
        }

        public virtual void DestroySpawned (string path)
        {
            if (!IsSpawned(path)) return;
            var spawnedObject = GetSpawned(path);
            spawnedMap.Remove(path);
            ObjectUtils.DestroyOrImmediate(spawnedObject.GameObject);
        }

        public virtual bool IsSpawned (string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return spawnedMap.ContainsKey(path);
        }

        public virtual SpawnedObject[] GetAllSpawned ()
        {
            return spawnedMap.Values.ToArray();
        }

        public virtual SpawnedObject GetSpawned (string path)
        {
            return spawnedMap[path];
        }

        protected virtual void DestroyAllSpawned ()
        {
            foreach (var spawnedObj in spawnedMap.Values)
                ObjectUtils.DestroyOrImmediate(spawnedObj.GameObject);
            spawnedMap.Clear();
        }
    }
}
