namespace YatechStudio.Navigation.Domain
{
    /// <summary>
    /// Bridge the host application implements to play UI sounds triggered by navigation.
    /// </summary>
    public interface INavigationAudioBridge
    {
        void PlayBackSound();
    }
}
