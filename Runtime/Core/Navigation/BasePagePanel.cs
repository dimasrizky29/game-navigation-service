using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Core
{
    /// <summary>
    /// Base class for a route destination that lives inside a scene. Handles fade transitions
    /// and nested sub-panel navigation.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePagePanel : MonoBehaviour, IPagePanel
    {
        [Header("Transition Settings")]
        [SerializeField] protected float fadeDuration = 0.15f;

        [Tooltip("Sub-panel shown when the page opens, and the id used as the root of the sub-panel stack.")]
        [SerializeField] protected string defaultSubPanelId = "Main";

        protected CanvasGroup _canvasGroup;
        protected readonly Dictionary<string, ISubPanel> _subPanelCache = new Dictionary<string, ISubPanel>();
        protected readonly Stack<string> _history = new Stack<string>();
        protected string _currentSubId;

        private CanvasGroupFader _fader;

        [Inject] protected INavigationService _navigationService;
        [Inject] protected INavigationAudioBridge _audioBridge;

        protected string DefaultSubPanelId => string.IsNullOrEmpty(defaultSubPanelId) ? "Main" : defaultSubPanelId;

        public bool IsActive => gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0.9f;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _fader = new CanvasGroupFader(_canvasGroup, fadeDuration);
            EnsureSubPanelsInitialized();
        }

        protected virtual void OnDestroy()
        {
            _fader?.Cancel();
        }

        /// <summary>
        /// Discovers and initialises every <see cref="ISubPanel"/> in the hierarchy. Safe to call
        /// repeatedly — the cache is only built once.
        /// </summary>
        protected void EnsureSubPanelsInitialized()
        {
            if (_subPanelCache.Count > 0) return;

            var children = GetComponentsInChildren<ISubPanel>(true);
            foreach (var sub in children)
            {
                if (sub == null) continue;

                var id = sub.SubPanelId;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[Nav] A sub-panel under '{name}' has an empty SubPanelId and is ignored.", this);
                    continue;
                }

                if (_subPanelCache.ContainsKey(id))
                {
                    Debug.LogWarning($"[Nav] Duplicate SubPanelId '{id}' under '{name}'. The first one wins.", this);
                    continue;
                }

                sub.Initialize(this);
                _subPanelCache.Add(id, sub);
                sub.SetVisibilityImmediate(false);
            }
        }

        // ---------------------------------------------------------------- IPagePanel

        public virtual void SetPayload(object payload)
        {
            EnsureSubPanelsInitialized();
            if (_subPanelCache.Count == 0) return;

            var targetId = payload as string;
            SwitchSubPanel(string.IsNullOrEmpty(targetId) ? DefaultSubPanelId : targetId, true);
        }

        public virtual async UniTask Show(CancellationToken token = default)
        {
            gameObject.SetActive(true);

            if (_canvasGroup.blocksRaycasts && _canvasGroup.alpha >= 1f)
            {
                _canvasGroup.interactable = true;
                return;
            }

            _canvasGroup.blocksRaycasts = true;

            if (string.IsNullOrEmpty(_currentSubId) && _subPanelCache.ContainsKey(DefaultSubPanelId))
                SwitchSubPanel(DefaultSubPanelId, false);

            await _fader.FadeAsync(1f, token);
            _canvasGroup.interactable = true;
        }

        public virtual async UniTask Hide(CancellationToken token = default)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            await _fader.FadeAsync(0f, token);

            _history.Clear();
            _currentSubId = null;
            gameObject.SetActive(false);
        }

        public virtual void SetVisibilityImmediate(bool isVisible)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _fader?.ApplyImmediate(isVisible);
            gameObject.SetActive(isVisible);
        }

        public virtual void SwitchSubPanel(string subPanelId, bool addToHistory = true)
        {
            EnsureSubPanelsInitialized();

            if (subPanelId == DefaultSubPanelId)
            {
                if (addToHistory && !string.IsNullOrEmpty(_currentSubId) && _currentSubId != DefaultSubPanelId)
                    _history.Push(_currentSubId);

                _currentSubId = DefaultSubPanelId;
                foreach (var sub in _subPanelCache.Values)
                    sub.SetVisibilityImmediate(false);

                return;
            }

            if (!_subPanelCache.TryGetValue(subPanelId, out var target))
            {
                Debug.LogWarning($"[Nav] SubPanel '{subPanelId}' not found under '{name}'.", this);
                return;
            }

            if (addToHistory && _currentSubId != subPanelId)
                _history.Push(string.IsNullOrEmpty(_currentSubId) ? DefaultSubPanelId : _currentSubId);

            _currentSubId = subPanelId;

            foreach (var sub in _subPanelCache.Values)
            {
                if (ReferenceEquals(sub, target))
                    sub.Show().Forget(OnSubPanelError);
                else
                    sub.SetVisibilityImmediate(false);
            }
        }

        public virtual void Back()
        {
            _audioBridge?.PlayBackSound();

            if (_history.Count > 0)
            {
                SwitchSubPanel(_history.Pop(), false);
                return;
            }

            _navigationService.GoBack().Forget(OnSubPanelError);
        }

        public virtual bool TryBackSubPanel()
        {
            if (_history.Count == 0) return false;

            _audioBridge?.PlayBackSound();
            SwitchSubPanel(_history.Pop(), false);
            return true;
        }

        private static void OnSubPanelError(System.Exception e)
            => Debug.LogException(e);
    }
}
