// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    [EditInProjectSettings]
    public class AudioConfiguration : Configuration
    {
        public const string DefaultAudioPathPrefix = "Audio";
        public const string DefaultVoicePathPrefix = "Voice";
        public const string PlaybackSpotVoiceTemplate = "{0}/{1}.{2}";

        [Tooltip("Configuration of the resource loader used with audio (BGM and SFX) resources.")]
        public ResourceLoaderConfiguration AudioLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultAudioPathPrefix };
        [Tooltip("Configuration of the resource loader used with voice resources.")]
        public ResourceLoaderConfiguration VoiceLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultVoicePathPrefix };
        [Tooltip(nameof(IAudioPlayer) + " implementation responsible for playing audio clips.")]
        public string AudioPlayer = typeof(AudioPlayer).AssemblyQualifiedName;
        [Range(0f, 1f), Tooltip("Master volume to set when the game is first started.")]
        public float DefaultMasterVolume = 1f;
        [Range(0f, 1f), Tooltip("BGM volume to set when the game is first started.")]
        public float DefaultBgmVolume = 1f;
        [Range(0f, 1f), Tooltip("SFX volume to set when the game is first started.")]
        public float DefaultSfxVolume = 1f;
        [Range(0f, 1f), Tooltip("Voice volume to set when the game is first started.")]
        public float DefaultVoiceVolume = 1f;
        [Tooltip("When enabled, each [@print] command will attempt to play an associated voice clip.")]
        public bool EnableAutoVoicing;
        [Tooltip("When auto voicing is enabled, controls method to associate voice clips with @print commands:" +
                 "\n • Text ID — Voice clips are associated by localizable text IDs. Removing, adding or re-ordering scenario script lines won't break the associations. Modifying printed text will break associations unless stable text identification is enabled." +
                 "\n • Playback Spot — Voice clips are associated by script name, line and inline indexes (playback spot). Removing, adding or re-ordering scenario script lines will break the associations. Modifying printed text will not break associations.")]
        public AutoVoiceMode AutoVoiceMode = AutoVoiceMode.TextId;
        [Tooltip("Dictates how to handle concurrent voices playback:" +
                 "\n • Allow Overlap — Concurrent voices will be played without limitation." +
                 "\n • Prevent Overlap — Prevent concurrent voices playback by stopping any played voice clip before playing a new one." +
                 "\n • Prevent Character Overlap — Prevent concurrent voices playback per character; voices of different characters (auto voicing) and any number of [@voice] command are allowed to be played concurrently.")]
        public VoiceOverlapPolicy VoiceOverlapPolicy = VoiceOverlapPolicy.PreventOverlap;
        [Tooltip("Assign localization tags to allow selecting voice language in the game settings independently of the main localization.")]
        public List<string> VoiceLocales;
        [Tooltip("Default duration of the volume fade in/out when starting or stopping playing audio.")]
        public float DefaultFadeDuration = .35f;

        [Header("Audio Mixer")]
        [Tooltip("Audio mixer to control audio groups. When not provided, will use a default one.")]
        public AudioMixer CustomAudioMixer;
        [Tooltip("Path of the mixer's group to control master volume.")]
        public string MasterGroupPath = "Master";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control master volume.")]
        public string MasterVolumeHandleName = "Master Volume";
        [Tooltip("Path of the mixer's group to control volume of background music.")]
        public string BgmGroupPath = "Master/BGM";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control background music volume.")]
        public string BgmVolumeHandleName = "BGM Volume";
        [Tooltip("Path of the mixer's group to control sound effects music volume.")]
        public string SfxGroupPath = "Master/SFX";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control sound effects volume.")]
        public string SfxVolumeHandleName = "SFX Volume";
        [Tooltip("Path of the mixer's group to control voice volume.")]
        public string VoiceGroupPath = "Master/Voice";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control voice volume.")]
        public string VoiceVolumeHandleName = "Voice Volume";

        /// <summary>
        /// Generates auto voice clip (local) resource path based on specified playback spot.
        /// </summary>
        public static string GetAutoVoiceClipPath (PlaybackSpot spot)
        {
            return string.Format(PlaybackSpotVoiceTemplate, spot.ScriptName, spot.LineNumber, spot.InlineIndex);
        }

        /// <summary>
        /// Generates auto voice clip (local) resource path based on specified localizable text;
        /// returns empty when the text doesn't contain localizable parts.
        /// </summary>
        public static string GetAutoVoiceClipPath (LocalizableText text)
        {
            for (int i = 0; i < text.Parts.Count; i++)
                if (!text.Parts[i].PlainText)
                    return $"{text.Parts[i].Script}/{text.Parts[i].Id}";
            return string.Empty;
        }

        /// <summary>
        /// Generates auto voice clip (local) resource path based on specified localizable text parameter;
        /// returns null when the text doesn't contain localizable parts.
        /// </summary>
        public static string GetAutoVoiceClipPath (LocalizableTextParameter param)
        {
            if (!Command.Assigned(param)) return null;
            if (param.DynamicValue)
            {
                if (param.RawValue.HasValue)
                    foreach (var part in param.RawValue.Value.Parts)
                        if (part.Kind == ParameterValuePartKind.IdentifiedText)
                            return $"{param.PlaybackSpot?.ScriptName}/{part.Id}";
                return null;
            }
            return GetAutoVoiceClipPath(param.Value);
        }
    }
}
