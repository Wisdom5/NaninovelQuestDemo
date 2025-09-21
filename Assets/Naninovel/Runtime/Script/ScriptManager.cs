// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptManager"/>
    [InitializeAtRuntime]
    public class ScriptManager : IScriptManager
    {
        public virtual ScriptsConfiguration Configuration { get; }
        public virtual IReadOnlyCollection<Script> Scripts => nameToScript.Values;
        public virtual IReadOnlyCollection<Script> ExternalScripts => nameToExternalScript.Values;
        public virtual int TotalCommandsCount { get; private set; } = -1;

        private readonly IResourceProviderManager providers;
        private readonly Dictionary<string, Script> nameToScript = new Dictionary<string, Script>();
        private readonly Dictionary<string, Script> nameToExternalScript = new Dictionary<string, Script>();
        private ResourceLoader<Script> scriptLoader;
        private ResourceLoader<Script> externalScriptLoader;

        public ScriptManager (ScriptsConfiguration config, IResourceProviderManager providers)
        {
            Configuration = config;
            this.providers = providers;
        }

        public virtual async UniTask InitializeServiceAsync ()
        {
            scriptLoader = Configuration.Loader.CreateFor<Script>(providers);
            externalScriptLoader = Configuration.ExternalLoader.CreateFor<Script>(providers);
            foreach (var resource in await scriptLoader.LoadAllAsync())
                nameToScript[resource.Object.Name] = resource;
            if (Configuration.EnableCommunityModding)
                foreach (var resource in await externalScriptLoader.LoadAllAsync())
                    nameToExternalScript[resource.Object.Name] = resource;
            if (Configuration.CountTotalCommands)
                TotalCommandsCount = CountTotalCommands();
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            scriptLoader?.UnloadAll();
            externalScriptLoader?.UnloadAll();
        }

        public bool HasScript (string scriptName)
        {
            return !string.IsNullOrEmpty(scriptName) && nameToScript.ContainsKey(scriptName);
        }

        public Script GetScript (string scriptName)
        {
            return nameToScript.TryGetValue(scriptName, out var script) ? script
                : throw new Error($"Failed to get `{scriptName}` script: not found.");
        }

        protected virtual int CountTotalCommands ()
        {
            var result = 0;
            foreach (var script in Scripts)
                result += script.ExtractCommands().Count;
            return result;
        }
    }
}
