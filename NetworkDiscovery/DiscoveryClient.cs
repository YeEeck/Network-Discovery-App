using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkDiscovery;

public class DiscoveryClient : IDisposable
{
    private UdpClient? _udpReceiver;
    private CancellationTokenSource? _cts;
    private readonly int _discoveryPort;
    private Task? _receiveTask;

    public DiscoveryClient(int discoveryPort)
    {
        _discoveryPort = discoveryPort;
    }

    public async Task StartDiscoveryAsync(Action<string> onDeviceDiscovered, CancellationToken? cancellationToken = null)
    {
        StopDiscovery(); // 停止任何现有操作

        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken ?? new CancellationToken());

        try
        {
            // 创建绑定到端口的接收客户端
            _udpReceiver = new UdpClient(_discoveryPort);
            _udpReceiver.EnableBroadcast = true;

            await SendDiscoveryBroadcast();

            // 启动接收任务并存储引用
            _receiveTask = ReceiveResponsesAsync(
                onDeviceDiscovered,
                _cts.Token
            );
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void StopDiscovery()
    {
        try
        {
            _cts?.Cancel();
            _receiveTask = null;

            // 安全关闭接收套接字
            if (_udpReceiver?.Client != null)
            {
                _udpReceiver.Client.Close();
                _udpReceiver.Close();
            }
        }
        finally
        {
            _udpReceiver = null;
        }
    }

    private async Task SendDiscoveryBroadcast()
    {
        using var sender = new UdpClient();
        sender.EnableBroadcast = true;

        byte[] data = Encoding.ASCII.GetBytes("DISCOVERY");
        await sender.SendAsync(
            data,
            data.Length,
            new IPEndPoint(IPAddress.Broadcast, _discoveryPort)
        );
    }

    private async Task ReceiveResponsesAsync(
        Action<string> onDeviceDiscovered,
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 显式检查对象状态
                if (_udpReceiver == null) return;

                var result = await _udpReceiver.ReceiveAsync(token);
                string message = Encoding.ASCII.GetString(
                    result.Buffer,
                    0,
                    result.Buffer.Length
                );

                if (message == "ACK")
                {
                    onDeviceDiscovered?.Invoke(
                        result.RemoteEndPoint.Address.ToString()
                    );
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出路径
        }
        catch (ObjectDisposedException)
        {
            // 对象已释放时的安全退出
        }
    }

    public void Dispose()
    {
        StopDiscovery();
        _cts?.Dispose();
        _udpReceiver?.Dispose();
        GC.SuppressFinalize(this);
    }
}
