using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YatechStudio.Navigation.Core;

namespace YatechStudio.Navigation.Samples
{
    public class MainMenuPage : BasePagePanel
    {
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        private void OnEnable()
        {
            if (shopButton != null) shopButton.onClick.AddListener(OnShop);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        }

        private void OnDisable()
        {
            if (shopButton != null) shopButton.onClick.RemoveListener(OnShop);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        }

        private void OnShop() => _navigationService.NavigateTo("Shop").Forget();
        private void OnSettings() => _navigationService.NavigateTo("Settings").Forget();

        public override async UniTask Show(System.Threading.CancellationToken token = default)
        {
            await base.Show(token);
            Debug.Log("[Sample] MainMenu shown.");
        }
    }
}