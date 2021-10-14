using Firmload.IO.Jig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firmload.Output
{
    internal class Program
    {
       

        private static int Main(string[] args)
        {
            var pnc = "300000000";

            // Pick bundle based on pnc
            var result = App.RunBundle(pnc, int.MaxValue).GetAwaiter().GetResult();

            if(result.TryGetValue("passed", out var passed) && bool.Parse(passed.ToString()))
            {
                // test passed
                return 0;
            }

            // test did not pass
            return -1;
        }
    }
}