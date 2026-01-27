using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QLTK_TOOL_TDST.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QLTK_TOOL_TDST
{
    public static class AsynchronousSocketListener
    {
        public static List<Account> WaitingAccounts { get; } = new List<Account>();
        public static ManualResetEvent allDone { get; } = new ManualResetEvent(false);

        public static int foundZone = -1;
        static AsynchronousSocketListener()
        {
        }

        private static void onMessage(JToken msg, StateObject state)
        {
            try
            {
                string action = (string)msg["action"];
                switch (action)
                {
                    case "updateState":
                        state.account.Zone = int.Parse(msg["zone"].ToString());
                        state.account.Diamond = int.Parse(msg["hongNgoc"].ToString());
                        break;
                    case "setStatus":
                        state.account.Status = (string)msg["status"];
                        var status = (string)msg["status"];
                        handleStatusChange(status, msg, state);
                        break;
                    case "done":
                        ProcessManager.Instance.OnDoneSignalReceived();
                        break;
                    case "foundBoss":
                        if (foundZone == -1)
                        {
                            foundZone = int.Parse(msg["zone"].ToString());
                            int mapId = int.Parse(msg["mapId"].ToString());
                            HandleBossFound(foundZone, mapId, state.account);
                        }
                        break;
                    case "logChangeState":
                        Console.WriteLine((string)msg["message"]);
                        break;

                    case "connected":
                        int id = (int)msg["id"];
                        var account = WaitingAccounts.FirstOrDefault(a => a.process.Id == id);
                        if (account != null)
                        {
                            state.account = account;
                            state.account.workSocket = state.workSocket;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log_server_error.txt", $"[{DateTime.Now}] Error in onMessage: {ex}\n");
            }
        }

        private static void handleStatusChange(string status, JToken msg, StateObject state)
        {
            try
            {
                switch (status)
                {
                    case "Standing":
                        state.account.sendMessage(new JObject
                        {
                            ["action"] = "goToWaitingMap",
                        });
                        break;
                    case "InZone":
                        if (state.account != null)
                        {
                            Console.WriteLine($"{((string)msg["message"]).ToString()}");
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log_server_error.txt", $"[{DateTime.Now}] Error in handleStatusChange: {ex}\n");
            }
        }

        private static void HandleBossFound(int foundZone, int mapId, Account account)
        {
            if (account == null || WaitingAccounts.Count == 0 || foundZone == -1)
                return;
            Console.WriteLine($"Account {account.Id} found boss in zone {foundZone}, map {mapId}");
            SendChangeZoneToOtherAccounts(foundZone, mapId, account);
        }

        private static void SendChangeZoneToOtherAccounts(int zone, int mapId, Account excludeAccount)
        {
            foreach (var account in WaitingAccounts)
            {
                // Không gửi cho account đã tìm được boss
                if (account != excludeAccount)
                {
                    account.sendMessage(new JObject
                    {
                        ["action"] = "changeToZone",
                        ["zone"] = zone,
                        ["mapId"] = mapId
                    });
                }
            }
        }

        public static void AllAccountsRunToMapId(int idMap)
        {
            if (WaitingAccounts.Count == 0)
                return;

            foreach (var account in WaitingAccounts)
            {
                account.sendMessage(new JObject
                {
                    ["action"] = "xmap",
                    ["idMap"] = idMap
                });
            }
        }

        public static void Test()
        {
            foreach (var account in WaitingAccounts)
            {
                account.sendMessage(new JObject
                {
                    ["action"] = "test"
                });
            }
        }


        #region Server operations

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 12345);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            try
            {
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Bind(localEndPoint);
                listener.Listen(200);

                // Start an asynchronous socket to listen for connections.  
                listener.BeginAccept(callback: new AsyncCallback(AcceptCallback), state: listener);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Ngay sau khi Accept 1 client -> bắt đầu chờ client kế tiếp
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

            var state = new StateObject
            {
                workSocket = handler,
            };

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            if (handler == null || !handler.Connected)
                return;

            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (SocketException)
            {
                return;
            }

            if (bytesRead > 0)
            {
                content = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);

                // Combine with any incomplete message from previous receive
                content = state.IncompleteMessage + content;
                state.IncompleteMessage = "";

                // Handle multiple JSON messages in one buffer
                try
                {
                    var messages = SplitJsonMessages(content, out string remainingContent);
                    state.IncompleteMessage = remainingContent;

                    foreach (var message in messages)
                    {
                        if (!string.IsNullOrEmpty(message.Trim()))
                        {
                            try
                            {
                                var msg = JObject.Parse(message);

                                string action = (string)msg["action"];
                                if (action == "close-socket")
                                {
                                    handler.Shutdown(SocketShutdown.Both);
                                    handler.Close();
                                    if (state.account != null)
                                        state.account.Status = "Disconnected";
                                    return;
                                }

                                onMessage(msg, state);
                            }
                            catch (JsonReaderException ex)
                            {
                                // Log JSON parsing error
                                File.AppendAllText("json_error.txt",
                                    $"JSON Error: {ex.Message}\nContent: {message}\nTime: {DateTime.Now}\n\n");
                                // Continue with next message instead of crashing
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText("socket_error.txt",
                        $"Socket Error: {ex.Message}\nContent: {content}\nTime: {DateTime.Now}\n\n");
                }

                // Continue receiving
                try
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
                catch (ObjectDisposedException)
                {
                    // Socket was disposed, ignore
                }
            }
        }

        private static List<string> SplitJsonMessages(string content, out string remainingContent)
        {
            var messages = new List<string>();
            remainingContent = "";

            // Simple approach: split by lines (assuming each JSON is on one line)
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("{") || trimmed.StartsWith("[")))
                {
                    // Check if line contains complete JSON
                    if (IsCompleteJson(trimmed))
                    {
                        messages.Add(trimmed);
                    }
                    else
                    {
                        remainingContent = trimmed;
                    }
                }
            }

            // If no line breaks, try to detect multiple JSON objects
            if (messages.Count == 0 && !string.IsNullOrEmpty(content.Trim()))
            {
                var extractedMessages = ExtractJsonObjects(content, out remainingContent);
                messages.AddRange(extractedMessages);
            }

            return messages;
        }

        private static bool IsCompleteJson(string json)
        {
            try
            {
                JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> ExtractJsonObjects(string content, out string remainingContent)
        {
            var messages = new List<string>();
            var currentJson = new StringBuilder();
            int braceCount = 0;
            bool inString = false;
            bool escaped = false;
            remainingContent = "";

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (!inString)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        currentJson.Append(c);
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        currentJson.Append(c);

                        if (braceCount == 0 && currentJson.Length > 0)
                        {
                            messages.Add(currentJson.ToString());
                            currentJson.Clear();
                        }
                    }
                    else if (c == '"')
                    {
                        inString = true;
                        currentJson.Append(c);
                    }
                    else if (braceCount > 0)
                    {
                        currentJson.Append(c);
                    }
                }
                else
                {
                    currentJson.Append(c);
                    if (!escaped && c == '"')
                    {
                        inString = false;
                    }
                    escaped = !escaped && c == '\\';
                }
            }

            // If we have incomplete JSON, save it as remaining content
            if (currentJson.Length > 0)
            {
                remainingContent = currentJson.ToString();
            }

            return messages;
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                _ = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void sendMessage(this Account account, object obj)
        {
            Send(account.workSocket, JsonConvert.SerializeObject(obj));
        }

        private static void Send(Socket handler, string data)
        {
            try
            {
                // Ensure JSON ends with newline for proper separation
                if (!data.EndsWith("\n"))
                {
                    data += "\n";
                }

                // Convert the string data to byte data using UTF8 encoding.  
                byte[] byteData = Encoding.UTF8.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                File.AppendAllText("send_error.txt", $"Send Error: {ex.Message}\nData: {data}\nTime: {DateTime.Now}\n\n");
            }
        }

        #endregion
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 4096;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public Account account;

        // Add buffer for incomplete JSON messages
        public string IncompleteMessage { get; set; } = string.Empty;
    }
}
