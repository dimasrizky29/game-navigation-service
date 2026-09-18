using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Infrastructure
{
    /// <summary>
    /// A single navigation destination. Can be authored inline inside a
    /// <see cref="NavigationConfig"/> list, or as a standalone asset when a route needs
    /// to be referenced from several configs.
    /// </summary>
    [CreateAssetMenu(fileName = "Route", menuName = "Yatech Studio/Navigation/Route Definition")]
    public class RouteDefinition : ScriptableObject
    {
        [Tooltip("Stable string identifier used by NavigateTo, e.g. \"Shop\".")]
        public string RouteId;

        [Tooltip("Behaviour of this route when it is navigated to.")]
        public RouteFlags Flags;

        [Tooltip("How the destination is presented. Determines which fields below are used.")]
        public NavType Type;

        [Tooltip("Scene name, used by ScenePage routes.")]
        public string TargetScene;

        [Tooltip("Addressable prefab, used by Window and Popup routes.")]
        public AssetReference Prefab;

        public bool HasFlag(RouteFlags flag) => (Flags & flag) != 0;

        /// <summary>
        /// True when the route carries the minimum data its <see cref="Type"/> requires.
        /// </summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(RouteId))
            {
                reason = "RouteId is empty.";
                return false;
            }

            if (Type == NavType.ScenePage)
            {
                if (string.IsNullOrWhiteSpace(TargetScene))
                {
                    reason = $"ScenePage route '{RouteId}' has no TargetScene.";
                    return false;
                }
            }
            else if (Prefab == null || !Prefab.RuntimeKeyIsValid())
            {
                reason = $"{Type} route '{RouteId}' has no valid Prefab reference.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
