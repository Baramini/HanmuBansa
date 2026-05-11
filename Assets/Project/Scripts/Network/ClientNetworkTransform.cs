using Unity.Netcode.Components;

// Override NetworkTransform
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        // Client has authority their own position
        return false;
    }
}