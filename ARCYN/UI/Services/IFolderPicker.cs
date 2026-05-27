namespace ARCYN.Core.Services
{
    public interface IFolderPicker
    {
        /// <summary>
        /// Returns selected folder path or null if user cancels.
        /// </summary>
        string? PickFolder(string title = "Select a folder");
    }
}
