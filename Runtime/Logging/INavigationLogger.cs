using System;

namespace YatechStudio.Navigation
{
    /// <summary>
    /// Minimal logging contract used by the navigation package so it does not depend on any
    /// specific logging library. Supply your own implementation through the
    /// <see cref="NavigationService"/> constructor, or rely on the default
    /// <see cref="UnityNavigationLogger"/>.
    /// </summary>
    public interface INavigationLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Exception(Exception exception, string message);
    }
}
