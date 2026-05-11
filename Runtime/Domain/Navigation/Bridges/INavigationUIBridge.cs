using UnityEngine;

public interface INavigationUIBridge
{
    void TransitionPanel(bool show, string targetScene);
    Transform GetLayer(NavType navType);
}
