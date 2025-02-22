﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AddressParser = ThwargUtils.AddressParser;

namespace SteelBotLauncher
{
    /// <summary>
    /// Code to routinely check status of known servers, via sending UDP packets to them
    /// </summary>
    class ServerMonitor
    {
        public delegate void ReportSomethingDelegateMethod(string msg);

        private class ServerCheckStatus
        {
            public DateTime LastCheckedUtc = DateTime.MinValue;
        }

        private Thread _thread = null;
        private GetServerAction _serverFetcher;
        private int _secondsDelay = 15;
        private Dictionary<Guid, ServerCheckStatus> _serverCheckStatuses = new Dictionary<Guid, ServerCheckStatus>();

        const int TIMEOUTSEC = 3;
        public delegate IEnumerable<ServerModel> GetServerAction();
        public void StartMonitor(GetServerAction serverFetcher)
        {
            if (Properties.Settings.Default.CheckServerStatus)
            {
                StopMonitor();
                _thread = new Thread(new ThreadStart(MonitorLoop));
                _serverFetcher = serverFetcher;
                _thread.Start();
            }
            return;
        }
        public void StopMonitor()
        {
            if (_thread != null)
            {
                _thread.Abort();
                _thread = null;
            }
        }
        private async void MonitorLoop()
        {
            Random random = new Random();
            while (true)
            {
                await CheckAllServers();
                Thread.Sleep(TimeSpan.FromSeconds(_secondsDelay));
            }
        }
        private async Task CheckAllServers()
        {
            // Fetch servers into local list, so that we don't hold an enumeration
            //  from main thread open any longer than necessary
            var servers = _serverFetcher().ToList();
            await Task.WhenAll(servers.Select(s => CheckServer(s)).ToArray());
        }
        private async Task CheckServer(ServerModel server)
        {
            // Get our status for this server
            if (!_serverCheckStatuses.ContainsKey(server.ServerId))
            {
                _serverCheckStatuses[server.ServerId] = new ServerCheckStatus();
            }
            var checkStatus = _serverCheckStatuses[server.ServerId];
            int delaysec = (server.UpStatus == ServerModel.ServerUpStatusEnum.Up ? server.StatusOnlineIntervalSeconds : server.StatusOfflineIntervalSeconds);
            // Is it time to check again
            if (DateTime.UtcNow - checkStatus.LastCheckedUtc < TimeSpan.FromSeconds(delaysec))
            {
                return;
            }
            checkStatus.LastCheckedUtc = DateTime.UtcNow;
            var address = AddressParser.Parse(server.ServerIpAndPort);
            if (string.IsNullOrEmpty(address.Ip) || address.Port <= 0) { return; }
            bool up = await IsUdpServerUp(address.Ip, address.Port);
            string status = GetStatusString(up);
            server.UpStatus = (up ? ServerModel.ServerUpStatusEnum.Up : ServerModel.ServerUpStatusEnum.Down);
            if (server.ConnectionStatus != status)
            {
                CallToUpdate(server, status);
            }
        }
        private async Task<bool> IsUdpServerUp(string address, int port)
        {
            UdpClient udpClient = new UdpClient();
            try
            {
                // udpClient.Client.ReceiveTimeout not used in Async calls
                udpClient.Connect(address, port);
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] sendBytes = Packet.MakeLoginPacket();
                udpClient.Send(sendBytes, sendBytes.Length);
                var receiveTask = udpClient.ReceiveAsync();
                var tsk = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(TIMEOUTSEC)));
                if (tsk == receiveTask)
                {
                    var result = await receiveTask;
                    var header = ByteArrayToNewStuff(result.Buffer);
                    if (((uint)header.Flags & 0x800000u) != 0 && result.Buffer.Length >= 24)
                    {
                        byte[] newBytes = new byte[4];
                        Buffer.BlockCopy(result.Buffer, 20, newBytes, 0, 4);
                        var n = BitConverter.ToUInt32(newBytes, 0);
                        var debug = string.Format("Got {0}: ", n);
                        for (int i = 0; i < result.Buffer.Length; ++i)
                        {
                            var bytn = result.Buffer[i];
                            debug += bytn.ToString("X2");
                            if (i == 4 || i == 8 || i == 12 || i == 14 || i == 16 || i == 18 || i == 20)
                            {
                                debug += " ";
                            }
                        }
                        System.Diagnostics.Debug.WriteLine(debug);

                    }
                    // TODO - extract number of players from buffer
                    return true;
                }
                else
                {
                    // TODO: clean up udpClient?
                    return false;
                }
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10054)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
            }
        }
        public Packet.PacketHeader ByteArrayToNewStuff(byte[] bytes)
        {
            System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            Packet.PacketHeader stuff = (Packet.PacketHeader)System.Runtime.InteropServices.Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Packet.PacketHeader));
            handle.Free();
            return stuff;
        }
        private byte[] ConstructPacket()
        {
            var data = new Packet.PacketHeader(Packet.PacketHeaderFlags.EchoRequest);
            uint checksum;
            data.CalculateHash32(out checksum);
            data.Checksum = checksum;
            return data.GetRaw();
        }
        private static string GetStatusString(bool up)
        {
            return (up ? "✓" : "X");
        }
        private bool IsTcpServerUp(string address, int port)
        {
            var tcpClient = new System.Net.Sockets.TcpClient();
            try
            {
                tcpClient.Connect(address, port);
                return true;
            }
            catch (Exception exc)
            {
                string debug = exc.ToString();
                return false;
            }
        }
        private void CallToUpdate(ServerModel server, string status)
        {
            if (System.Windows.Application.Current == null) return;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                    {
                        PerformUpdate(server, status);
                    }));
        }
        /// <summary>
        /// Called on UI thread
        /// </summary>
        private void PerformUpdate(ServerModel server, string status)
        {
            if (server.ConnectionStatus != status)
            {
                server.ConnectionStatus = status;
            }

        }
    }
}
