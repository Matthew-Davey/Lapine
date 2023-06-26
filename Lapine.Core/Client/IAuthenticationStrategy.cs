namespace Lapine.Client;

public interface IAuthenticationStrategy {
    String Mechanism { get; }

    Byte[] Respond(Byte stage, in ReadOnlySpan<Byte> challenge);
}
