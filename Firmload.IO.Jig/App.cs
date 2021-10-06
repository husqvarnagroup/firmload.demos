using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firmload.IO.Jig
{
    public class App
    {
        private SocketClient _client = new SocketClient();
        private Protocol _converter = new Protocol();
        private CancellationTokenSource _waitForResponse = new CancellationTokenSource();
        private TifElement _response = null;
        private string _pendingElement = "";

        public static async Task<JObject> RunBundle(string bundlePnc, int timeout)
        {
            var app = new App();


            // Setup callbacks
            app._client.BytesReceived += app.OnBytesReceived;
            app._converter.OnError += OnError;
            app._converter.OnRequest += app.OnRequest;

            try
            {
                return await app.Start(bundlePnc, timeout);
            } finally
            {
                app._client.BytesReceived -= app.OnBytesReceived;
                app._converter.OnError -= OnError;
                app._converter.OnRequest -= app.OnRequest;

                app._client?.Close();
            }
        }
        /// <summary>
        /// Main loop
        /// 1. Sent qr-code to firmloade result
        /// </summary>
        private async Task<JObject> Start(string pnc, int timeout)
        {
            await _client.OpenAsync(IPAddress.Parse("127.0.0.1"), 51511);

            // Start execution by sending a barcode
            SendEvent("Test", "Begin", $"https://hqr.codes?pnc={pnc}");

            try
            {
                // Wait for the bundle to finish executing
                var response = await WaitForResponse("Test", "Ended", timeout);

                return OnTestEnded(response);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private void OnBytesReceived(object sender, byte[] e)
        {
            _converter.Add(e);
        }

        private static void OnError(object sender, Exception e)
        {
        }

        private void OnRequest(object sender, TifElement e)
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
        private async Task<TifElement> WaitForResponse(string family, string command, int timeoutMs)
        {
            _pendingElement = $"{family}.{command}";
            TifElement result = null;

            if (_response != null &&
                string.Compare(_response.Family, family, true) == 0 &&
                string.Compare(_response.Command, command, true) == 0)
            {
                result = _response;
                _response = null;

                return result;
            }
            else
            {
                _response = null;
            }

            if (_waitForResponse != null && !_waitForResponse.IsCancellationRequested)
            {
                _waitForResponse.Cancel();
            }

            _waitForResponse = new CancellationTokenSource();
            var timeoutToken = new CancellationTokenSource(timeoutMs);

            var combined = CancellationTokenSource.CreateLinkedTokenSource(_waitForResponse.Token,
                timeoutToken.Token);

            await Task.Run(async () =>
            {
                while (!combined.IsCancellationRequested)
                {
                    await Task.Delay(10);
                }
            });

            if (timeoutToken.IsCancellationRequested)
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
        private void SendEvent(string family, string command, params JToken[] inParameters)
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
        private void SendKeepAlive()
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
        private JObject OnTestEnded(TifElement e)
        {
            
            // Is raised when a bundle have finished executing or when a error occurs.
            // * First argument - 0/1 (False/True) if the execution was successfull
            // * Second argument - json structure contaning details about test execution and errors

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(e.Parameters[1].ToString()));
            return JObject.Parse(json);
        }

        #endregion
    }
}
