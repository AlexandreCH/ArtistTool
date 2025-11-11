using System.ComponentModel;

namespace ArtistTool.Services
{
    [Description("Registry of information about supporting images for a main photograph.")]
    public interface IImageRegistry
    {
        public void RegisterImage(string filename, string title, string description);
        [Description("Gets a list of all registered image titles.")]
        public string[] GetTitles();
        [Description("Gets image information by title.")]
        public string GetImageInformationByTitle(string title);
        [Description("Gets image information by filename.")]
        public string GetImageInformationByFilename(string filename);
    }
}
