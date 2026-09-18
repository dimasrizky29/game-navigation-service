namespace YatechStudio.Navigation.Infrastructure
{
    /// <summary>
    /// Immutable snapshot of a navigation position, used for history / back-stack restoration.
    /// </summary>
    public readonly struct RouteState
    {
        public readonly string RouteId;
        public readonly string Environment;
        public readonly object Payload;

        public RouteState(string routeId, string environment = "", object payload = null)
        {
            RouteId = routeId;
            Environment = environment;
            Payload = payload;
        }
    }
}
