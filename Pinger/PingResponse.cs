using System.Net;

namespace Pinger
{
    public class PingResponse
    {
        public string Address { get; set; }
        
        public long RoundtripTime { get; set; }
    }
}