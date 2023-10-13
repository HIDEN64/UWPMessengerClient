using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace UWPMessengerClient.MSNP
{
    public class SocketCommands
    {
        private Socket socket;
        private string serverAddress = "";
        private int serverPort = 0;

        public SocketCommands(string address, int port)
        {
            serverAddress = address;
            serverPort = port;
        }

        public void SetReceiveTimeout(int timeout)
        {
            socket.ReceiveTimeout = timeout;//in ms
        }

        public void ConnectSocket()
        {
            //creates a tcp socket then connects it to the server
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry iPHostEntry = Dns.GetHostEntry(serverAddress);
            IPAddress iPAddress = iPHostEntry.AddressList[0];
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, serverPort);
            socket.Connect(iPEndPoint);
        }

        public void SendCommand(string message)
        {
            if (socket.Connected)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                socket.Send(messageBytes);
            }
        }

        public void SendCommandWithException(string message)
        {
            if (socket.Connected)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                socket.Send(messageBytes);
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
        }

        public void SendCommand(byte[] message)
        {
            if (socket.Connected)
            {
                socket.Send(message);
            }
        }

        public void SendCommandWithException(byte[] message)
        {
            if (socket.Connected)
            {
                socket.Send(message);
            }
            else
            {
                throw new Exceptions.NotConnectedException();
            }
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
            int bytesRead = socket.Receive(buffer);
            string receivedBytesString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return receivedBytesString;
        }

        public void CloseSocket()
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        ~SocketCommands()
        {
            CloseSocket();
        }
    }
}
