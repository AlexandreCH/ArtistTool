using System;
using System.Collections.Generic;
using System.Text;

namespace ArtistTool.Services
{
    public interface IProjectManager
    {
        Task InitProjectAsync(string photoId);
        Task<int> InitReportAsync(string photoId);
        string GetProjectDirectory(string photoId);
        string GetReportDirectory(string photoId, int reportNumber);
        Task<int[]> GetExistingReportsAsync(string photoId);
        Task WriteReportInfoAsync(string photoId, int reportId, string html, string css);
        Task<(ReadOnlyMemory<byte> file, string mediaType)> GetReportAssetAsync(string photoId, int reportNumber, string filename);
        Task<string> WriteReportAssetAsync(string photoId, int reportNumber, string filename, byte[] infoToWrite);
        Task<string> WriteReportAssetAsync(string photoId, int reportNumber, string filename, string infoToWrite);
        string GetFilenameForPhoto(string? medium = null);
    }
}
