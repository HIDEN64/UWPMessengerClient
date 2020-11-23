﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.UI.Core;

namespace UWPMessengerClient
{
    public partial class SwitchboardConnection
    {
        public ObservableCollection<Message> MessageList { get; set; } = new ObservableCollection<Message>();
        public void ReceivingCallback(IAsyncResult asyncResult)
        {
            SwitchboardConnection switchboardConnection = (SwitchboardConnection)asyncResult.AsyncState;
            int bytes_received = switchboardConnection.SBSocket.StopReceiving(asyncResult);
            switchboardConnection.outputString = Encoding.ASCII.GetString(switchboardConnection.outputBuffer, 0, bytes_received);
            if (switchboardConnection.outputString.StartsWith("MSG") && !switchboardConnection.outputString.Contains("TypingUser"))
            {
                AddMessageToList();
            }
            if (bytes_received > 0)
            {
                SBSocket.BeginReceiving(outputBuffer, new AsyncCallback(ReceivingCallback), switchboardConnection);
            }
        }

        public void AddMessageToList()
        {
            string messageText = outputString.Substring(outputString.LastIndexOf("\r\n") + 2);//2 counting for \r and \n
            string[] MSGParams = outputString.Split(" ");
            string senderDisplayName = MSGParams[2];
            Windows.Foundation.IAsyncAction task = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageList.Add(new Message() { message_text = messageText, sender = senderDisplayName });
            });
        }
    }
}
