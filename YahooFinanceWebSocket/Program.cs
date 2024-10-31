using ProtoBuf;
using System;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
namespace YahooFinanceWebSocket;

internal class Program
{
    static async Task Main()
    {
        IObservable<PricingData> obs = CreateYahooFinanceObservable(["SPY", "EURUSD=X"]);
        IDisposable d = obs.Subscribe(x => Console.WriteLine($"{x.Id}: {x.Price}, at {DateTimeOffset.FromUnixTimeMilliseconds(x.Time).TimeOfDay}"));
        await Task.Delay(10000);
        d.Dispose();
        Console.Write("disposed");
        await Task.Delay(5000);
    }

    public static IObservable<PricingData> CreateYahooFinanceObservable(IEnumerable<string> symbols)
    {
        byte[] requestMessage = CreateRequestMessage(symbols);
        byte[] responseBuffer = new byte[1024];

        return Observable.Create<PricingData>(async (observer, ct) =>
        {
            ClientWebSocket client = new();
            await client.ConnectAsync(new Uri("wss://streamer.finance.yahoo.com/"), ct);
            if (client.State != WebSocketState.Open)
                observer.OnError(new Exception("Connection failed."));

            await client.SendAsync(requestMessage, WebSocketMessageType.Text, true, ct);
            try
            {
                while (true)
                {
                    WebSocketReceiveResult response = await client.ReceiveAsync(responseBuffer, ct);
                    string responseMessage = Encoding.UTF8.GetString(responseBuffer, 0, response.Count);
                    byte[] bytes = Convert.FromBase64String(responseMessage);
                    PricingData y = Serializer.Deserialize<PricingData>((ReadOnlySpan<byte>)bytes);
                    observer.OnNext(y);
                }
            }
            catch (Exception e) 
            { 
                observer.OnError(e); 
            }

            return Disposable.Create(() =>
            {
                client.Dispose();
            });

        }).Publish().RefCount();
    }

    private static byte[] CreateRequestMessage(IEnumerable<string> symbols)
    {
        StringBuilder sb = new();
        sb.Append("{\"subscribe\":[");
        IEnumerable<string> symbolsList = symbols.Select(symbol => $"\"{symbol}\"");
        sb.AppendJoin(", ", symbolsList);
        sb.Append("]}");
        string message = sb.ToString();
        return Encoding.UTF8.GetBytes(message);
    }
}
