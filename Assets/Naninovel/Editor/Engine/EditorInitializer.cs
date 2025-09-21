// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel
{
    public static class EditorInitializer
    {
        public static async UniTask InitializeAsync ()
        {
            if (Engine.Initialized) return;

            var configProvider = new ProjectConfigurationProvider();
            var services = new List<IEngineService>();

            var resources = new ResourceProviderManager(configProvider.GetConfiguration<ResourceProviderConfiguration>());
            services.Add(resources);

            var l10n = new LocalizationManager(configProvider.GetConfiguration<LocalizationConfiguration>(), resources);
            services.Add(l10n);

            var communityL10n = new CommunityLocalization();
            services.Add(communityL10n);

            var scripts = new ScriptManager(configProvider.GetConfiguration<ScriptsConfiguration>(), resources);
            services.Add(scripts);

            var vars = new CustomVariableManager(configProvider.GetConfiguration<CustomVariablesConfiguration>());
            services.Add(vars);

            var text = new TextManager(configProvider.GetConfiguration<ManagedTextConfiguration>(), resources, l10n, communityL10n);
            services.Add(text);

            var localizer = new TextLocalizer(text, scripts, l10n, communityL10n);
            services.Add(localizer);

            await Engine.InitializeAsync(new EngineParams {
                Services = services,
                ConfigurationProvider = configProvider,
                Behaviour = new EditorBehaviour(),
                Time = new UnityTime()
            });
        }
    }
}
