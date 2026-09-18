namespace YatechStudio.Navigation.Domain
{
    [System.Flags]
    public enum RouteFlags
    {
        None = 0,
        RequiresAuth = 1 << 0,
        IsAdditive = 1 << 1,
        IsTransient = 1 << 2,
        ClearHistory = 1 << 3,
        BlockBack = 1 << 4
    }
}
