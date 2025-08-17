using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetworkDiscoveryApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverCommand))]
    private int _targetPort = 9999;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverCommand))]
    private bool _isDiscovering;
    [ObservableProperty]
    private string _statusMessage = "准备就绪";
    [ObservableProperty]
    private string _log = "";
    [ObservableProperty]
    private ObservableCollection<DeviceInfo> _discoveredDevices = new();
    public bool IsNotDiscovering => !IsDiscovering;

    // 设备发现事件
    public event EventHandler<string> DeviceDiscovered;

    public MainViewModel()
    {
        DiscoveredDevices = new();
    }
    [RelayCommand(CanExecute = nameof(CanDiscover))]
    private async Task DiscoverAsync()
    {
        try
        {
            IsDiscovering = true;
            DiscoveredDevices.Clear();
            StatusMessage = "搜索中...";
            Log = $"开始搜索端口 {TargetPort} 的设备...\n";

            Log += "发现请求已发送\n";

            // 开始监听响应
            //_ = ListenForResponsesAsync(_discoverCts.Token);
            DiscoveredDevices.Add(new DeviceInfo("demo"));

            // 设置10秒超时
            await Task.Delay(TimeSpan.FromSeconds(10));

            StatusMessage = $"发现完成，找到 {DiscoveredDevices.Count} 台设备";
            Log += $"设备发现完成，共找到 {DiscoveredDevices.Count} 台设备\n";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "搜索超时";
            Log += "搜索操作超时\n";
        }
        catch (Exception ex)
        {
            StatusMessage = "发生错误";
            Log += $"错误: {ex.Message}\n";
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private bool CanDiscover() => !IsDiscovering && TargetPort > 0 && TargetPort <= 65535;

    private void OnDeviceDiscovered(string ip)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DiscoveredDevices.Add(new DeviceInfo(ip));
        });
        DeviceDiscovered?.Invoke(this, ip);
    }
    [RelayCommand]
    private void ClearResults()
    {
        DiscoveredDevices.Clear();
        Log = "";
        StatusMessage = "已清除结果";
    }
}
public partial class DeviceInfo : ObservableObject
{
    [ObservableProperty]
    private string _ipAddress;

    [ObservableProperty]
    private DateTime _timestamp;

    public DeviceInfo(string ip)
    {
        IpAddress = ip;
        Timestamp = DateTime.Now;
    }
}
