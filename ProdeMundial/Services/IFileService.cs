using Microsoft.AspNetCore.Components.Forms;

namespace ProdeMundial.Web.Services
{
    public interface IFileService
    {
        Task<string> UploadLogoAsync(IBrowserFile file);
    }
}
