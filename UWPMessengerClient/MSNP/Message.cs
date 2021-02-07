using System;
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
        private string _message_text;
        private string _sender;
        private string _receiver;
        private string _sender_email;
        private string _receiver_email;
        private bool _IsHistory;
        private int NumberOfInkChunks;
        private List<InkChunk> InkChunks = new List<InkChunk>();
        public byte[] InkBytes { get; private set; }
        private BitmapImage _InkImage;
        public string InkMessageID { get; private set; }
        public string Base64Ink { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string message_text
        {
            get => _message_text;
            set
            {
                _message_text = value;
                NotifyPropertyChanged();
            }
        }

        public string sender
        {
            get => _sender;
            set
            {
                _sender = value;
                NotifyPropertyChanged();
            }
        }

        public string receiver
        {
            get => _receiver;
            set
            {
                _receiver = value;
                NotifyPropertyChanged();
            }
        }

        public string sender_email
        {
            get => _sender_email;
            set
            {
                _sender_email = value;
                NotifyPropertyChanged();
            }
        }

        public string receiver_email
        {
            get => _receiver_email;
            set
            {
                _receiver_email = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsHistory
        {
            get => _IsHistory;
            set
            {
                _IsHistory = value;
                NotifyPropertyChanged();
            }
        }

        public BitmapImage InkImage
        {
            get => _InkImage;
            set
            {
                _InkImage = value;
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
            NumberOfInkChunks = chunks;
            InkMessageID = message_id;
            chunk = chunk.Replace("base64:", "");
            InkChunk inkChunk = new InkChunk()
            {
                ChunkNumber = 0,
                MessageID = InkMessageID,
                EncodedChunk = chunk
            };
            InkChunks.Add(inkChunk);
        }

        public void ReceiveInkChunk(int chunk_number, string chunk)
        {
            if (chunk_number > NumberOfInkChunks)
            {
                throw new Exception();
            }
            InkChunk inkChunk = new InkChunk()
            {
                ChunkNumber = chunk_number,
                MessageID = InkMessageID,
                EncodedChunk = chunk
            };
            InkChunks.Add(inkChunk);
            if (chunk_number == (NumberOfInkChunks - 1))
            {
                JoinChunks();
            }
        }

        public void JoinChunks()
        {
            string joined_chunks = "";
            foreach (InkChunk inkChunk in InkChunks)
            {
                joined_chunks += inkChunk.EncodedChunk;
            }
            Base64Ink = joined_chunks;
            InkBytes = Convert.FromBase64String(joined_chunks);
        }
    }
}
