using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Client;

public class Client
{
    public static void Main()
    {
        PipeDirection direction = PipeDirection.In;
        HandleInheritability inheritability = HandleInheritability.None;
        TokenImpersonationLevel impersonationLevel = TokenImpersonationLevel.None;
        PipeOptions pipeOptions = PipeOptions.None;
        int timeout = Timeout.Infinite;
        string serverName = ".";
        string pipeName = "abcdefgh";


        SafePipeHandle? safePipeHandle = Workaround.GetSafePipeHandle(direction,
            inheritability,
            impersonationLevel,
            pipeOptions,
            timeout,
            serverName,
            pipeName);

        if (safePipeHandle == null)
        {
            Console.WriteLine("Could not create the handle.");
            return;
        }

        Console.WriteLine("Client: Handle created and connected. Passing to stream...");

        NamedPipeClientStream client = new(direction,
            isAsync: (pipeOptions & PipeOptions.Asynchronous) != 0,
            isConnected: true, // Important: You can only connect here
            safePipeHandle);

        // Do not try to use client.Connect().  That method will try to create a new pipe
        // instead of reusing the existing one that was created above. Since the name has
        // already  been used, it throws.

        Console.WriteLine("Client: named pipe client stream created.");

        try
        {
            // This works now as long as the pipe was connected in the client constructor
            Console.WriteLine($"Old ReadMode: {client.ReadMode}");
            client.ReadMode = PipeTransmissionMode.Message;
            Console.WriteLine($"New ReadMode: {client.ReadMode}");

            // The rest of the code below is a usage example to prove it works

            byte[] msg = new byte[] { 44, 55, 66, 77 }; // Should match the server's

            // Works only for Out
            if (direction == PipeDirection.Out)
            {
                Console.WriteLine("Client: Sending message...");
                client.Write(msg, 0, msg.Length);
                Console.WriteLine("Client: Message sent!");
            }
            else // Works for In or InOut
            {
                byte[] received = new byte[] { 0, 0, 0, 0 };
                Console.WriteLine("Client: Reading message...");
                client.Read(received.AsSpan());
                Console.WriteLine("Client: Message read!");
                for (int i = 0; i < received.Length; i++)
                {
                    if (received[i] != msg[i])
                    {
                        Console.WriteLine($"Client: Byte {i} not equal: {received[i]} != {msg[i]}");
                        return;
                    }
                }
                Console.WriteLine("Client: Message received successfully!");
            }
        }
        finally
        {
            Console.WriteLine("Client: Disposing...");
            client.Dispose();
            Console.WriteLine("Client: Disposed!");
        }
    }
}
