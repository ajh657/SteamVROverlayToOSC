using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Buffers.Binary;

namespace SteamVROverlayToOSC;

public sealed class OSCClient : IDisposable
{
    private readonly object _sync = new();
    private UdpClient? _client;
    private IPEndPoint _endpoint;

    public OSCClient() : this("127.0.0.1", 9000) { }

    public OSCClient(string host, int port)
    {
        Configure(host, port);
    }

    public void Configure(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("OSC host is required.", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var addresses = Dns.GetHostAddresses(host);
        var address = addresses.FirstOrDefault(ip =>
            ip.AddressFamily == AddressFamily.InterNetwork) ??
            addresses.FirstOrDefault(ip =>
                ip.AddressFamily == AddressFamily.InterNetworkV6);

        if (address is null)
            throw new ArgumentException("OSC host could not be resolved.", nameof(host));

        lock (_sync)
        {
            _endpoint = new IPEndPoint(address, port);
            _client ??= address.AddressFamily == AddressFamily.InterNetworkV6
                ? new UdpClient(AddressFamily.InterNetworkV6)
                : new UdpClient(AddressFamily.InterNetwork);
        }
    }

    public async Task SendAsync(string address, params object[] arguments)
    {
        if (string.IsNullOrWhiteSpace(address) || address[0] != '/')
            throw new ArgumentException("OSC address must start with '/'.", nameof(address));

        var packet = Encode(address, arguments);
        UdpClient client;
        IPEndPoint endpoint;
        lock (_sync)
        {
            client = _client ?? throw new InvalidOperationException("OSC client is not configured.");
            endpoint = _endpoint;
        }

        await client.SendAsync(packet, packet.Length, endpoint);
    }

    internal static byte[] Encode(string address, object[] arguments)
    {
        using var output = new MemoryStream();
        WritePaddedString(output, address);

        var types = new StringBuilder(",");
        foreach (var argument in arguments)
        {
            types.Append(argument switch
            {
                int => 'i',
                float => 'f',
                double => 'd',
                long => 'h',
                string => 's',
                bool value => value ? 'T' : 'F',
                _ => throw new ArgumentException(
                    $"Unsupported OSC argument type: {argument?.GetType().Name ?? "null"}")
            });
        }
        WritePaddedString(output, types.ToString());

        foreach (var argument in arguments)
        {
            switch (argument)
            {
                case int value:
                    Span<byte> bytes = stackalloc byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(bytes, value);
                    output.Write(bytes);
                    break;
                case float value:
                    Span<byte> bytes = stackalloc byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(bytes, BitConverter.SingleToInt32Bits(value));
                    output.Write(bytes);
                    break;
                case double value:
                    Span<byte> bytes = stackalloc byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(bytes, BitConverter.DoubleToInt64Bits(value));
                    output.Write(bytes);
                    break;
                case long value:
                    Span<byte> bytes = stackalloc byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(bytes, value);
                    output.Write(bytes);
                    break;
                case string value:
                    WritePaddedString(output, value);
                    break;
            }
        }

        return output.ToArray();
    }

    private static void WritePaddedString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
        stream.WriteByte(0);
        while ((stream.Length % 4) != 0)
            stream.WriteByte(0);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    private IPEndPoint _endpoint = new(IPAddress.Loopback, 9000);
}
