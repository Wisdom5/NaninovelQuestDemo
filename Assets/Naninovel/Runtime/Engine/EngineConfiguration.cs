// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class EngineConfiguration : Configuration
    {
        public const string DefaultGeneratedDataPath = "NaninovelData";
        public static readonly string[] DefaultTypeAssemblies = { "Assembly-CSharp", "Assembly-CSharp-Editor", "Elringus.Naninovel.Runtime", "Elringus.Naninovel.Editor" };

        [Tooltip("Relative (to the application data directory) path to store the automatically generated assets.")]
        public string GeneratedDataPath = DefaultGeneratedDataPath;
        [Tooltip("Whether to assign a specific layer to all the engine objects. Engine's camera will use the layer for the culling mask. Use this to isolate Naninovel objects from being rendered by other cameras.")]
        public bool OverrideObjectsLayer;
        [Tooltip("When `Override Objects Layer` is enabled, the specified layer will be assigned to all the engine objects.")]
        public int ObjectsLayer;
        [Tooltip("Log type to use for UniTask-related exceptions.")]
        public LogType AsyncExceptionLogType = LogType.Error;
        [Tooltip("When looking for various types (eg, actor implementations, serialization handlers, managed text, etc) the engine will only scan the exported types of the specified assemblies for better performance. In case you're keeping your Naninovel-related types outside of Unity's predefined assemblies (using assembly definitions), add the assembly names here.\n\nWarning: Recompile the solution or restart Unity editor after modifying the list in order for changes to take effect.")]
        public string[] TypeAssemblies = DefaultTypeAssemblies;

        [Header("Initialization")]
        [Tooltip("Whether to automatically initialize the engine when application starts.")]
        public bool InitializeOnApplicationLoad = true;
        [Tooltip("Whether to apply `DontDestroyOnLoad` to the engine objects, making their lifetime independent of any loaded scenes. When disabled, the objects will be part of the Unity scene where the engine was initialized and will be destroyed when the scene is unloaded.")]
        public bool SceneIndependent = true;
        [Tooltip("Whether to show a loading UI while the engine is initializing.")]
        public bool ShowInitializationUI = true;
        [Tooltip("UI to show while the engine is initializing (when enabled). Will use a default one when not provided.")]
        public ScriptableUIBehaviour CustomInitializationUI;

        [Header("Bridging")]
        [Tooltip("Whether to automatically start the bridging server to communicate with external Naninovel tools: IDE extension, web editor, etc.")]
        public bool EnableBridging;
        [Tooltip("The network port for the server to listen. Change both here and in the external tools in case the default port is occupied by another application.")]
        public int ServerPort = 41016;
        [Tooltip("Whether to automatically generate project metadata when Unity editor is started.")]
        public bool AutoGenerateMetadata = true;
        [Tooltip("Whether to generate metadata used for script labels autocompletion. May take a substantial time when there are a lot of scripts in the project.")]
        public bool GenerateLabelMetadata = true;

        [Header("Development Console")]
        [Tooltip("Whether to enable development console.")]
        public bool EnableDevelopmentConsole = true;
        [Tooltip("When enabled, development console will only be available in development (debug) builds.")]
        public bool DebugOnlyConsole;
    }
}
