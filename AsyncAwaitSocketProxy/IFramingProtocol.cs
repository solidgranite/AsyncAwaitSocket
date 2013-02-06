namespace AsyncAwaitSocketProxy
{
    public interface IFramingProtocol
    {
        byte StartFrame { get; set; }

        byte EndFrame { get; set; }

        byte[] FrameMessage(byte[] messageBytes);

        string UnframeMessage(string message);
    }
}