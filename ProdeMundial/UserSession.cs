using ProdeMundial.Domain.Entities;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ProdeMundial.Web
{
      

public class UserSession
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        public bool IsSuperAdmin => CurrentUser != null && CurrentUser.CompanyId == 1 && (CurrentUser.Id == 1 || CurrentUser.Name == "Sebastian");

        public UserSession(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        // Mantenemos tus propiedades intactas
        public AppUser? CurrentUser { get; set; }
        public int CurrentCompanyId { get; set; }
        public event Action? OnUserChanged;
        public bool IsAuthenticated => CurrentUser != null && CurrentCompanyId != 0;

        // Cambiamos tu Login para que sea asíncrono y guarde en el navegador de forma segura
        public async Task LoginAsync(AppUser user, int companyId)
        {
            CurrentUser = user;
            CurrentCompanyId = companyId;

            // Guardamos ambos datos en el SessionStorage del navegador
            await _sessionStorage.SetAsync("user_session_data", user);
            await _sessionStorage.SetAsync("user_company_id", companyId);

            OnUserChanged?.Invoke();
        }

        // Cambiamos tu Logout para que limpie el navegador
        public async Task LogoutAsync()
        {
            CurrentUser = null;
            CurrentCompanyId = 0;

            await _sessionStorage.DeleteAsync("user_session_data");
            await _sessionStorage.DeleteAsync("user_company_id");

            OnUserChanged?.Invoke();
        }

        // 🔥 EL NUEVO MÉTODO SALVAVIDAS: Rehidrata la sesión si Blazor la vació en memoria
        public async Task EnsureUserLoadedAsync()
        {
            // Si ya están en memoria, no hacemos nada (es ultra rápido)
            if (CurrentUser != null && CurrentCompanyId != 0) return;

            try
            {
                // Intentamos recuperar los datos desde el navegador
                var userResult = await _sessionStorage.GetAsync<AppUser>("user_session_data");
                var companyResult = await _sessionStorage.GetAsync<int>("user_company_id");

                if (userResult.Success && userResult.Value != null && companyResult.Success)
                {
                    CurrentUser = userResult.Value;
                    CurrentCompanyId = companyResult.Value;
                }
            }
            catch
            {
                // Protege el renderizado previo (Prerendering) si el navegador no está listo
                CurrentUser = null;
                CurrentCompanyId = 0;
            }
        }
    }

}

