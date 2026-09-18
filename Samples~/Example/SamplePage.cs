using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YatechStudio.Navigation.Core;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Samples
{
    /// <summary>
    /// Sample page that navigates to another route when its button is clicked.
    /// Copy this file into a scene with a <see cref="ScenePageContainer"/> and a Canvas/Button.
    /// </summary>
    public sealed class SamplePage : BasePagePanel
    {
        [SerializeField] private Button navigateButton;
        [SerializeField] private string targetRouteId = "Lobby";

        private void OnEnable()
        {
            if (navigateButton != null)
                navigateButton.onClick.AddListener(OnNavigateClicked);
        }

        private void OnDisable()
        {
            if (navigateButton != null)
                navigateButton.onClick.RemoveListener(OnNavigateClicked);
        }

        private void OnNavigateClicked()
        {
            _navigationService.NavigateTo(targetRouteId).Forget();
        }

        public override async UniTask Show(System.Threading.CancellationToken token = default)
        {
            await base.Show(token);
            // Custom entrance animation or logic here.
        }
    }
}
