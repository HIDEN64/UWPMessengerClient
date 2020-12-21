using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace UWPMessengerClient
{
    public class SocketCommands
    {
        private Socket socket;
        private string server_address = "";
        private int server_port = 0;

        public SocketCommands(string address, int port)
        {
            server_address = address;
            server_port = port;
        }

        public void SetReceiveTimeout(int timeout)
        {
            socket.ReceiveTimeout = timeout;//in ms
        }

        public void ConnectSocket()
        {
            //creates a tcp socket then connects it to the server
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry iPHostEntry = Dns.GetHostEntry(server_address);
            IPAddress iPAddress = iPHostEntry.AddressList[0];
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, server_port);
            socket.Connect(iPEndPoint);
        }

        public void SendCommand(string msg)
        {
            byte[] message = Encoding.UTF8.GetBytes(msg);
            socket.Send(message);
        }

        public void SendCommand(byte[] message)
        {
            socket.Send(message);
        }

        public void BeginReceiving(byte[] buffer, AsyncCallback asyncCallback, object stateObject)
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, asyncCallback, stateObject);
        }

        public int StopReceiving(IAsyncResult ar)
        {
            if (socket.Connected)
            {
                return socket.EndReceive(ar);
            }
            else
            {
                return 0;
            }
        }

        public string ReceiveMessage(byte[] buffer)
        {
            int bytes_read = socket.Receive(buffer);
            string received_bytes_string = Encoding.UTF8.GetString(buffer, 0, bytes_read);
            return received_bytes_string;
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
