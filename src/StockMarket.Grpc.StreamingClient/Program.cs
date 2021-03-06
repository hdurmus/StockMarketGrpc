﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using StockMarket.Grpc.Proto;
using static StockMarket.Grpc.Proto.StockMarketService;


namespace StockMarket.Grpc.StreamingClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            
            var client = new StockMarketServiceClient(channel);

            var cts = new CancellationTokenSource();
            
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            
            using var replies = client.GetStockMarketStream(new StockStreamRequest(), cancellationToken: cts.Token);

            try
            {
                await foreach (var stockData in replies.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
                {
                    PrintStockInfo(stockData);
                }
            }
            catch  
            {
                replies.Dispose();
                Console.WriteLine("Stream cancelled.");
            }
            
            
            Console.WriteLine("Press a key to exit");

            Console.ReadKey();
        }

        static void PrintStockInfo(StockData stockData)
        {
            bool compare (string item1, string item2)
                =>
                String.Compare(item1, item2, StringComparison.OrdinalIgnoreCase) == 0;

            var symbol = stockData.Symbol; 

            var color = Console.ForegroundColor;
            if (compare(symbol, "MSFT"))
                Console.ForegroundColor = ConsoleColor.Green;
            else if (compare(symbol, "FB"))
                Console.ForegroundColor = ConsoleColor.Blue;
            else if (compare(symbol, "AAPL"))
                Console.ForegroundColor = ConsoleColor.Red;
            else if (compare(symbol, "GOOG"))
                Console.ForegroundColor = ConsoleColor.Magenta;
            else if (compare(symbol, "AMZN"))
                Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine($"Symbol {stockData.Symbol} - Date {stockData.Date.ToDateTime().ToString("MM/dd/yyyy")} - High Price {ToDecimal (stockData.DayHigh)} - Low Price {ToDecimal (stockData.DayLow)}");
            Console.ForegroundColor = color;
        }
        
        public static decimal ToDecimal(Proto.Decimal value)
        {
            return value.Units + value.Nanos / 1_000_000_000;
        }
    }
}