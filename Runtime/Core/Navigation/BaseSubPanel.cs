using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Core
{
    /// <summary>
    /// Base class for a panel nested inside a <see cref="BasePagePanel"/> (tabs, edit dialogs,
    /// sub-screens). Owns its own fade so concurrent switches cannot race over the alpha value.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BaseSubPanel : MonoBehaviour, ISubPanel
    {
        [Header("Sub Panel Settings")]
        [SerializeField] private string subPanelId;

        [SerializeField] protected float fadeDuration = 0.1f;

        protected IPagePanel _parent;
        protected CanvasGroup _canvasGroup;

        private CanvasGroupFader _fader;

        public string SubPanelId => subPanelId;

        public bool IsActive => gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0.9f;

        public void Initialize(IPagePanel parent)
        {
            _parent = parent;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                _fader = new CanvasGroupFader(_canvasGroup, fadeDuration);
            }
        }

        protected virtual void OnDestroy() => _fader?.Cancel();

        public virtual async UniTask Show(CancellationToken token = default)
        {
            gameObject.SetActive(true);

            EnsureFader();
            if (_canvasGroup.blocksRaycasts && _canvasGroup.alpha >= 1f)
            {
                _canvasGroup.interactable = true;
                return;
            }

            _canvasGroup.blocksRaycasts = true;
            await _fader.FadeAsync(1f, token);
            _canvasGroup.interactable = true;
        }

        public virtual async UniTask Hide(CancellationToken token = default)
        {
            EnsureFader();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            await _fader.FadeAsync(0f, token);
            gameObject.SetActive(false);
        }

        public virtual void SetVisibilityImmediate(bool isVisible)
        {
            EnsureFader();
            _fader.ApplyImmediate(isVisible);
            gameObject.SetActive(isVisible);
        }

        /// <summary>Override to receive the payload routed to this sub-panel.</summary>
        public virtual void SetPayload(object payload) { }

        private void EnsureFader()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_fader == null) _fader = new CanvasGroupFader(_canvasGroup, fadeDuration);
        }
    }
}
