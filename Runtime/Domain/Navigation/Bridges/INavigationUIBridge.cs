using UnityEngine;

public interface INavigationUIBridge
{
    void TransitionPanel(bool show, string routeId);
    Transform GetLayer(NavType navType);
}
