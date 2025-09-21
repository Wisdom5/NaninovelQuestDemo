// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class SpawnSettings : ResourcefulSettings<SpawnConfiguration>
    {
        protected override string HelpUri => "guide/special-effects.html";

        protected override Type ResourcesTypeConstraint => typeof(GameObject);
        protected override string ResourcesCategoryId => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@spawn %name%` to instantiate and `@despawn %name%` to destroy the prefab.";

        [MenuItem("Naninovel/Resources/Spawn")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
