using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firmload.DemoClient
{
    public class TifModel
    {
        [JsonProperty("events")]
        public List<TifElement> Events { get; set; } = new List<TifElement>();

        [JsonProperty("methods")]
        public List<TifElement> Methods { get; set; } = new List<TifElement>();

        public bool TryGet(string family, string command, out TifElement result)
        {
            result = null;

            var match = 
                Methods.FirstOrDefault(e => 
                    string.Compare(e.Family, family, true) == 0 && 
                    string.Compare(e.Command, command, true) == 0) 
                ??
                Events.FirstOrDefault(e =>
                    string.Compare(e.Family, family, true) == 0 &&
                    string.Compare(e.Command, command, true) == 0);

            if(match == null)
            {
                return false;
            }

            result = new TifElement()
            {
                Command = match.Command,
                Family = match.Family,
                Parameters = new List<Newtonsoft.Json.Linq.JToken>()
            };

            return true;
        }
    }
}
