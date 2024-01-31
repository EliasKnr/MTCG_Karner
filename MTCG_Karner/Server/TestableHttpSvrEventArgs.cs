using System.Net.Sockets;

namespace MTCG_Karner.Server;

public class TestableHttpSvrEventArgs : HttpSvrEventArgs
{
    public string ResponseData { get; private set; }
    public int ResponseStatusCode { get; private set; }

    public TestableHttpSvrEventArgs(TcpClient client, string plainMessage)
        : base(client, plainMessage)
    {
    }

    public override void Reply(int status, string? payload = null)
    {
        this.ResponseStatusCode = status;
        this.ResponseData = payload;
        // Here you can also log or handle the response as needed for testing.
    }
}