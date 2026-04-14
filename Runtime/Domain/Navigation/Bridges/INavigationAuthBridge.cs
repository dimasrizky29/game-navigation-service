using System;

public interface INavigationAuthBridge
{
    bool IsAuthenticated { get; }
    event Action<bool> OnAuthenticationChanged; 
}
