using System.Collections.Generic;
using UnityEngine;

namespace YatechStudio.Navigation.Infrastructure
{
    /// <summary>
    /// Single source of truth for every navigable destination in the application.
    /// Create one via <b>Assets &gt; Create &gt; Yatech Studio &gt; Navigation &gt; Navigation Config</b>.
    /// </summary>
    [CreateAssetMenu(fileName = "NavigationConfig", menuName = "Yatech Studio/Navigation/Navigation Config")]
    public class NavigationConfig : ScriptableObject
    {
        [Header("Default Routes")]
        [Tooltip("Route used when a navigation is blocked by the auth guard.")]
        public string LoginRouteId = "Login";

        [Tooltip("Route used after a successful sign-in when no route was pending.")]
        public string DefaultRouteId = "MainMenu";

        [Header("Scene Loading")]
        [Tooltip("How long to wait for the loaded scene's ScenePageContainer to register itself.")]
        [Min(1f)]
        public float SceneRegisterTimeoutSeconds = 10f;

        [Tooltip("Extra delay after a scene has registered, to let transition visuals settle. Set to 0 to disable.")]
        [Min(0f)]
        public float SceneTransitionDelaySeconds = 2f;

        [Header("All Routes")]
        public List<RouteDefinition> Routes = new List<RouteDefinition>();

        private Dictionary<string, RouteDefinition> _lookup;

        /// <summary>
        /// O(1) route lookup. The dictionary is built on first access and cached. Call
        /// <see cref="InvalidateCache"/> after editing <see cref="Routes"/> at runtime.
        /// </summary>
        public IReadOnlyDictionary<string, RouteDefinition> Lookup
        {
            get
            {
                if (_lookup != null) return _lookup;

                _lookup = new Dictionary<string, RouteDefinition>(Routes.Count);
                foreach (var route in Routes)
                {
                    if (route == null || string.IsNullOrWhiteSpace(route.RouteId)) continue;
                    if (_lookup.ContainsKey(route.RouteId))
                    {
                        Debug.LogWarning($"[Nav] Duplicate RouteId '{route.RouteId}' in {name}. The first entry wins.", this);
                        continue;
                    }

                    _lookup.Add(route.RouteId, route);
                }

                return _lookup;
            }
        }

        public bool TryGet(string routeId, out RouteDefinition definition)
        {
            if (string.IsNullOrEmpty(routeId))
            {
                definition = null;
                return false;
            }

            return Lookup.TryGetValue(routeId, out definition);
        }

        public void InvalidateCache() => _lookup = null;

        /// <summary>Kept for backwards compatibility. Prefer <see cref="Lookup"/>.</summary>
        public Dictionary<string, RouteDefinition> ToDictionary()
        {
            var dict = new Dictionary<string, RouteDefinition>();
            foreach (var pair in Lookup) dict[pair.Key] = pair.Value;
            return dict;
        }

        private void OnValidate()
        {
            _lookup = null;

            foreach (var route in Routes)
            {
                if (route != null && !route.IsValid(out var reason))
                    Debug.LogWarning($"[Nav] {name}: {reason}", this);
            }
        }
    }
}
