using System;
using UnityEngine;

namespace YatechStudio.Navigation
{
    /// <summary>
    /// Default <see cref="INavigationLogger"/> implementation backed by <see cref="Debug"/>.
    /// All messages are prefixed with <c>[Nav]</c> for easy console filtering.
    /// </summary>
    public sealed class UnityNavigationLogger : INavigationLogger
    {
        public static readonly UnityNavigationLogger Instance = new UnityNavigationLogger();

        private const string Prefix = "[Nav] ";

        public void Info(string message) => Debug.Log(Prefix + message);

        public void Warning(string message) => Debug.LogWarning(Prefix + message);

        public void Error(string message) => Debug.LogError(Prefix + message);

        public void Exception(Exception exception, string message) => Debug.LogException(exception);
    }

    /// <summary>
    /// Logger that discards everything. Used when the package must stay completely silent.
    /// </summary>
    public sealed class NullNavigationLogger : INavigationLogger
    {
        public static readonly NullNavigationLogger Instance = new NullNavigationLogger();

        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
        public void Exception(Exception exception, string message) { }
    }
}
