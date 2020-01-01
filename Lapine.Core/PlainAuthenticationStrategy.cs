namespace Lapine {
    using System;

    using static System.Text.Encoding;

    public class PlainAuthenticationStrategy : IAuthenticationStrategy {
        public String Mechanism => "PLAIN";

        public const String DefaultUsername = "guest";
        public const String DefaultPassword = "guest";

        public String Username { get; }
        public String Password { get; }

        public PlainAuthenticationStrategy(String username = DefaultUsername, String password = DefaultPassword) {
            Username = username;
            Password = password;
        }

        public ReadOnlySpan<Byte> Respond(Byte stage, in ReadOnlySpan<Byte> challenge) =>
            UTF8.GetBytes($"\0{Username}\0{Password}");
    }
}
