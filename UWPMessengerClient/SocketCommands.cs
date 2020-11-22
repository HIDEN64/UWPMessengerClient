using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace UWPMessengerClient
{
    class SocketCommands
    {
        private Socket socket;
        private string server_address = "";
        private static int server_port = 0;

        public SocketCommands(string address, int port)
        {
            server_address = address;
            server_port = port;
        }

        public void ConnectSocket()
        {
            //creates a tcp socket then connects it to the server
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 10000;
            IPHostEntry iPHostEntry = Dns.GetHostEntry(server_address);
            IPAddress iPAddress = iPHostEntry.AddressList[0];
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, server_port);
            socket.Connect(iPEndPoint);
        }

        public void SendCommand(string msg)
        {
            byte[] message = Encoding.ASCII.GetBytes(msg);
            socket.Send(message);
        }

        public void BeginReceiving(byte[] buffer, AsyncCallback asyncCallback, object stateObject)
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, asyncCallback, stateObject);
        }

        public int StopReceiving(IAsyncResult ar)
        {
            return socket.EndReceive(ar);
        }

        public string ReceiveMessage(int message_size = 4096)
        {
            int size = 0;
            byte[] received_bytes = new byte[message_size];
            try
            {
                do
                {
                    size = socket.Receive(received_bytes);
                }
                while (socket.Available != 0);
            }
            catch (SocketException e)
            {
                return "Socket exception: " + e.Message + "\n";
            }
            if (size != 0)
            {
                string received_bytes_string = Encoding.ASCII.GetString(received_bytes);
                return received_bytes_string;
            }
            else
            {
                return "";
            }
        }

        public void CloseSocket()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        ~SocketCommands()
        {
            CloseSocket();
        }
    }
}
