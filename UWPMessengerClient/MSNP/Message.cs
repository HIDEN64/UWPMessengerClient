﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.Streams;
using Windows.UI.Core;
using System.Runtime.InteropServices.WindowsRuntime;

namespace UWPMessengerClient.MSNP
{
    public class Message : INotifyPropertyChanged
    {
        private string messageText;
        private string sender;
        private string receiver;
        private string senderEmail;
        private string receiverEmail;
        private bool isHistory;
        private int numberOfInkChunks;
        private List<InkChunk> inkChunks = new List<InkChunk>();
        public byte[] InkBytes { get; private set; }
        private BitmapImage inkImage;
        public string InkMessageID { get; private set; }
        public string Base64Ink { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string MessageText
        {
            get => messageText;
            set
            {
                messageText = value;
                NotifyPropertyChanged();
            }
        }

        public string Sender
        {
            get => sender;
            set
            {
                sender = value;
                NotifyPropertyChanged();
            }
        }

        public string Receiver
        {
            get => receiver;
            set
            {
                receiver = value;
                NotifyPropertyChanged();
            }
        }

        public string SenderEmail
        {
            get => senderEmail;
            set
            {
                senderEmail = value;
                NotifyPropertyChanged();
            }
        }

        public string ReceiverEmail
        {
            get => receiverEmail;
            set
            {
                receiverEmail = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsHistory
        {
            get => isHistory;
            set
            {
                isHistory = value;
                NotifyPropertyChanged();
            }
        }

        public BitmapImage InkImage
        {
            get => inkImage;
            set
            {
                inkImage = value;
                NotifyPropertyChanged();
            }
        }

        public void ReceiveSingleInk(string encoded_ink)
        {
            encoded_ink = encoded_ink.Replace("base64:", "");
            Base64Ink = encoded_ink;
            InkBytes = Convert.FromBase64String(encoded_ink);
        }

        public void ReceiveFirstInkChunk(int chunks, string message_id, string chunk)
        {
            numberOfInkChunks = chunks;
            InkMessageID = message_id;
            chunk = chunk.Replace("base64:", "");
            InkChunk inkChunk = new InkChunk()
            {
                ChunkNumber = 0,
                MessageID = InkMessageID,
                EncodedChunk = chunk
            };
            inkChunks.Add(inkChunk);
        }

        public void ReceiveInkChunk(int chunk_number, string chunk)
        {
            if (chunk_number > numberOfInkChunks)
            {
                throw new Exception();
            }
            InkChunk inkChunk = new InkChunk()
            {
                ChunkNumber = chunk_number,
                MessageID = InkMessageID,
                EncodedChunk = chunk
            };
            inkChunks.Add(inkChunk);
            if (chunk_number == (numberOfInkChunks - 1))
            {
                JoinChunks();
            }
        }

        public void JoinChunks()
        {
            string joined_chunks = "";
            foreach (InkChunk inkChunk in inkChunks)
            {
                joined_chunks += inkChunk.EncodedChunk;
            }
            Base64Ink = joined_chunks;
            InkBytes = Convert.FromBase64String(joined_chunks);
        }
    }
}
