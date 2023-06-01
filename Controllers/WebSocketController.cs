using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using System.Text;

[Route("Controller")]
public class WebSocketController : ControllerBase
{

    private IEnigmaMachine _enigmaMachine;
    private IRandomSettingsGenerator _settingsGenerator;
    public WebSocketController(IEnigmaMachine enigmaMachine, IRandomSettingsGenerator settingsGenerator)
    {
        _enigmaMachine = enigmaMachine;
        _settingsGenerator = settingsGenerator;
    }

    [HttpGet]
    [Route("/ws")]
    public async Task WebsocketEncryption()
    {
        try
        {
            if (_enigmaMachine.GetSettings() == null)
            {
                _enigmaMachine.UpdateSettings(_settingsGenerator.GenerateRandomSettings());
            }

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await RunWebSocket(ws);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }

            _enigmaMachine.Reset();
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            HttpContext.Response.StatusCode = 500;
        }
    }

    private async Task RunWebSocket(WebSocket ws)
    {
        
        var buffer = new byte[1];

        var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var msgArray = Encoding.UTF8.GetChars(buffer);

            foreach (var character in msgArray)
            {
                var cipherChar = _enigmaMachine.EncryptCharacter(character);
                var msgToSend = Encoding.UTF8.GetBytes(cipherChar.ToString());
                await ws.SendAsync(new ArraySegment<byte>(msgToSend, 0, msgToSend.Length), receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);

                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
    }
}