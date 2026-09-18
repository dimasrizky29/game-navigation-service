using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Samples
{
    public class SettingsPanel : MonoBehaviour, IViewPanel
    {
        [SerializeField] private Button closeButton;

        public bool IsActive => gameObject.activeSelf;

        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);
        }

        public UniTask Show(CancellationToken token = default)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public UniTask Hide(CancellationToken token = default)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetVisibilityImmediate(bool isVisible) => gameObject.SetActive(isVisible);

        private void OnClose() => Hide().Forget();
    }
}