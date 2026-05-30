namespace SlayTheMissions.Network;

public class JoinRoomResponse
{
    public bool Success { get; set; }
    public string PlayerUUID { get; set; } = "";
    public string Message { get; set; } = "";
}