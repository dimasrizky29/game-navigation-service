using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YatechStudio.Navigation.Core
{
    /// <summary>
    /// Fades a <see cref="CanvasGroup"/>. Each fader owns a single in-flight fade: starting a new
    /// fade cancels the previous one, so rapid Show/Hide calls can never race over the alpha value.
    /// </summary>
    internal sealed class CanvasGroupFader
    {
        private readonly CanvasGroup _group;
        private CancellationTokenSource _fadeCts;
        private float _duration;

        public CanvasGroupFader(CanvasGroup group, float defaultDuration)
        {
            _group = group;
            _duration = defaultDuration;
        }

        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0f, value);
        }

        public float Alpha => _group != null ? _group.alpha : 0f;

        public void Cancel()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;
        }

        public async UniTask FadeAsync(float targetAlpha, CancellationToken external = default)
        {
            if (_group == null) return;

            Cancel();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(external);

            if (_duration <= 0f)
            {
                _group.alpha = targetAlpha;
                return;
            }

            var startAlpha = _group.alpha;

            try
            {
                var elapsed = 0f;
                while (elapsed < _duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _duration);
                    await UniTask.Yield(PlayerLoopTiming.Update, _fadeCts.Token);
                }

                _group.alpha = targetAlpha;
            }
            catch (System.OperationCanceledException)
            {
                // Superseded by a newer fade — the newer fade owns the final alpha.
            }
        }

        public void ApplyImmediate(bool isVisible)
        {
            Cancel();
            if (_group == null) return;

            _group.alpha = isVisible ? 1f : 0f;
            _group.blocksRaycasts = isVisible;
            _group.interactable = isVisible;
        }
    }
}
