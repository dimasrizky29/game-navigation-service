using Cysharp.Threading.Tasks;

namespace YatechStudio.Navigation.Domain
{
    public interface INavigationService
    {
        string CurrentRoute { get; }

        UniTask GoBack();
        bool CanGoBack { get; }
        UniTask<bool> HandleHardwareBack();

        UniTask NavigateTo(string targetRouteId, string additionalNameScene = "", object payload = null);
        UniTask NavigateToInEnv(string routeId, string env, object payload = null);
        void RegisterView(IPageContainer view);
    }
}
