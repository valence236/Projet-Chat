namespace ChatAppFrontend.Services
{
    public static class SessionManager
    {
        public static string Token { get; set; }
        public static int UserId { get; set; }
        public static string Username { get; set; }

        public static void Clear()
        {
            Token = null;
            UserId = 0;
            Username = null;
        }
    }
}
