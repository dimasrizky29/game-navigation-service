using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Samples
{
    public class ConfirmPopupPanel : MonoBehaviour, IViewPanel
    {
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        public bool IsActive => gameObject.activeSelf;

        private void OnEnable()
        {
            if (yesButton != null) yesButton.onClick.AddListener(OnYes);
            if (noButton != null) noButton.onClick.AddListener(OnNo);
        }

        private void OnDisable()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(OnYes);
            if (noButton != null) noButton.onClick.RemoveListener(OnNo);
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

        private void OnYes()
        {
            Debug.Log("[Sample] Purchase confirmed.");
            Hide().Forget();
        }

        private void OnNo() => Hide().Forget();
    }
}