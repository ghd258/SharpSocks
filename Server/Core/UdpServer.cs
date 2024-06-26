﻿using System.Net;
using System.Net.Sockets;

namespace Server.Core;

internal class UdpServer
{
    private readonly List<IPEndPoint> _proxyPointList = new();
    private readonly Func<IPEndPoint, byte[], Task> _callBackAsync;
    public IPEndPoint ClientPoint { get; set; }
    public TcpClient TcpClient { get; set; }
    public UdpClient UdpClient { get; set; }
    public UdpServer(IPEndPoint ipEndpoint, TcpClient tcpClient, Func<IPEndPoint, byte[], Task> callBackFunc)
    {
        ClientPoint = ipEndpoint;
        TcpClient = tcpClient;
        UdpClient = new UdpClient(0);
        _callBackAsync = callBackFunc;
        _ = UdpRecieveAsync();
    }

    /// <summary>
    /// UDP接收回调
    /// </summary>
    /// <param name="ar"></param>
    private async Task UdpRecieveAsync()
    {
        try
        {
            while (true)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var receiveInfo = await UdpClient.ReceiveAsync(cts.Token);
                if (receiveInfo.Buffer.Length > 0)
                {
                    if (_proxyPointList.Contains(receiveInfo.RemoteEndPoint))
                    {
                        var header = GetUdpHeader(receiveInfo.RemoteEndPoint);
                        var sendData = header.Concat(receiveInfo.Buffer).ToArray();
                        await _callBackAsync(ClientPoint, EncodeBytes(sendData));
                    }
                }
                else
                {
                    UdpClient.Dispose();
                    break;
                }
            }

        }
        catch (SocketException)
        {

        }
    }

    public async Task UdpSendAsync(IPEndPoint ipEndpoint, byte[] data)
    {
        await UdpClient.SendAsync(data, data.Length, ipEndpoint);
        _proxyPointList.Add(ipEndpoint);
    }

    /// <summary>
    /// 关闭UDP对象及TCP依赖
    /// </summary>
    public void Close()
    {
        try
        {
            WriteLog($"Close the udp proxy tunnel to {ClientPoint}");
            UdpClient.Close();
            TcpClient.GetStream().Close();
            TcpClient.Close();
        }
        catch (Exception)
        {

        }
    }

    ~UdpServer()
    {
        UdpClient.Dispose();
        TcpClient.Dispose();
    }
}
