
using ArtistTool.Domain;

namespace ArtistTool.Services
{
    public class ProjectManager(IPhotoDatabase db) : IProjectManager
    {
        private readonly string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(ArtistTool));

        private string projectsPath => Path.Combine(appPath, "Projects");

        private string PhotoPath(string photoId) => Path.Combine(projectsPath, photoId);
    
        private string ReportsPath(string photoId) => Path.Combine(PhotoPath(photoId), "Reports");

        private string ReportPath(string photoId, int reportNumber ) => Path.Combine(ReportsPath(photoId), $"{reportNumber:00000}");

        private void ValidateApplicationDirectory()
        {
            if (!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
            }

            if (!Directory.Exists(projectsPath))
            {
                Directory.CreateDirectory(projectsPath);
            }
        }

        private void ValidateProjectDirectory(string photoId)
        {
            ValidateApplicationDirectory();
            var projectDir = PhotoPath(photoId);
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }
        }   

        private void ValidateReportsDirectory(string photoId)
        {
            ValidateProjectDirectory(photoId);
            var reportsDir = ReportsPath(photoId);
            if (!Directory.Exists(reportsDir))
            {
                Directory.CreateDirectory(reportsDir);
            }
        }   

        private void ValidateReportDirectory(string photoId, int reportNumber)
        {
            ValidateReportsDirectory(photoId);
            var reportDir = ReportPath(photoId, reportNumber);
            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }
        }

        public Task<int[]> GetExistingReportsAsync(string photoId)
        {
            ValidateReportsDirectory(photoId);
            var reportsFolder = new DirectoryInfo(ReportsPath(photoId));
            return Task.FromResult<int[]>([..reportsFolder.EnumerateDirectories().Select(d => int.Parse(d.Name))]);
        }

        public string GetFilenameForPhoto(string? medium = null) => 
            medium is null ? "photo.jpg" : $"photo_{medium}.jpg";

        public string GetProjectDirectory(string photoId) => PhotoPath(photoId);

        public async Task<(ReadOnlyMemory<byte> file, string mediaType)> GetReportAssetAsync(string photoId, int reportNumber, string filename)
        {
            var file = Path.Combine(ReportPath(photoId, reportNumber), filename);

            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Report asset not found", filename);
            }

            var mediaType = Path.GetExtension(file) switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
            
            return (await File.ReadAllBytesAsync(file), mediaType);
        }

        public string GetReportDirectory(string photoId, int reportNumber) => ReportPath(photoId, reportNumber);

        public Task InitProjectAsync(string photoId)
        {
            ValidateProjectDirectory(photoId);
            return Task.CompletedTask;
        }

        public async Task<int> InitReportAsync(string photoId)
        {
            var reports = await GetExistingReportsAsync(photoId);
            var nextReportNumber = reports.Length == 0 ? 1 : reports.Max() + 1;
            ValidateReportDirectory(photoId, nextReportNumber);
            var photo = await db.GetPhotographWithIdAsync(photoId) ?? throw new InvalidOperationException("Photo not found in database");
            var photoBytes = await File.ReadAllBytesAsync(photo.Path);
            var photoReportName = GetFilenameForPhoto();
            await WriteReportAssetAsync(photoId, nextReportNumber, photoReportName, photoBytes);
            return nextReportNumber;
        }

        public async Task<string> WriteReportAssetAsync(string photoId, int reportNumber, string filename, byte[] infoToWrite)
        {
            var file = Path.Combine(ReportPath(photoId, reportNumber), filename);
            await File.WriteAllBytesAsync(file, infoToWrite);
            return file;
        }

        public async Task<string> WriteReportAssetAsync(string photoId, int reportNumber, string filename, string infoToWrite)
        {
            var file = Path.Combine(ReportPath(photoId, reportNumber), filename);
            await File.WriteAllTextAsync(file, infoToWrite);
            return file;
        }

        public async Task WriteReportInfoAsync(string photoId, int reportId, string html, string css)
        {
            await WriteReportAssetAsync(photoId, reportId, "index.html", html);
            await WriteReportAssetAsync(photoId, reportId, "index.css", css);
        }
    }
}
