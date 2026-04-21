using System;

namespace AI_TestMaker.DB.Login
{
    public static class Session
    {
        public static int UserId { get; set; }
        public static string Username { get; set; }
        public static bool IsGuest { get; set; }
        public static int GuestTestsUsed { get; set; }
        public static User CurrentUser { get; set; }

        public static bool IsLogged => UserId > 0;

        public static void StartGuest()
        {
            UserId = 0;
            Username = "Invitado";
            IsGuest = true;
            GuestTestsUsed = 0;

            CurrentUser = new User
            {
                Id = 0,
                Username = "Invitado",
                Nombre = "Invitado",
                Foto = null
            };
        }

        public static void Logout()
        {
            UserId = 0;
            Username = null;
            IsGuest = false;
            GuestTestsUsed = 0;
            CurrentUser = null;
        }
    }
}
