namespace Lapine.Client;

public interface IAuthenticationStrategy {
    String Mechanism { get; }

    ReadOnlySpan<Byte> Respond(Byte stage, in ReadOnlySpan<Byte> challenge);
}
