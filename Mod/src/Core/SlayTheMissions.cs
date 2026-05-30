using Godot;
using System;
using System.Text;

namespace SlayTheMissions.Core;

public static class SlayTheMissionsMode
{
    private const string ConfigPath = "user://SlayTheMissions/config.cfg";

    public static string CompetitionCode { get; set; } = string.Empty;
    
    public static string PlayerUUID { get; private set;} = string.Empty;
    public static string PlayerName { get; set; } = string.Empty;
    public static string DisplayName { get; set; } = string.Empty;

    public static bool IsCompetitionMode { get; set; } = false;
    public static bool HasJoinedCompetition { get; set; } = false;

    public static void InitPlayer()
    {
        var config = new ConfigFile();

        if (config.Load(ConfigPath) == Error.Ok && config.HasSectionKey("Player", "UUID"))
        {
            PlayerUUID = config.GetValue("Player", "UUID").AsString();
        }
        else
        {
            PlayerUUID = Guid.NewGuid().ToString("N");
            config.SetValue("Player", "UUID", PlayerUUID);

            config.Save(ConfigPath);
        }

        if (config.HasSectionKey("Player", "Name"))
        {
            PlayerName = config.GetValue("Player", "Name").AsString();
        }
    }

    public static bool SaveName(string name) 
    { 
        var cleaned = SanitizeName(name); 
        if (string.IsNullOrEmpty(cleaned))
        {
            return false; 
        }
        PlayerName = cleaned; 

        var config = new ConfigFile(); 
        config.Load(ConfigPath); 
        config.SetValue("Player", "Name", PlayerName); 
        return config.Save(ConfigPath) == Error.Ok;
    }

    public static void Reset()
    {
        CompetitionCode = string.Empty;
        DisplayName = string.Empty;

        HasJoinedCompetition = false;
        IsCompetitionMode = false;
    }

    public static string SanitizeName(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (char c in input.Trim())
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= 0xAC00 && c <= 0xD7A3) || c == '_')
                sb.Append(c);
        string r = sb.ToString(); return r.Length > 16 ? r.Substring(0, 16) : r;
    }
}