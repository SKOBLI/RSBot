using RSBot.Core.Objects;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace RSBot.Core.Network
{
    public class EventServer
    {
        public async Task StartServerAsync(string pipeName)
        {
            while (true)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        Console.WriteLine($"Named Pipe Server started with name '{pipeName}'");
                        await server.WaitForConnectionAsync();
                        Console.WriteLine("Client connected.");

                        using (var writer = new StreamWriter(server) { AutoFlush = true })
                        using (var reader = new StreamReader(server))
                        {
                            await HandleClientAsync(writer, reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(StreamWriter writer, StreamReader reader)
        {
            while (true)
            {
                var message = await reader.ReadLineAsync();
                if (message == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                Console.WriteLine($"Received: {message}");

                string response;
                if (message.StartsWith("getPlayerPosition"))
                {
                    var position = Game.Player.Movement.Source;
                    response = $"X:{position.X} Y:{position.Y}";
                }
                else if (message.StartsWith("MoveTo"))
                {
                    var parts = message.Split(new[] { ' ', ':', 'X', 'Y' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 && float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y))
                    {
                        Position destination = new Position(x, y);
                        Game.Player.MoveTo(destination);
                        
                        response = $"Executed MoveTo to position ({x}, {y})";
                    }
                    else
                    {
                        response = $"Unknown response from client: {message}";
                    }
                }
                else
                {
                    response = $"Unknown command: {message}";
                }

                await writer.WriteLineAsync(response);
                Console.WriteLine($"Sent: {response}");
            }
        }
    }
}
