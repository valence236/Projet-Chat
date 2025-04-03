using System.IO;

namespace ChatAppFrontend.Services
{
    public static class SessionManager
    {
        public static string? Token { get; set; }
        public static string? Username { get; set; }

        public static void Clear()
        {
            Token = null;
            Username = null;
        }

        public static void Logout()
        {
            Clear();

            // Si tu utilises un fichier de session (comme session.json), on le supprime
            if (File.Exists("session.json"))
            {
                File.Delete("session.json");
            }
        }
    }
}
