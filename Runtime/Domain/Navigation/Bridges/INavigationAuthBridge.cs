using System;

namespace YatechStudio.Navigation.Domain
{
    /// <summary>
    /// Bridge the host application implements to expose authentication state to the auth guard.
    /// </summary>
    public interface INavigationAuthBridge
    {
        bool IsAuthenticated { get; }

        /// <summary>Raised with <c>true</c> on sign-in and <c>false</c> on sign-out.</summary>
        event Action<bool> OnAuthenticationChanged;
    }
}
