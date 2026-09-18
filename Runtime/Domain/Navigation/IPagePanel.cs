namespace YatechStudio.Navigation.Domain
{
    public interface IPagePanel : IViewPanel
    {
        void SetPayload(object payload);

        void SwitchSubPanel(string subPanelId, bool addToHistory = true);

        void Back();

        bool TryBackSubPanel();
    }
}
