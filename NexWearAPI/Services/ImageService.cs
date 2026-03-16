using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace NexWearAPI.Services
{
    public interface IImageService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string imageUrl);
    }

    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        // Lista de tipos de archivos permitidos - A04 OWASP
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Estándares que permiten el intercambio de diversos tipos de archivos
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
        
        // Máximo permitido 5MB
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImageService(IConfiguration config)
        {
            var cloudName = config["Cloudinary:CloudName"]!;
            var apiKey = config["Cloudinary:ApiKey"]!;
            var apiSecret = config["Cloudinary:ApiSecret"]!;

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Siempre utilizar HTTPS
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            // A04 - Validar tipo y tamaño ed archivo
            if (!IsValidFile(file)) return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "nexwear/products",
                Transformation = new Transformation()
                    .Width(800)
                    .Height(800)
                    .Crop("limit") // No agranda, solo reduce si es mayor
                    .Quality("auto") // Optimización automática
                    .FetchFormat("auto") // Formato para el navegador
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            // Si hubo error en la subida
            if (result.Error is not null) return null;

            return result.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            // Extraer el PublicId desde la URL de cloudinary
            var publicId = ExtractPublicId(imageUrl);

            if (publicId is null) return false;

            var deleteParams = new DeletionParams(publicId);

            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        // Validaciones para A04 OWASP
        private bool IsValidFile(IFormFile file)
        {
            // Verificar que no este vacío
            if (file is null || file.Length == 0) return false;

            // Verificar tamaño máximo
            if (file.Length > MaxFileSizeBytes) return false;

            // Verificar el extensión del archivo
            var extension = Path.GetExtension(file.FileName.ToLowerInvariant());
            if (!_allowedExtensions.Contains(extension)) return false;

            // Verificar MIME type - doble vcalidación para evitar archivos renombrados
            if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant())) return false;

            return true;
        }

        // Extraer PublicId de la URL de Cloudinary
        private static string? ExtractPublicId(string imageUrl)
        {
            try
            {
                // Buscar "/upload" en la URL y tomar el contenido
                var uploadIndex = imageUrl.IndexOf("/upload", StringComparison.Ordinal);

                if (uploadIndex == -1) return null;

                var afterUpload = imageUrl[(uploadIndex + 8)..]; // Saltar "/upload"

                // Saltar la versión si existe (v1234567 /)
                if (afterUpload.StartsWith("v") && afterUpload.Contains("/"))
                    afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];

                // Quitar la extensión
                var lastDot = afterUpload.LastIndexOf('.');
                return lastDot != -1 ? afterUpload[..lastDot] : afterUpload;
            }
            catch
            {
                return null;
            }
        }
    }
}
