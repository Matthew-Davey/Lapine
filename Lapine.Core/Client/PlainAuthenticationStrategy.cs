namespace Lapine.Client;

using static System.Text.Encoding;

public class PlainAuthenticationStrategy(String username = "guest", String password = "guest") : IAuthenticationStrategy {
    public String Mechanism => "PLAIN";

    public const String DefaultUsername = "guest";
    public const String DefaultPassword = "guest";

    public String Username { get; } = username;
    public String Password { get; } = password;

    public Byte[] Respond(Byte stage, in ReadOnlySpan<Byte> challenge) =>
        UTF8.GetBytes($"\0{Username}\0{Password}");
}
