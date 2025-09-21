// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class EngineSettings : ConfigurationSettings<EngineConfiguration>
    {
        #pragma warning disable CS0162
        #if JSON_AVAILABLE
        private const bool jsonAvailable = true;
        #else
        private const bool jsonAvailable = false;
        #endif

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(EngineConfiguration.CustomInitializationUI)] = p => DrawWhen(Configuration.ShowInitializationUI, p);
            drawers[nameof(EngineConfiguration.ObjectsLayer)] = property => {
                if (!Configuration.OverrideObjectsLayer) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.intValue = EditorGUILayout.LayerField(label, property.intValue);
            };
            drawers[nameof(EngineConfiguration.EnableBridging)] = property => {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (jsonAvailable) OnChanged(BridgingService.RestartServer, property);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(property);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.HelpBox("Install 'Newtonsoft Json' package via Unity's package manager to enable bridging.", MessageType.Info);
                }
            };
            drawers[nameof(EngineConfiguration.ServerPort)] = p => DrawWhen(jsonAvailable && Configuration.EnableBridging, p);
            drawers[nameof(EngineConfiguration.AutoGenerateMetadata)] = p => DrawWhen(jsonAvailable && Configuration.EnableBridging, p);
            drawers[nameof(EngineConfiguration.GenerateLabelMetadata)] = p => DrawWhen(jsonAvailable && Configuration.EnableBridging, p);
            drawers[nameof(EngineConfiguration.DebugOnlyConsole)] = p => DrawWhen(Configuration.EnableDevelopmentConsole, p);
            return drawers;
        }
    }
}
