namespace ProdeMundial.Web
{
    public class UserSession
    {
        public string CurrentUser { get; set; } = string.Empty; // Usuario por defecto vacio
        public event Action? OnUserChanged;

        public void ChangeUser(string newUser)
        {
            CurrentUser = newUser;
            OnUserChanged?.Invoke();
        }
    }
}
