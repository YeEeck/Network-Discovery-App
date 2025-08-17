using System.Net.Sockets;
using System.Net;
using System.Text;

namespace NetworkDiscovery;

public class DiscoveryClient : IDisposable
{
    private UdpClient? _udpReceiver;
    private CancellationTokenSource? _cts;
    private readonly int _discoveryPort;

    public DiscoveryClient(int discoveryPort)
    {
        _discoveryPort = discoveryPort;
    }

    public async Task StartDiscoveryAsync(int port, Action<string> onDeviceDiscovered, CancellationToken? cancellationToken = null)
    {
        StopDiscovery();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken ?? new CancellationToken());

        try
        {
            // 创建接收器（绑定任意可用端口）
            _udpReceiver = new UdpClient(port);

            // 发送DISCOVERY广播
            await SendDiscoveryBroadcast();

            // 开始接收响应
            _ = ReceiveResponsesAsync(onDeviceDiscovered, _cts.Token);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void StopDiscovery()
    {
        _cts?.Cancel();
        _udpReceiver?.Close();
        _udpReceiver = null;
    }

    private async Task SendDiscoveryBroadcast()
    {
        using var sender = new UdpClient();
        sender.EnableBroadcast = true;

        byte[] data = Encoding.ASCII.GetBytes("DISCOVERY");
        await sender.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _discoveryPort));
    }

    private async Task ReceiveResponsesAsync(Action<string> onDeviceDiscovered, CancellationToken token)
    {
        var buffer = new byte[1024];

        try
        {
            while (!token.IsCancellationRequested && _udpReceiver != null)
            {
                UdpReceiveResult result = await _udpReceiver.ReceiveAsync(token);
                string message = Encoding.ASCII.GetString(result.Buffer);

                if (message == "ACK")
                {
                    onDeviceDiscovered?.Invoke(result.RemoteEndPoint.Address.ToString());
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常终止
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        StopDiscovery();
        _cts?.Dispose();
    }
}
