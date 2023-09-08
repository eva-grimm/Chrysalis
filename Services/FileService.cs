using Chrysalis.Enums;
using Chrysalis.Services.Interfaces;

namespace Chrysalis.Services
{
    public class FileService : IFileService
    {
        private readonly string? _defaultBTUserImage = "";
        private readonly string? _defaultCompanyImage = "";
        private readonly string? _defaultProjectImage = "";

        public async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile? file)
        {
            try
            {
                using MemoryStream memoryStream = new();
                await file!.CopyToAsync(memoryStream);
                byte[] byteFile = memoryStream.ToArray();
                memoryStream.Close();
                return byteFile;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string? ConvertByteArrayToFile(byte[]? fileData, string? extension)
        {
            try
            {
                // TO-DO: Rework Handling Null Data
                if (fileData == null || fileData.Length == 0) return string.Empty;

                string? fileBase64Data = Convert.ToBase64String(fileData!);
                fileBase64Data = string.Format($"data:{extension};base64,{fileBase64Data}");
                return fileBase64Data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string? ConvertByteArrayToFile(byte[]? fileData, string? extension, DefaultImage defaultImage)
        {
            try
            {
                if (fileData == null || fileData.Length == 0) 
                {
                    switch (defaultImage)
                    {
                        case DefaultImage.BTUserImage: return _defaultBTUserImage;
                        case DefaultImage.CompanyImage: return _defaultCompanyImage;
                        case DefaultImage.ProjectImage: return _defaultProjectImage;
                    }
                }

                string? fileBase64Data = Convert.ToBase64String(fileData!);
                fileBase64Data = string.Format($"data:{extension};base64,{fileBase64Data}");
                return fileBase64Data;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}