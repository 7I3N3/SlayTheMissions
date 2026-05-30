#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using SlayTheMissions.Network;

namespace SlayTheMissions.Core;

public static class CompetitionManager
{
    public static event Action? Joined;
    public static event Action? Left;
    public static event Action<string>? Kicked;

    public static async Task<bool> JoinAsync(string code)
    {
        var result = await NetworkManager.Instance.PostAsync("/api/codes/join", new
        {
            code,
            playerUUID = SlayTheMissionsMode.PlayerUUID,
            name = SlayTheMissionsMode.PlayerName
        });

        var root = JsonDocument.Parse(result).RootElement;
        bool success = root.GetProperty("success").GetBoolean();
        if (!success)
        {
            return false;
        }

        SlayTheMissionsMode.CompetitionCode = code;
        SlayTheMissionsMode.HasJoinedCompetition = true;

        await NetworkManager.Instance.ConnectAsync(code, SlayTheMissionsMode.PlayerUUID);

        Joined?.Invoke();

        return true;
    }

    public static async Task LeaveAsync()
    {
        await NetworkManager.Instance.PostAsync("/api/codes/leave", new
        {
            playerUUID = SlayTheMissionsMode.PlayerUUID
        });

        await NetworkManager.Instance.DisconnectAsync();

        SlayTheMissionsMode.Reset();
        
        Left?.Invoke();
    }

    public static async Task SendMissionClear(string missionID)
    {
        await NetworkManager.Instance.SendAsync(new
        {
            type = "mission_clear",
            missionID
        });
    }

    public static void Initialize()
    {
        NetworkManager.Instance.MessageReceived += OnSocketMessage;
    }

    private static void OnSocketMessage(string json)
    {
        using var doc = JsonDocument.Parse(json);
        
        string type = doc.RootElement.GetProperty("type").GetString() ?? "";
        switch (type)
        {
            case "kick":
                {
                    string reason = doc.RootElement.GetProperty("reason").GetString() ?? "";

                    Kicked?.Invoke(reason);

                    break;
                }
        }
    }
}