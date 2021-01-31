using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPMessengerClient.MSNP
{
    public class InkChunk
    {
        public int ChunkNumber { get; set; }
        public string MessageID { get; set; }
        public string EncodedChunk { get; set; }
    }
}
