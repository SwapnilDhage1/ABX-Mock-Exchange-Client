using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ABXClient
{
    public class Packet
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Sequence { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int, Packet> packets = new Dictionary<int, Packet>();

            try
            {
               
                using (TcpClient client = new TcpClient("127.0.0.1", 3000))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] request = new byte[] { 1, 0 };
                    stream.Write(request, 0, request.Length);

                    byte[] buffer = new byte[17];
                    int bytesRead;

                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (bytesRead < 17) continue;

                        string symbol = Encoding.ASCII.GetString(buffer, 0, 4);
                        string side = Encoding.ASCII.GetString(buffer, 4, 1);
                        int quantity = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 5));
                        int price = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 9));
                        int sequence = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 13));

                        packets[sequence] = new Packet
                        {
                            Symbol = symbol,
                            Side = side,
                            Quantity = quantity,
                            Price = price,
                            Sequence = sequence
                        };
                    }
                }
                
                int minSeq = packets.Keys.Min();
                int maxSeq = packets.Keys.Max();

               
                for (int i = minSeq; i <= maxSeq; i++)
                {
                    if (!packets.ContainsKey(i))
                    {
                        using (TcpClient resendClient = new TcpClient("127.0.0.1", 3000))
                        using (NetworkStream resendStream = resendClient.GetStream())
                        {
                            byte[] buffer = new byte[17];
                            byte[] resendRequest = new byte[] { 2, (byte)i };
                            resendStream.Write(resendRequest, 0, resendRequest.Length);

                            resendStream.Read(buffer, 0, buffer.Length);

                            string symbol = Encoding.ASCII.GetString(buffer, 0, 4);
                            string side = Encoding.ASCII.GetString(buffer, 4, 1);
                            int quantity = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 5));
                            int price = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 9));
                            int sequence = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 13));

                            packets[sequence] = new Packet
                            {
                                Symbol = symbol,
                                Side = side,
                                Quantity = quantity,
                                Price = price,
                                Sequence = sequence
                            };
                        }
                    }
                }

               
                var orderedPackets = packets.Values.OrderBy(p => p.Sequence).ToList();
                string jsonOutput = JsonSerializer.Serialize(orderedPackets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("output.json", jsonOutput);

                Console.WriteLine("Output written to output.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
