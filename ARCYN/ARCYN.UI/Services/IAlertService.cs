namespace ARCYN.UI.Services;

public interface IAlertService
{
    void Show(string message, string title = "ARCYN");
}
