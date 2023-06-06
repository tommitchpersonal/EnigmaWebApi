using System.Net.WebSockets;
using System.Text;

public class WebSocketService : IWebSocketService
{
    public async Task RunWebSocketAsync(WebSocket ws, IEnigmaMachine enigmaMachine)
    {
        var buffer = new byte[1];

        var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var msgArray = Encoding.UTF8.GetChars(buffer);

            foreach (var character in msgArray)
            {
                var cipherChar = enigmaMachine.EncryptCharacter(character);
                var msgToSend = Encoding.UTF8.GetBytes(cipherChar.ToString());
                await ws.SendAsync(new ArraySegment<byte>(msgToSend, 0, msgToSend.Length), receiveResult.MessageType, receiveResult.EndOfMessage, CancellationToken.None);

                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
    }
}