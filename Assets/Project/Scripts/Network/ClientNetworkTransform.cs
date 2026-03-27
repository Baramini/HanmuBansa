using Unity.Netcode.Components;

// Override NetworkTransform to give movement authority to the client.
// This allows the owner client to move their tank without
// the server overwriting the position.
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        // -- Return false: client has authority over their own position --
        return false;
    }
}