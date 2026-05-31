namespace ProdeMundial.Web.Services
{
    using Microsoft.AspNetCore.Components.Forms;

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadLogoAsync(IBrowserFile file)
        {
            // Solo permitimos imágenes
            if (!file.ContentType.StartsWith("image/")) return null;

            var folderPath = Path.Combine(_env.WebRootPath, "logos");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // Nombre único para evitar sobrescribir
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var path = Path.Combine(folderPath, fileName);

            using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 2); // Max 2MB
            using var fs = new FileStream(path, FileMode.Create);
            await stream.CopyToAsync(fs);

            return $"/logos/{fileName}"; // Retornamos la ruta relativa para la DB
        }
    }
}
