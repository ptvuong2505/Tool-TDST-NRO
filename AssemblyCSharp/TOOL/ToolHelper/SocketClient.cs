using Assets.src.g;
using LitJson;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TOOl.Auto;
using static TOOl.Auto.AutoGoback;

namespace AssemblyCSharp.TOOL.ToolHelper;

public class SocketClient : ThreadAction<SocketClient>
{
    private const string pathLogSocket = "log_socket_client.txt";

    public int port = 12345;

    public bool isConnected;

    private Socket sender;

    public void initSender()
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, port);
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(remoteEP);
            sendMessage(new
            {
                action = "connected",
                id = Process.GetCurrentProcess().Id
            });
            isConnected = true;
            performAction();
        }
        catch (Exception ex)
        {
            //writeLog(ex.ToString());
        }
    }

    private void onMessage(JsonData msg)
    {
        string action = (string)msg["action"];
        switch (action)
        {
            case "goToWaitingMap":
                AutoGoback.Waiting();
                break;
            case "changeToZone":
                int zoneId = int.Parse(msg["zoneId"].ToString());
                int mapId = int.Parse(msg["mapId"].ToString());
                MainTool.ChangeToZoneBoss(zoneId, mapId);
                break;
            case "test":
                //MainTool.GetInfor("BOSS Tiểu đội trưởng vừa xuất hiện tại Đông Nam Guru");
                MainTool.GetInfor("BOSS Tiểu đội trưởng vừa xuất hiện tại Vực maima");
                break;
            case "writeData":
                MainTool.WriteDate();
                break;
            case "xmap":
                MainTool.RuntoMap(int.Parse(msg["idMap"].ToString()));
                break;
            default:
                writeLog(">> Lost action " + action + " \n");
                break;
        }
    }

    public void sendMessage(object obj)
    {
        string json = JsonMapper.ToJson(obj);
        if (!json.EndsWith("\n"))
        {
            json += "\n";
        }
        byte[] msg = Encoding.UTF8.GetBytes(json);
        try
        {
            sender.Send(msg);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    protected override void action()
    {
        if (!isConnected)
        {
            return;
        }
        byte[] bytes = new byte[4096];
        string incompleteMessage = "";
        while (true)
        {
            JsonData msg;
            try
            {
                int bytesRec = sender.Receive(bytes);
                string receive = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                receive = incompleteMessage + receive;
                incompleteMessage = "";

                string[] messages = receive.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string message in messages)
                {
                    string trimmed = message.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    try
                    {
                        msg = JsonMapper.ToObject(trimmed);
                        MainThreadDispatcher.dispatcher(delegate
                        {
                            onMessage(msg);
                        });
                    }
                    catch (Exception)
                    {
                        // Message chưa đầy đủ, lưu lại cho lần sau
                        incompleteMessage = trimmed;
                    }
                }
            }
            catch (SocketException)
            {
                GameCanvas.startOKDlg("Mất kết nối với QLTK");
                break;
            }
            catch (ObjectDisposedException)
            {
                GameCanvas.startOKDlg("Mất kết nối với QLTK");
                break;
            }
            catch (Exception ex3)
            {
                //writeLog(ex3.ToString());
                continue;
            }
        }
    }

    private void loadPort()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--port") + 1;
        try
        {
            port = int.Parse(args[index].ToString());
        }
        catch (Exception ex)
        {
            writeLog(ex.ToString());
        }
    }

    public void close()
    {
        Socket socket = sender;
        if (socket != null && socket.Connected)
        {
            sendMessage(new
            {
                action = "close-socket"
            });
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
    }

    private void writeLog(string log)
    {
        try
        {
            File.AppendAllText("log_socket_client.txt", log + "\n");
        }
        catch
        {
        }
    }
}
