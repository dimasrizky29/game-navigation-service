using UnityEngine;

namespace YatechStudio.Navigation.Domain
{
    /// <summary>
    /// Bridge the host application implements to supply UI layers and transition feedback.
    /// </summary>
    public interface INavigationUIBridge
    {
        void TransitionPanel(bool show, string routeId);
        void UpdateTransitionProgress(float progress);
        Transform GetLayer(NavType navType);
    }
}
