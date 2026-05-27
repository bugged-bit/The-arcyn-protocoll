namespace ARCYN.Platform
{
    public interface IMessageService
    {
        void ShowError(string title, string message);
        void ShowInfo(string title, string message);
    }
}