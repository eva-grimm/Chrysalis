using Chrysalis.Enums;
using Chrysalis.Services.Interfaces;

namespace Chrysalis.Services
{
    public class FileService : IFileService
    {
        private readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        private readonly string _defaultBTUserImageSrc = "/img/DefaultUserImage.png";
        private readonly string _defaultCompanyImageSrc = "/img/DefaultCompanyImage.png";
        private readonly string _defaultProjectImageSrc = "/img/DefaultProjectImage.png";

        public string ConvertByteArrayToFile(byte[]? fileData, string? extension, DefaultImage fileType)
        {
            if (fileData == null || fileData.Length == 0)
            {
                switch (fileType)
                {
                    case DefaultImage.BTUserImage: return _defaultBTUserImageSrc;
                    case DefaultImage.CompanyImage: return _defaultCompanyImageSrc;
                    case DefaultImage.ProjectImage: return _defaultProjectImageSrc;
                }
            }

            try
            {
                string fileBase64Data = Convert.ToBase64String(fileData!);
                return string.Format($"data:{extension};base64,{fileBase64Data}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file)
        {
            try
            {
                MemoryStream memoryStream = new();
                await file.CopyToAsync(memoryStream);
                byte[] byteFile = memoryStream.ToArray();
                memoryStream.Close();
                memoryStream.Dispose();

                return byteFile;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string FormatFileSize(long bytes)
        {
            int counter = 0;
            decimal fileSize = bytes;
            while (Math.Round(fileSize / 1024) >= 1)
            {
                fileSize /= bytes;
                counter++;
            }
            return string.Format("{0:n1}{1}", fileSize, suffixes[counter]);
        }

        public string GetFileIcon(string? file)
        {
            string fileImage = "default";

            if (!string.IsNullOrWhiteSpace(file))
            {
                fileImage = Path.GetExtension(file).Replace(".", "");
                return $"/img/filetype/{fileImage}.png";
            }
            return fileImage;
        }
    }
}