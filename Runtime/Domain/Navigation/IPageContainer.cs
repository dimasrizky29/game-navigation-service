namespace YatechStudio.Navigation.Domain
{
    public interface IPageContainer
    {
        void HideAllPanels();
        IViewPanel GetPanel(string routeId);
    }
}
