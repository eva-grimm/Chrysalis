using Chrysalis.Enums;

namespace Chrysalis.Services.Interfaces
{
    public interface IFileService
    {
        public Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file);
        public string ConvertByteArrayToFile(byte[]? fileData, string? extension, DefaultImage imageType);
        public string FormatFileSize(long bytes);
        public string GetFileIcon(string? file);
    }
}