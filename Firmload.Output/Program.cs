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
            var pnc = "200000000";
            var partHid = "100000001DEV4000011000004";
            var productHid = "100000001DEV4000011000004";
            var ipr = "24AD63BA-BB42-4099-A4E3-BC7B969A4AE0";
        
            var qrCode = new Uri($"https://hqr.codes?hid={productHid}&ipr={ipr}&part-FirstPart={partHid}&pnc={pnc}");
            // Pick bundle based on pnc
            var result = App.RunBundleRaw(";HTTPS://HQR.CODES/?HID=9704937014NE2021402010254&IPR=24AD63BA-BB42-4099-A4E3-BC7B969A4AE0&part-Gearbox=5998061016DZ2021381000219", int.MaxValue).GetAwaiter().GetResult();

            var index = 0;

            Console.WriteLine("Fingerprint\n----------------");

            foreach (var id in result["fingerprint"] as JObject)
            {
                Console.WriteLine($"  {id.Key}:{id.Value}");
            }

            Console.WriteLine("\n----------------State\n----------------");

            foreach (var id in result["fingerprint"] as JObject)
            {
                Console.WriteLine($"  {id.Key}:{id.Value}");
            }

            Console.WriteLine("\n----------------Parts\n----------------");

            foreach (var part in result["parts"]["added"] as JArray)
            {
                Console.WriteLine($"Part[{index++}\n");

                Console.WriteLine("   Fingerprint\n----------------");

                foreach(var id in part["fingerprint"] as JObject)
                {
                    Console.WriteLine($"     {id.Key}:{id.Value}");
                }

                Console.WriteLine("\n   State\n----------------");

                foreach (var id in part["state"] as JObject)
                {
                    Console.WriteLine($"     {id.Key}:{id.Value}");
                }
            }

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