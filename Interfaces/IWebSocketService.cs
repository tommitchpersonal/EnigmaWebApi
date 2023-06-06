using System.Net.WebSockets;

public interface IWebSocketService
{
    public Task RunWebSocketAsync(WebSocket ws, IEnigmaMachine enigmaMachine);
}