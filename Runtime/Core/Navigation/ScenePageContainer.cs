using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Core
{
    /// <summary>
    /// Registers a scene's local pages with the <see cref="INavigationService"/>. Place one per
    /// scene that hosts <see cref="BasePagePanel"/> instances, then map each RouteId to its panel.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ScenePageContainer : MonoBehaviour, IPageContainer
    {
        [Serializable]
        public struct RoutePanelMap
        {
            [Tooltip("Must match a RouteId in the NavigationConfig.")]
            public string routeId;

            public BasePagePanel panel;
        }

        [SerializeField] private List<RoutePanelMap> panels = new List<RoutePanelMap>();

        private readonly Dictionary<string, IViewPanel> _map = new Dictionary<string, IViewPanel>();
        private INavigationService _navService;

        public IReadOnlyList<RoutePanelMap> PanelConfigs => panels;

        private void Awake()
        {
            BuildMap();
        }

        [Inject]
        public void Construct(INavigationService navService)
        {
            _navService = navService;
            _navService.RegisterView(this);
        }

        public void HideAllPanels()
        {
            foreach (var panel in _map.Values)
                panel.SetVisibilityImmediate(false);
        }

        public IViewPanel GetPanel(string routeId)
        {
            if (string.IsNullOrEmpty(routeId)) return null;
            return _map.TryGetValue(routeId, out var panel) ? panel : null;
        }

        private void BuildMap()
        {
            _map.Clear();

            foreach (var item in panels)
            {
                if (item.panel == null)
                {
                    Debug.LogWarning($"[Nav] ScenePageContainer '{name}' has an entry with no panel assigned.", this);
                    continue;
                }

                if (string.IsNullOrEmpty(item.routeId))
                {
                    Debug.LogWarning($"[Nav] ScenePageContainer '{name}' has a panel '{item.panel.name}' with no RouteId.", this);
                    continue;
                }

                if (_map.ContainsKey(item.routeId))
                {
                    Debug.LogWarning($"[Nav] ScenePageContainer '{name}' maps RouteId '{item.routeId}' more than once. The first panel wins.", this);
                    continue;
                }

                _map.Add(item.routeId, item.panel);
                item.panel.SetVisibilityImmediate(false);
            }
        }
    }
}
