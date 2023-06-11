using System.Net.WebSockets;
using System.Text;
using log4net;

public class WebSocketService : IWebSocketService
{
    private ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

    public async Task RunWebSocketAsync(WebSocket ws, IEnigmaMachine enigmaMachine)
    {
        _log.Info("WebSocket connection started");
        var buffer = new byte[1];

        var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            _log.Info("WebSocket received message");
            var msgArray = Encoding.UTF8.GetChars(buffer);

            foreach (var character in msgArray)
            {
                var cipherChar = enigmaMachine.EncryptCharacter(character);
                var msgToSend = Encoding.UTF8.GetBytes(cipherChar.ToString());
                _log.Info("WebSocket sending encoded character");
                await ws.SendAsync(new ArraySegment<byte>(msgToSend, 0, msgToSend.Length), receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);
            }

            receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        _log.Info("Websocket closed");
    }
}