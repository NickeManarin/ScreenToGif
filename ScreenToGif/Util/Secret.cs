using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    public static class Secret
    {
        public static string Email { get; set; } = "screentogif@outlook.com";

        public static string Password { get; set; } = "xD{S02r(n>-VO";

        public static int Port { get; set; } = 587; //Or 25

        public static string Host { get; set; } = "smtp-mail.outlook.com ";
    }
}
