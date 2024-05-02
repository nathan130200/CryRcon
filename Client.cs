using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using static CryRcon.Constants;

namespace CryRcon;

public class Client(string host, ushort port, string password = default)
{
    static readonly WhirlpoolManaged Hasher = new();

    private readonly DnsEndPoint m_endpoint = new(host, port);
    private readonly byte[] m_password = password != null ? Encoding.UTF8.GetBytes(password) : [];

    private Socket m_socket;
    private CancellationTokenSource m_cts;
    private NetworkStream m_stream;
    private volatile bool m_disposed;
    private volatile byte m_state;

    private readonly ConcurrentQueue<byte[]> m_queue = new();
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<string>> m_callbacks = new();

    public bool IsConnected
        => !m_disposed && m_cts != null && !m_cts.IsCancellationRequested;

    public bool IsAuthenticated
        => m_state == 2;

    public async Task ConnectAsync(CancellationToken token = default)
    {
        m_socket?.Dispose();
        m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await m_socket.ConnectAsync(m_endpoint);

        m_cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        m_stream = new NetworkStream(m_socket, true);

        _ = Task.Run(BeginReceive, token);
        _ = Task.Run(BeginSend, token);
    }

    public Task<string> SendCommandAsync(string str)
    {
        uint commandId = 0;

        while (commandId == 0)
            commandId = (uint)Random.Shared.Next(int.MaxValue);

        var tcs = new TaskCompletionSource<string>();

        m_callbacks[commandId] = tcs;

        using (var ms = new MemoryStream())
        {
            ms.WriteUInt32BE(RPC_MAGIC);
            ms.WriteUInt8(RPC_CLIENT_COMMAND);
            ms.WriteUInt32BE(commandId);
            ms.WriteString(str);
            m_queue.Enqueue(ms.ToArray());
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;

        m_disposed = true;

        m_cts?.Dispose();
        m_cts?.Cancel();
        m_cts = null;

        m_socket?.Dispose();
        m_socket = null;
    }

    Task SendPacketAsync(MemoryStream pkt)
    {
        m_queue.Enqueue(pkt.ToArray());
        return Task.CompletedTask;
    }

    async Task BeginReceive()
    {
        m_state = 0;

        try
        {
            while (!m_cts.IsCancellationRequested)
            {
                await Task.Delay(1);

                if (m_stream.ReadUInt32BE() != RPC_MAGIC)
                {
                    Debug.WriteLine("err: Invalid header. Expected: RPC_MAGIC");

                    // discard data
                    for (int i = 0; i < m_socket.Available; i++)
                        _ = m_stream.ReadByte();
                }
                else
                {
                    var type = m_stream.ReadUInt8();

                    if (m_state == 0)
                    {
                        if (type == RPC_SERVER_CHALLENGE)
                        {
                            var verifyKey = m_stream.ReadUInt8Array(16);

                            var result = new byte[verifyKey.Length + m_password.Length];
                            verifyKey.CopyTo(result, 0);
                            m_password.CopyTo(result, verifyKey.Length);

                            result = Hasher.ComputeHash(result);

                            using (var ms = new MemoryStream())
                            {
                                ms.WriteUInt32BE(RPC_MAGIC);
                                ms.WriteUInt8(RPC_CLIENT_AUTH);
                                ms.Write(result);
                                await SendPacketAsync(ms);
                            }

                            m_state = 1;
                        }
                    }
                    else if (m_state == 1)
                    {
                        if (type == RPC_SERVER_AUTH_SUCCESS)
                            m_state = 2;
                        else if (type == RPC_SERVER_AUTH_FAILED)
                            throw new RpcException("Authentication failed.");
                    }
                    else if (m_state == 2)
                    {
                        if (type == RPC_SERVER_COMMAND_RESULT)
                        {
                            var cmd = m_stream.ReadUInt32BE();
                            var len = m_stream.ReadInt32LE();

                            var buf = new byte[len];

                            if (len > 0)
                                await m_stream.ReadAsync(buf);

                            if (m_callbacks.TryRemove(cmd, out var callback))
                                callback.TrySetResult(Encoding.UTF8.GetString(buf, 0, len));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Dispose();
        }
    }

    internal async Task BeginSend()
    {
        try
        {
            while (!m_cts.IsCancellationRequested)
            {
                await Task.Delay(1);

                if (m_queue.TryDequeue(out var buffer))
                    await m_stream.WriteAsync(buffer);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Dispose();
        }
    }
}
