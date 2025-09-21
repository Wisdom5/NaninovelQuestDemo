// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.IO;
using System.Text;
using Naninovel.Bridging;
using Naninovel.Metadata;
using Naninovel.UI;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class BridgingService
    {
        private static readonly Server server;
        private static readonly string metadataFilePath;
        private static readonly MessageSerializer serializer = new MessageSerializer(new JsonSerializer());

        static BridgingService ()
        {
            metadataFilePath = Path.Combine(PackagePath.GeneratedDataPath, "Metadata.json");
            server = new Server($"{Application.productName} (Unity)", new BridgingListener(), new JsonSerializer());
            server.OnClientException += (e, _) => Engine.Err($"Bridging: {e}");
            server.OnClientConnected += c => c.Send(new UpdateMetadata { Metadata = LoadCachedMetadata() });
            server.Subscribe<GotoRequest>(HandleGotoRequest);
        }

        [InitializeOnLoadMethod]
        public static void RestartServer ()
        {
            StopServer();
            var config = ProjectConfigurationProvider.LoadOrDefault<EngineConfiguration>();
            if (!config.EnableBridging) return;
            if (config.AutoGenerateMetadata) GenerateMetadataOnEditorStart();
            StartServer(FindServerPort(config.ServerPort));
        }

        [MenuItem("Naninovel/Update Metadata %#u", priority = 3)]
        public static void UpdateMetadata ()
        {
            var meta = MetadataGenerator.GenerateProjectMetadata();
            var xml = SerializeMetadata(meta);
            File.WriteAllText(metadataFilePath, xml, Encoding.UTF8);
            BroadcastMetadata(meta);
        }

        private static Project LoadCachedMetadata ()
        {
            if (!File.Exists(metadataFilePath)) return new Project();
            var xml = File.ReadAllText(metadataFilePath);
            return DeserializeMetadata(xml);
        }

        private static string SerializeMetadata (Project meta)
        {
            var message = new UpdateMetadata { Metadata = meta };
            return serializer.Serialize(message);
        }

        private static Project DeserializeMetadata (string xml)
        {
            if (!serializer.TryDeserialize<UpdateMetadata>(xml, out var message))
                throw new FormatException("Provided metadata string is not a correct format.");
            return message.Metadata;
        }

        private static void StartServer (int port)
        {
            server.Start(port);
            Engine.OnInitializationFinished += AttachServiceListeners;
            Engine.OnDestroyed += BroadcastPlaybackStopped;
            server.WaitForExit.AsUniTask().Forget();
        }

        private static void StopServer ()
        {
            server.StopAsync().AsUniTask().Forget();
            Engine.OnInitializationFinished -= AttachServiceListeners;
            Engine.OnDestroyed -= BroadcastPlaybackStopped;
        }

        private static void AttachServiceListeners ()
        {
            if (!(Engine.Behaviour is RuntimeBehaviour)) return;
            Engine.GetService<IScriptPlayer>().OnCommandExecutionStart += BroadcastPlayedCommand;
        }

        private static void HandleGotoRequest (GotoRequest request)
        {
            var script = request.PlaybackSpot.ScriptName;
            var line = request.PlaybackSpot.LineIndex;
            if (!Application.isPlaying) EditorApplication.EnterPlaymode();
            if (Engine.Initialized) OnEngineInit();
            else Engine.OnInitializationFinished += OnEngineInit;

            void OnEngineInit ()
            {
                Engine.OnInitializationFinished -= OnEngineInit;
                var player = Engine.GetService<IScriptPlayer>();
                if (player.PlayedScript != null && player.PlayedScript.Name == script)
                    player.RewindAsync(line).Forget();
                else
                    Engine.GetService<IStateManager>().ResetStateAsync()
                        .ContinueWith(() => player.PreloadAndPlayAsync(script))
                        .ContinueWith(() => Engine.GetService<IUIManager>().GetUI<ITitleUI>()?.Hide())
                        .ContinueWith(() => player.RewindAsync(line)).Forget();
            }
        }

        private static void BroadcastMetadata (Project meta)
        {
            var message = new UpdateMetadata { Metadata = meta };
            server.Broadcast(message);
        }

        private static void BroadcastPlayedCommand (Command command)
        {
            var status = new PlaybackStatus {
                Playing = true,
                PlayedSpot = GetPlaybackSpot(command)
            };
            server.Broadcast(new UpdatePlaybackStatus { PlaybackStatus = status });
        }

        private static Bridging.PlaybackSpot GetPlaybackSpot (Command command)
        {
            return new Bridging.PlaybackSpot {
                ScriptName = command.PlaybackSpot.ScriptName,
                LineIndex = command.PlaybackSpot.LineIndex,
                InlineIndex = command.PlaybackSpot.InlineIndex
            };
        }

        private static void BroadcastPlaybackStopped ()
        {
            var status = new PlaybackStatus { Playing = false };
            server.Broadcast(new UpdatePlaybackStatus { PlaybackStatus = status });
        }

        private static void GenerateMetadataOnEditorStart ()
        {
            const string key = "NaninovelMetaGeneratedAfterStart";
            if (SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true);
            UpdateMetadata();
        }

        private static int FindServerPort (int preferredPort)
        {
            const int range = 10;
            var port = preferredPort;
            while (!PortFinder.IsPortAvailable(port))
            {
                port++;
                if (port == preferredPort + range)
                    throw new Error($"Failed to establish bridging connection; tried {preferredPort}-{preferredPort + range} ports." +
                                    $"You can change the preferred port or disable bridging in the Naninovel engine configuration.");
            }
            return port;
        }
    }
}
