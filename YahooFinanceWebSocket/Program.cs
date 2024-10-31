using ProtoBuf;
using System;
using System.Net.WebSockets;
using System.Text;

namespace YahooFinanceWebSocket
{
    internal class Program
    {
        static async Task Main()
        {
            using ClientWebSocket client = new();
            using CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(1200));

            try
            {
                await client.ConnectAsync(new Uri("wss://streamer.finance.yahoo.com/"), cts.Token);

                if (client.State == WebSocketState.Open)
                {
                    Console.WriteLine("Connection successful.");

                    string message = """{"subscribe":["SPY", "INVE-B.ST", "^OMX", "SBB-B.ST"]}""";
                    byte[] bytesToSend = Encoding.UTF8.GetBytes(message);
                    await client.SendAsync(bytesToSend, WebSocketMessageType.Text, true, cts.Token);

                    byte[] responseBuffer = new byte[1024];
                    while (true)
                    {
                        WebSocketReceiveResult response = await client.ReceiveAsync(responseBuffer, cts.Token);
                        string responseMessage = Encoding.UTF8.GetString(responseBuffer, 0, response.Count);
                        //Console.WriteLine($"Message: {responseMessage}");

                        byte[] bytes = Convert.FromBase64String(responseMessage);
                        PricingData y = Serializer.Deserialize<PricingData>((ReadOnlySpan<byte>)bytes);

                        Console.WriteLine($"{y.Id}: {y.Price}, at {DateTimeOffset.FromUnixTimeMilliseconds(y.Time).TimeOfDay}");
                    }
                }
            }
            catch (WebSocketException e) { Console.WriteLine(e.Message); }
        }
    }
}