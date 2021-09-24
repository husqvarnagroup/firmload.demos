using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Firmload.DemoClient
{
    public class Protocol
    {
        private const string End = "\r\n";
        private const string Error = "ERROR";
        private const string Event = "EVENT";

        private readonly List<string> _receivedStrings = new List<string>();

        private string _currentMessage = string.Empty;

        public event EventHandler<TifElement> OnResponse;

        public event EventHandler<TifElement> OnRequest;

        public event EventHandler<Exception> OnError;

        /// <summary>
        /// Serializes a tif-element into a byte representation.
        /// </summary>
        /// <param name="message">Message to serialize</param>
        /// <param name="sendAsEvent">If true, appends EVENT to the begining of the byte sequence.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static byte[] Serialize(TifElement message, bool sendAsEvent)
        {
            var result = sendAsEvent ? "EVENT " : "";
            result = $"{result}{message.Family}.{message.Command}";

            foreach (var p in message.Parameters)
            {
                switch (p.Type)
                {
                    case JTokenType.String:
                        result += $" \"{p}\"";
                        break;

                    case JTokenType.Boolean:
                        result += (bool)p ? " 1" : " 0";
                        break;
                    case JTokenType.Integer:
                        result += " " + p.ToString();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(p.Type), $"Parameter type {p.Type} is not supported.");
                }
            }

            result += End;
            return StringToByteArray(result);
        }


        public void Add(byte[] data)
        {
            var stringData = ByteArrayToString(data);

            try
            {
                _currentMessage += stringData;

                var responses = new List<string>();
                var pos = _currentMessage.IndexOf(End, StringComparison.Ordinal);

                while (pos >= 0)
                {
                    var subString = _currentMessage.Substring(0, pos);
                    responses.Add(subString);
                    _currentMessage = _currentMessage.Remove(0, pos + End.Length);
                    pos = _currentMessage.IndexOf(End, StringComparison.Ordinal);
                }

                foreach (var response in responses)
                {
                    CheckResponse(response);
                }
            }
            catch (Exception x)
            {
                throw new ArgumentException("Failed to parse '" + stringData + "', reason: " + x.Message, x);
            }
        }

        protected virtual void FireHandleResponse(TifElement e)
        {
            OnResponse?.Invoke(this, e);
        }

        protected virtual void FireHandleResponseNotOk(Exception e)
        {
            OnError?.Invoke(this, e);
        }

        private static string ByteArrayToString(byte[] payload)
        {
            return Encoding.Default.GetString(payload);
        }

        private static byte[] StringToByteArray(string s)
        {
            return Encoding.ASCII.GetBytes(s);
        }

        private void CheckResponse(string response)
        {
            _receivedStrings.Add(response);

            if (response == Error)
            {
                var message = string.Join(" ", _receivedStrings);
                message = message.Remove(message.Length - response.Length, response.Length).Trim();

                FireHandleResponseNotOk(new Exception(message));
                _receivedStrings.Clear();
            } else
            {
                var message = ParseRequest(response);
                FireHandleRequest(message);

                _receivedStrings.Clear();
            }
        }

        private void FireHandleRequest(TifElement element)
        {
            if(OnRequest != null && element != null)
            {
                OnRequest?.Invoke(this, element);
            }
        }

        private TifElement ParseRequest(string response)
        {
            // Example match: Request.MotorSpeed args
            var match = Regex.Match(response, @"^([a-zA-Z0-9]+)\.([a-zA-Z0-9]+)(.*)");

            if(!match.Success)
            {
                return null;
            }

            var result = new TifElement()
            {
                Family = match.Groups[1].Value,
                Command = match.Groups[2].Value
            };

            // Example match: "one" 2 "three" 4
            foreach (Match arg in Regex.Matches(match.Groups[3].Value, "\"[^\"]+\"|[\\d]+|[a-zA-Z0-9]+"))
            {
                if(arg.Value.StartsWith("\""))
                {
                    result.Parameters.Add(arg.Value.Trim('"'));
                } else if(int.TryParse(arg.Value, out var numeric))
                {
                    result.Parameters.Add(numeric);
                }
                else if(bool.TryParse(arg.Value, out var condition))
                {
                    result.Parameters.Add(condition);
                }
            }

            return result;
        }
    }
}
