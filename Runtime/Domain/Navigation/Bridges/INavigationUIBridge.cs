using UnityEngine;

public interface INavigationUIBridge
{
    void ShowLoading(bool show);
    Transform GetLayer(NavType navType);
}
