namespace YatechStudio.Navigation.Domain
{
    public interface ISubPanel : IViewPanel
    {
        string SubPanelId { get; }

        void Initialize(IPagePanel parent);
    }
}
