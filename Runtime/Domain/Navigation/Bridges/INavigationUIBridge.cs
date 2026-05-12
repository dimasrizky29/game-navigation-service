using UnityEngine;

public interface INavigationUIBridge
{
    void TransitionPanel(bool show, string routeId);
    void UpdateTransitionProgress(float progress);
    Transform GetLayer(NavType navType);
}
