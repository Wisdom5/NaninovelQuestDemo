// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.FX;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    public abstract class VideoActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }
        protected virtual Dictionary<string, VideoAppearance> Appearances { get; } = new Dictionary<string, VideoAppearance>();
        protected virtual int TextureDepthBuffer => 24;
        protected virtual RenderTextureFormat TextureFormat => RenderTextureFormat.ARGB32;
        protected virtual string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().MasterGroupPath;

        private readonly string streamExtension;
        private readonly Tweener<FloatTween> volumeTweener = new Tweener<FloatTween>();

        private LocalizableResourceLoader<VideoClip> videoLoader;
        private string appearance;
        private bool visible;

        protected VideoActor (string id, TMeta metadata)
            : base(id, metadata)
        {
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerManager = Engine.GetService<IResourceProviderManager>();
            var localizationManager = Engine.GetService<ILocalizationManager>();
            videoLoader = new LocalizableResourceLoader<VideoClip>(
                providerManager.GetProviders(ActorMetadata.Loader.ProviderTypes), providerManager,
                localizationManager, $"{ActorMetadata.Loader.PathPrefix}/{Id}");
            videoLoader.OnLocalized += HandleAppearanceLocalized;

            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMetadata, GameObject, false);

            SetVisibility(false);
        }

        public virtual UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.BlurAsync(intensity, duration, easingType, asyncToken);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            var previousAppearance = this.appearance;
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
            {
                foreach (var app in Appearances.Values)
                    app.Video.Stop();
                return;
            }

            foreach (var kv in Appearances)
                if (!kv.Key.EqualsFast(appearance) && !kv.Key.EqualsFast(previousAppearance))
                    kv.Value.Video.Stop();

            var videoAppearance = await GetAppearanceAsync(appearance, asyncToken);
            var video = videoAppearance.Video;

            if (!video.isPrepared)
            {
                video.Prepare();
                // Player could be invalid, as we're invoking this from sync version of change appearance.
                while (asyncToken.EnsureNotCanceled(video) && !video.isPrepared)
                    await AsyncUtils.WaitEndOfFrameAsync();
                if (!video) return;
            }

            var previousTexture = video.targetTexture;
            videoAppearance.Video.targetTexture = RenderTexture.GetTemporary((int)video.width, (int)video.height, TextureDepthBuffer, TextureFormat);
            videoAppearance.Video.Play();

            foreach (var kv in Appearances)
                kv.Value.TweenVolumeAsync(kv.Key.EqualsFast(appearance) && visible ? 1 : 0, duration, asyncToken).Forget();
            await TransitionalRenderer.TransitionToAsync(video.targetTexture, duration, easingType, transition, asyncToken);
            if (!video) return;

            if (previousTexture)
                RenderTexture.ReleaseTemporary(previousTexture);
            if (previousAppearance != this.appearance)
                ReleaseResources(previousAppearance, this);
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            this.visible = visible;

            foreach (var appearance in Appearances.Values)
                appearance.TweenVolumeAsync(visible && appearance.Video.isPlaying ? 1 : 0, duration, asyncToken).Forget();
            await TransitionalRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, asyncToken);
        }

        public override async UniTask HoldResourcesAsync (string appearance, object holder)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            await GetAppearanceAsync(appearance);
            videoLoader.Hold(appearance, holder);
        }

        public override void ReleaseResources (string appearance, object holder)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            videoLoader.Release(appearance, holder);

            if (videoLoader.CountHolders(appearance) == 0 &&
                Appearances.TryGetValue(appearance, out var player))
            {
                Appearances.Remove(appearance);
                DisposeAppearancePlayer(player);
            }
        }

        public override void Dispose ()
        {
            base.Dispose();

            foreach (var videoAppearance in Appearances.Values)
            {
                if (!videoAppearance.Video) continue;
                RenderTexture.ReleaseTemporary(videoAppearance.Video.targetTexture);
                ObjectUtils.DestroyOrImmediate(videoAppearance.GameObject);
            }

            Appearances.Clear();

            if (videoLoader != null)
            {
                videoLoader.OnLocalized -= HandleAppearanceLocalized;
                videoLoader.ReleaseAll(this);
            }
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual async UniTask<VideoAppearance> GetAppearanceAsync (string videoName, AsyncToken asyncToken = default)
        {
            if (Appearances.TryGetValue(videoName, out var cached)) return cached;

            var videoPlayer = Engine.CreateObject<VideoPlayer>(videoName, parent: Transform);
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = ShouldLoopAppearance(videoName);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = PathUtils.Combine(Application.streamingAssetsPath, $"{ActorMetadata.Loader.PathPrefix}/{Id}/{videoName}") + streamExtension;
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
            }
            else
            {
                var videoClip = await videoLoader.LoadAndHoldAsync(videoName, this);
                asyncToken.ThrowIfCanceled();
                if (!videoClip.Valid) throw new Error($"Failed to load `{videoName}` resource for `{Id}` video actor. Make sure the video clip is assigned in the actor resources.");
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }

            var videoAppearance = new VideoAppearance(videoPlayer, SetupAudioSource(videoPlayer));
            Appearances[videoName] = videoAppearance;

            return videoAppearance;
        }

        protected virtual bool ShouldLoopAppearance (string appearance)
        {
            return !appearance.EndsWith("NoLoop", StringComparison.OrdinalIgnoreCase);
        }

        protected virtual AudioSource SetupAudioSource (VideoPlayer player)
        {
            var audioManager = Engine.GetService<IAudioManager>();
            var audioSource = player.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.bypassReverbZones = true;
            audioSource.bypassEffects = true;
            if (audioManager.AudioMixer && !string.IsNullOrEmpty(MixerGroup))
                audioSource.outputAudioMixerGroup = audioManager.AudioMixer.FindMatchingGroups(MixerGroup)?.FirstOrDefault();
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);
            return audioSource;
        }

        protected virtual void DisposeAppearancePlayer (VideoAppearance player)
        {
            player.Video.Stop();
            RenderTexture.ReleaseTemporary(player.Video.targetTexture);
            ObjectUtils.DestroyOrImmediate(player.GameObject);
        }

        protected virtual void HandleAppearanceLocalized (Resource<VideoClip> resource)
        {
            if (Appearance == videoLoader.GetLocalPath(resource))
                Appearance = Appearance;
        }
    }
}
