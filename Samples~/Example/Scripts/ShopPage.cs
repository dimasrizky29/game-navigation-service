using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YatechStudio.Navigation.Core;

namespace YatechStudio.Navigation.Samples
{
    public class ShopPage : BasePagePanel
    {
        [SerializeField] private Button detailButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            if (detailButton != null) detailButton.onClick.AddListener(OnDetail);
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            if (backButton != null) backButton.onClick.AddListener(Back);
        }

        private void OnDisable()
        {
            if (detailButton != null) detailButton.onClick.RemoveListener(OnDetail);
            if (buyButton != null) buyButton.onClick.RemoveListener(OnBuy);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
        }

        private void OnDetail() => SwitchSubPanel("Detail");
        private void OnBuy() => _navigationService.NavigateTo("Confirm").Forget();
    }
}