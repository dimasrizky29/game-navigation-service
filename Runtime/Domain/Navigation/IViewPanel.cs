using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YatechStudio.Navigation.Domain
{
    public interface IViewPanel
    {
        GameObject gameObject { get; }

        UniTask Show(CancellationToken token = default);
        UniTask Hide(CancellationToken token = default);

        void SetVisibilityImmediate(bool isVisible);

        bool IsActive { get; }
    }
}
