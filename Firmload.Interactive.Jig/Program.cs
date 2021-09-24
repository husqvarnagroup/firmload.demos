using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firmload.DemoClient
{
    internal class Program
    {
        private static SocketClient _client = new SocketClient();
        private static Protocol _converter = new Protocol();
        private static CancellationTokenSource _waitForResponse = new CancellationTokenSource();
        private static TifElement _response = null;
        private static string _pendingElement = "";
        private static string OutDir = ".\\tests";

        private static int Main(string[] args)
        {
            // Setup callbacks
            _client.BytesReceived += OnBytesReceived;
            _converter.OnError += OnError;
            _converter.OnRequest += OnRequest;

            var result = Task.Run(Start).GetAwaiter().GetResult();

            Console.WriteLine($"Return code: {result}");

            return result;
        }
        /// <summary>
        /// Main loop
        /// 1. Sent qr-code to firmload
        /// 2. Wait for test to start (optional)
        /// 3. Wait for bundle to be ready
        /// 4. Request motor speed
        /// 5. Receive motor speed
        /// 6. Wait for test to complete, save result
        /// </summary>
        private static async Task<int> Start()
        {
            await _client.OpenAsync(IPAddress.Parse("127.0.0.1"), 51511);

            // Start execution by sending a barcode
            SendEvent("Test", "Begin", "https://hqr.codes?hid=970494102HYP2021354001309");

            try
            {
                // Wait for Firmload to locate the correct bundle and prepare execution
                var response = await WaitForResponse("Test", "Started", 5000);

                OnTestStarted(response);

                // Wait for the bundle to be ready to receive events
                await WaitForResponse("Bundle", "Ready", 5000);

                // Send an event to the bundle: request to read rpm
                SendEvent("Request", "ReadRpm");

                // Wait for the bundle to read the rpm
                response = await WaitForResponse("Response", "ReadRpm", 5000);

                OnGotRpm(response);

                // Wait for the bundle to finish executing
                response = await WaitForResponse("Test", "Ended", 15000);

                return OnTestEnded(response);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine(e.Message);
                return -2;
            }
            catch(Exception x)
            {
                Console.WriteLine($"Unhandled exception: {x.Message}");
                return -3;
            }
        }

        private static void OnBytesReceived(object sender, byte[] e)
        {
            _converter.Add(e);
        }

        private static void OnError(object sender, Exception e)
        {
        }

        private static void OnRequest(object sender, TifElement e)
        {
            Task.Run(() =>
            {
                if (string.Compare($"{e.Family}.{e.Command}", "system.deviceinfo", true) == 0)
                {
                    // Always respond to keep-alive requests
                    SendKeepAlive();
                }
                else
                {
                    if (_waitForResponse != null && !_waitForResponse.IsCancellationRequested)
                    {
                        // The executing bundle have sent a response
                        if (!string.IsNullOrWhiteSpace(_pendingElement) && string.Compare($"{e.Family}.{e.Command}", _pendingElement, true) == 0)
                        {
                            _response = e;
                            _waitForResponse.Cancel();
                        }
                        else
                        {
                            Console.WriteLine($"Got response for {e}, expected {_pendingElement ?? "null"}");
                            Console.WriteLine(e);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unexpected response: {e}");
                    }
                }
            });
        }
        /// <summary>
        /// Blocks and waits for a response to be received from Firmload.
        /// </summary>
        /// <param name="family">Reponse Tif-family</param>
        /// <param name="command">Reponse Tif-command</param>
        /// <param name="timeoutMs">Timeout in milliseconds before throwing a <see cref="TimeoutException"/></param>
        /// <returns></returns>
        private static async Task<TifElement> WaitForResponse(string family, string command, int timeoutMs)
        {
            _pendingElement = $"{family}.{command}";
            TifElement result = null;

            if(_response != null && 
                string.Compare(_response.Family, family, true) == 0 &&
                string.Compare(_response.Command, command, true) == 0)
            {
                result = _response;
                _response = null;

                return result; 
            } else
            {
                _response = null;
            }

            if(_waitForResponse != null && !_waitForResponse.IsCancellationRequested)
            {
                _waitForResponse.Cancel();
            }

            _waitForResponse = new CancellationTokenSource();
            var timeoutToken = new CancellationTokenSource(timeoutMs);

            var combined = CancellationTokenSource.CreateLinkedTokenSource(_waitForResponse.Token,
                timeoutToken.Token);

            await Task.Run(async () =>
            {
                while(!combined.IsCancellationRequested)
                {
                    await Task.Delay(10);
                }
            });

            if(timeoutToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Did not receive any response for {family}.{command} within {timeoutMs} ms.");
            }

            result = _response;
            _response = null;

            return result;
        }
        /// <summary>
        /// Creates a tif element which is serailized into bytes and sent to Firmload.
        /// </summary>
        /// <param name="family"></param>
        /// <param name="command"></param>
        /// <param name="inParameters"></param>
        private static void SendEvent(string family, string command, params JToken[] inParameters)
        {
            var payload = Protocol.Serialize(new TifElement()
            {
                Family = family,
                Command = command,
                Parameters = new List<JToken>(inParameters ?? new JToken[0])
            }, true);

            _client.Send(payload);
        }
        /// <summary>
        /// Sends keep-alive back to Firmload (run in a task to not block the receiving thread)
        /// </summary>
        private static void SendKeepAlive()
        {
            Task.Run(() =>
            {
                // Payload should always look like this
                var payload = Encoding.UTF8.GetBytes("1.0.0.0\r\nOK\r\n");
                _client.Send(payload);
            });
        }

        

        #region Response callbacks

        /// <summary>
        /// Invoked when the bundle have finished executing
        /// </summary>
        /// <param name="e">Tif-element sent from Firmload</param>
        private static int OnTestEnded(TifElement e)
        {
            if (!Directory.Exists($"{OutDir}"))
            {
                Directory.CreateDirectory(OutDir);
            }

            // Is raised when a bundle have finished executing or when a error occurs.
            // * First argument - 0/1 (False/True) if the execution was successfull
            // * Second argument - json structure contaning details about test execution and errors

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(e.Parameters[1].ToString()));
            var report = JObject.Parse(json);

            var output = $"{OutDir}\\{report["id"]}.json";
            File.WriteAllText(output, json);

            Console.WriteLine($"Firmload finished executing bundle, result {e.Parameters[0]}, saved to {output}");

            return report.TryGetValue("passed", out var passed) ? (bool.Parse(passed.ToString()) ? 1 : 0) : -1;
        }
        /// <summary>
        /// Invoked when Firmload have started executing a bundle
        /// </summary>
        /// <param name="e"></param>
        private static void OnTestStarted(TifElement e)
        {
            // Is raised when all conditions in Firmload is OK:
            // * A bundle matching the HID or PNC is found
            // * A connection to a product have been established

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(e.Parameters[0].ToString()));
            var bundle = JObject.Parse(json);

            Console.WriteLine($"Firmload is executing bundle '{bundle["name"]}'");
        }
        /// <summary>
        /// Invoked when the bundle sends the motor rpm
        /// </summary>
        /// <param name="e"></param>
        private static void OnGotRpm(TifElement e)
        {
            var speed = e.Parameters[0].ToString();
            Console.WriteLine($"Motor speed: {speed}");
        }

        #endregion
    }
}