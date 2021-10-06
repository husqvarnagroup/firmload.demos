using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firmload.IO.Jig
{

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TifElement
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("name")]
        public string Name { get => Command; set => Command = value; }
        
        [JsonProperty("family")]
        public string Family { get; set; }

        public List<JToken> Parameters = new List<JToken>();

        public override string ToString()
        {
            var result =  $"{Family}.{Command} {string.Join(' ',Parameters.Select(p => p.ToString()))}";

            if(result.Length > 32)
            {
                return $"{result.Substring(0, 64)}...";
            }

            return result;
        }
    }
}
