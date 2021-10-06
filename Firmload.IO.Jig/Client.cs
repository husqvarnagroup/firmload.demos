using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firmload.IO.Jig
{
    public class SocketClient
    {
        private readonly TcpClient _client = new TcpClient();
        private CancellationTokenSource _closeCancellationTokenSource = new CancellationTokenSource();
        private Thread _readThread;
        private NetworkStream _stream;

        /// <summary>
        /// Raised when bytes have been received from the remote host.
        /// </summary>
        public event EventHandler<byte[]> BytesReceived;

        public void Close()
        {
            if (_closeCancellationTokenSource != null)
            {
                if (!_closeCancellationTokenSource.IsCancellationRequested)
                {
                    _closeCancellationTokenSource.Cancel();
                }
            }

            _client?.Close();
            _stream?.Dispose();

            try
            {
                _readThread?.Interrupt();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Attempts to open a tcp connection to a remote host.
        /// </summary>
        /// <param name="address">IP Address of the remote host</param>
        /// <param name="port">Port number</param>
        /// <returns></returns>
        public async Task OpenAsync(IPAddress address, int port)
        {
            await _client.ConnectAsync(address, port);
            _stream = _client.GetStream();

            _closeCancellationTokenSource = new CancellationTokenSource();

            StartReadThread(_closeCancellationTokenSource.Token);
        }

        /// <summary>
        /// Sends an array of bytes to the remote host
        /// </summary>
        /// <param name="payload">Payload to send</param>
        public void Send(byte[] payload)
        {
            _stream.Write(payload, 0, payload.Length);
        }

        /// <summary>
        /// Attempts to read from the underlaying stream (host connection).
        /// </summary>
        private void Read()
        {
            byte[] bytes = new byte[2048];

            try
            {
                var bytesRead = _stream.Read(bytes, 0, bytes.Length);

                if (bytesRead > 0)
                {
                    var buffer = new byte[bytesRead];
                    Buffer.BlockCopy(bytes, 0, buffer, 0, bytesRead);

                    BytesReceived?.Invoke(this, buffer);
                }
            }
            catch (Exception)
            {
                Close();
            }
        }

        private void StartReadThread(CancellationToken cancellationToken)
        {
            _readThread = new Thread(
                () =>
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            Read();
                            Thread.Yield();
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        // ignored
                    }
                });

            _readThread.IsBackground = true;
            _readThread.Name = $"{nameof(SocketClient)} read thread";
            _readThread.Start();
        }
    }
}