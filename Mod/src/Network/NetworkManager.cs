#nullable enable
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SlayTheMissions.Network;

public class NetworkManager
{
    public static NetworkManager Instance { get; set; } = new();

    private readonly HttpClient _httpClient = new();
    private ClientWebSocket? _webSocket;

    private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    
    private NetworkManager()
    {
        
    }
    
    public const string ServerURL = "http://localhost:5000";
    public const string WebSocketURL = "ws://localhost:5000/ws";

    public event Action<string>? MessageReceived;

    public async Task<string> PostAsync(string endpoint, object body)
    {
        string json = JsonSerializer.Serialize(body);

        var response = await _httpClient.PostAsync($"{ServerURL}{endpoint}", new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetAsync(string endpoint)
    {
        return await _httpClient.GetStringAsync($"{ServerURL}{endpoint}");
    }

    public async Task ConnectAsync(string roomCode, string playerUUID)
    {
        _webSocket = new ClientWebSocket();

        await _webSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

        await SendAsync(new
        {
            type = "auth",
            roomCode,
            playerUUID
        });

        _ = Task.Run(ReceiveLoop);
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket == null)
        {
            return;
        }

        try
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "leave", CancellationToken.None);
            }
        }
        catch
        {
        }

        _webSocket.Dispose();
        _webSocket = null;
    }

    public async Task SendAsync<T>(T packet)
    {
        if (_webSocket == null)
        {
            return;
        }

        if (_webSocket.State != WebSocketState.Open)
        {
            return;
        }

        string json = JsonSerializer.Serialize(packet);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[4096];

        while (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            MessageReceived?.Invoke(json);
        }
    }
}