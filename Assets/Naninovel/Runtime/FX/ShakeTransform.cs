// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.Commands;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="Transform"/>.
    /// </summary>
    public abstract class ShakeTransform : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        public virtual string SpawnedPath { get; private set; }
        public virtual string ObjectName { get; private set; }
        public virtual int ShakesCount { get; private set; }
        public virtual float ShakeDuration { get; private set; }
        public virtual float DurationVariation { get; private set; }
        public virtual float ShakeAmplitude { get; private set; }
        public virtual float AmplitudeVariation { get; private set; }
        public virtual bool ShakeHorizontally { get; private set; }
        public virtual bool ShakeVertically { get; private set; }

        protected virtual int DefaultShakesCount => defaultShakesCount;
        protected virtual float DefaultShakeDuration => defaultShakeDuration;
        protected virtual float DefaultDurationVariation => defaultDurationVariation;
        protected virtual float DefaultShakeAmplitude => defaultShakeAmplitude;
        protected virtual float DefaultAmplitudeVariation => defaultAmplitudeVariation;
        protected virtual bool DefaultShakeHorizontally => defaultShakeHorizontally;
        protected virtual bool DefaultShakeVertically => defaultShakeVertically;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();
        protected virtual Vector3 DeltaPos { get; private set; }
        protected virtual Vector3 InitialPos { get; private set; }
        protected virtual Transform ShakenTransform { get; private set; }
        protected virtual bool Loop { get; private set; }
        protected virtual Tweener<VectorTween> PositionTweener { get; } = new Tweener<VectorTween>();
        protected virtual CancellationTokenSource CTS { get; private set; }

        [SerializeField] private int defaultShakesCount = 3;
        [SerializeField] private float defaultShakeDuration = .15f;
        [SerializeField] private float defaultDurationVariation = .25f;
        [SerializeField] private float defaultShakeAmplitude = .5f;
        [SerializeField] private float defaultAmplitudeVariation = .5f;
        [SerializeField] private bool defaultShakeHorizontally;
        [SerializeField] private bool defaultShakeVertically = true;

        public virtual void SetSpawnParameters (IReadOnlyList<string> parameters, bool asap)
        {
            if (PositionTweener.Running)
                PositionTweener.CompleteInstantly();
            if (ShakenTransform != null)
                ShakenTransform.position = InitialPos;

            SpawnedPath = gameObject.name;
            ObjectName = parameters?.ElementAtOrDefault(0);
            ShakesCount = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantInt() ?? DefaultShakesCount);
            ShakeDuration = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? DefaultShakeDuration);
            DurationVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? DefaultDurationVariation);
            ShakeAmplitude = Mathf.Abs(parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? DefaultShakeAmplitude);
            AmplitudeVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? DefaultAmplitudeVariation);
            ShakeHorizontally = bool.Parse(parameters?.ElementAtOrDefault(6) ?? DefaultShakeHorizontally.ToString());
            ShakeVertically = bool.Parse(parameters?.ElementAtOrDefault(7) ?? DefaultShakeVertically.ToString());
            Loop = ShakesCount <= 0;
        }

        public virtual async UniTask AwaitSpawnAsync (AsyncToken asyncToken = default)
        {
            ShakenTransform = GetShakenTransform();
            if (!ShakenTransform)
            {
                SpawnManager.DestroySpawned(SpawnedPath);
                Engine.Warn($"Failed to apply `{GetType().Name}` FX to `{ObjectName}`: transform to shake not found.");
                return;
            }

            asyncToken = InitializeCTS(asyncToken);
            InitialPos = ShakenTransform.position;
            DeltaPos = new Vector3(ShakeHorizontally ? ShakeAmplitude : 0, ShakeVertically ? ShakeAmplitude : 0, 0);

            if (Loop) LoopRoutine(asyncToken).Forget();
            else
            {
                for (int i = 0; i < ShakesCount; i++)
                    await ShakeSequenceAsync(asyncToken);
                if (SpawnManager.IsSpawned(SpawnedPath))
                    SpawnManager.DestroySpawned(SpawnedPath);
            }

            await AsyncUtils.WaitEndOfFrameAsync(asyncToken); // Otherwise consequent shake won't work.
        }

        protected abstract Transform GetShakenTransform ();

        protected virtual async UniTask ShakeSequenceAsync (AsyncToken asyncToken)
        {
            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);
            await MoveAsync(InitialPos - amplitude * .5f, duration * .25f, asyncToken);
            await MoveAsync(InitialPos + amplitude, duration * .5f, asyncToken);
            await MoveAsync(InitialPos, duration * .25f, asyncToken);
        }

        protected virtual async UniTask MoveAsync (Vector3 position, float duration, AsyncToken asyncToken)
        {
            var tween = new VectorTween(ShakenTransform.position, position, duration, pos => ShakenTransform.position = pos, false, EasingType.SmoothStep);
            await PositionTweener.RunAsync(tween, asyncToken, ShakenTransform);
        }

        protected virtual void OnDestroy ()
        {
            Loop = false;
            CTS?.Cancel();
            CTS?.Dispose();

            if (ShakenTransform != null)
                ShakenTransform.position = InitialPos;

            if (Engine.Initialized && SpawnManager.IsSpawned(SpawnedPath))
                SpawnManager.DestroySpawned(SpawnedPath);
        }

        protected virtual async UniTaskVoid LoopRoutine (AsyncToken asyncToken)
        {
            while (Loop && Application.isPlaying && asyncToken.EnsureNotCanceledOrCompleted())
                await ShakeSequenceAsync(asyncToken);
        }

        protected virtual AsyncToken InitializeCTS (AsyncToken token)
        {
            CTS?.Cancel();
            CTS?.Dispose();
            CTS = CancellationTokenSource.CreateLinkedTokenSource(token.CancellationToken);
            return new AsyncToken(CTS.Token, token.CompletionToken);
        }
    }
}
