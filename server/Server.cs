using System;
using System.IO.Pipes;

namespace Server;

public class Server
{
    public static void Main()
    {
        string pipeName = "abcdefgh";
        PipeDirection direction = PipeDirection.Out;

        NamedPipeServerStream server = new(pipeName, direction, 1, PipeTransmissionMode.Message);

        Console.WriteLine("Server: named pipe server stream created.");

        // The rest of the code below is a usage example to prove it works

        byte[] received = new byte[] { 0, 0, 0, 0 };
        try
        {
            Console.WriteLine("Server: Waiting for connection...");
            server.WaitForConnection();
            Console.WriteLine("Server: Connection established!");

            byte[] msg = new byte[] { 44, 55, 66, 77 }; // Should match the client's

            // Works for Out or InOut
            if ((direction & PipeDirection.Out) != 0)
            {
                Console.WriteLine("Server: Sending message...");
                server.Write(msg, 0, msg.Length);
                Console.WriteLine("Server: Message sent!");
            }
            else // Works only for In
            {
                Console.WriteLine("Server: Reading message...");
                server.Read(received.AsSpan());
                Console.WriteLine("Server: Message read!");
                for (int i = 0; i < received.Length; i++)
                {
                    if (received[i] != msg[i])
                    {
                        Console.WriteLine($"Server: Byte {i} not equal: {received[i]} != {msg[i]}");
                        return;
                    }
                }
                Console.WriteLine("Server: Message received successfully!");
            }
            Console.WriteLine("Server: Disconnecting...");
            server.Disconnect();
            Console.WriteLine("Server: Disconnected!");
        }
        finally
        {
            Console.WriteLine("Server: Disposing...");
            server.Dispose();
            Console.WriteLine("Server: Disposed!");
        }
    }
}
