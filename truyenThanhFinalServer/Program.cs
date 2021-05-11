using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NetCoreServer;

namespace SslChatServer
{
    class ChatSession : SslSession
    {
        public ChatSession(SslServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} connected!");
        }

        protected override void OnHandshaked()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} handshaked!");

            // Send invite message
            string message = "Hello from SSL chat! Please send a message or '!' to disconnect the client!";
            Send(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            Server.Multicast(message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Disconnect();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat SSL session caught an error with code {error}");
        }
    }

    class ChatServer : SslServer
    {
        public ChatServer(SslContext context, IPAddress address, int port) : base(context, address, port) { }

        protected override SslSession CreateSession() { return new ChatSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat SSL server caught an error with code {error}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // SSL server port
            int port = 2222;

            Console.WriteLine($"SSL server port: {port}");

            Console.WriteLine();

            /*  https://www.scottbrady91.com/C-Sharp/PEM-Loading-in-dotnet-core-and-dotnet 
             *  https://stackoverflow.com/questions/50227580/create-x509certificate2-from-pem-file-in-net-core
              // loaded PEM file with labels stripped. Full value omitted for brevity
                var certPem = "MIIB3zCCAYWgAwIBAgIUImttQCULqkHxYbDivb...FwT3WZO4S5JB5jvPg9hCnlXPjNwaC";

                var cert = new X509Certificate2(Convert.FromBase64String(certPem));

                // can be combined with the private key from the previous section 
                var certWithKey = cert.CopyWithPrivateKey(key);
             */

            var certLetsCrypt = X509Certificate2.CreateFromPemFile(@"/etc/letsencrypt/live/iothtnhust20201.xyz/fullchain.pem");

            //Pass the file path and file name to the StreamReader constructor
            StreamReader sr = new StreamReader(@"/etc/letsencrypt/live/iothtnhust20201.xyz/chain.pem");
            //Read the first line of text
            var lineRead = sr.ReadLine();
            //Continue to read until you reach end of file
            while (lineRead != null)
            {
                //write the lie to console window
                Console.WriteLine(lineRead);
                //Read the next line
                lineRead = sr.ReadLine();
            }
            //close the file
            sr.Close();

            // Create and prepare a new SSL server context
            //var context = new SslContext(SslProtocols.Tls12, new X509Certificate2("server.pfx", "qwerty"));
            var context = new SslContext(SslProtocols.Tls12, certLetsCrypt);

            // Create a new SSL chat server
            var server = new ChatServer(context, IPAddress.Any, port);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.Multicast(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }
    }
}
