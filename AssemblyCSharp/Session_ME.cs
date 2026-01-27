using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AssemblyCSharp.TOOL;
using UnityEngine;
using System.Net;
using AssemblyCSharp.TOOL.ToolHelper;

public class Session_ME : ISession
{
	public class Sender
	{
		public List<Message> sendingMessage;

		public Sender()
		{
			sendingMessage = new List<Message>();
		}

		public void AddMessage(Message message)
		{
			sendingMessage.Add(message);
		}

		public void run()
		{
			while (connected)
			{
				try
				{
					if (getKeyComplete)
					{
						while (sendingMessage.Count > 0)
						{
							Message m = sendingMessage[0];
							doSendMessage(m);
							sendingMessage.RemoveAt(0);
						}
					}
					try
					{
						Thread.Sleep(5);
					}
					catch (Exception ex)
					{
						Cout.LogError(ex.ToString());
					}
				}
				catch (Exception)
				{
					Res.outz("error send message! ");
				}
			}
		}
	}

	private class MessageCollector
	{
		public void run()
		{
			try
			{
				while (connected)
				{
					Message message = readMessage();
					if (message == null)
					{
						break;
					}
					try
					{
						if (message.command == -27)
						{
							getKey(message);
						}
						else
						{
							onRecieveMsg(message);
						}
					}
					catch (Exception)
					{
						Cout.println("LOI NHAN  MESS THU 1");
					}
					try
					{
						Thread.Sleep(5);
					}
					catch (Exception)
					{
						Cout.println("LOI NHAN  MESS THU 2");
					}
				}
			}
			catch (Exception ex3)
			{
				Debug.Log("error read message!");
				Debug.Log(ex3.Message.ToString());
			}
			if (!connected)
			{
				return;
			}
			if (messageHandler != null)
			{
				if (currentTimeMillis() - timeConnected > 500)
				{
					messageHandler.onDisconnected(isMainSession);
				}
				else
				{
					messageHandler.onConnectionFail(isMainSession);
				}
			}
			if (sc != null)
			{
				cleanNetwork();
			}
		}

		private void getKey(Message message)
		{
			try
			{
				sbyte b = message.reader().readSByte();
				key = new sbyte[b];
				for (int i = 0; i < b; i++)
				{
					key[i] = message.reader().readSByte();
				}
				for (int j = 0; j < key.Length - 1; j++)
				{
					ref sbyte reference = ref key[j + 1];
					reference ^= key[j];
				}
				getKeyComplete = true;
				GameMidlet.IP2 = message.reader().readUTF();
				GameMidlet.PORT2 = message.reader().readInt();
				GameMidlet.isConnect2 = ((message.reader().readByte() != 0) ? true : false);
                if (isMainSession && GameMidlet.isConnect2)
				{
					GameCanvas.connect2();
				}
			}
			catch (Exception)
			{
			}
		}

		private Message readMessage2(sbyte cmd)
		{
			int num = readKey(dis.ReadSByte()) + 128;
			int num2 = readKey(dis.ReadSByte()) + 128;
			int num3 = readKey(dis.ReadSByte()) + 128;
			int num4 = (num3 * 256 + num2) * 256 + num;
			sbyte[] array = new sbyte[num4];
			int num5 = 0;
			byte[] src = dis.ReadBytes(num4);
			Buffer.BlockCopy(src, 0, array, 0, num4);
			recvByteCount += 5 + num4;
			int num6 = recvByteCount + sendByteCount;
			strRecvByteCount = num6 / 1024 + "." + num6 % 1024 / 102 + "Kb";
			if (getKeyComplete)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = readKey(array[i]);
				}
			}
			return new Message(cmd, array);
		}

		private Message readMessage()
		{
			try
			{
				sbyte b = dis.ReadSByte();
				if (getKeyComplete)
				{
					b = readKey(b);
				}
				if (b == -32 || b == -66 || b == 11 || b == -67 || b == -74 || b == -87 || b == 66 || b == 12)
				{
					return readMessage2(b);
				}
				int num;
				if (getKeyComplete)
				{
					sbyte b2 = dis.ReadSByte();
					sbyte b3 = dis.ReadSByte();
					num = ((readKey(b2) & 0xFF) << 8) | (readKey(b3) & 0xFF);
				}
				else
				{
					sbyte b4 = dis.ReadSByte();
					sbyte b5 = dis.ReadSByte();
					num = (b4 & 0xFF00) | (b5 & 0xFF);
				}
				sbyte[] array = new sbyte[num];
				int num2 = 0;
				int num3 = 0;
				byte[] src = dis.ReadBytes(num);
				Buffer.BlockCopy(src, 0, array, 0, num);
				recvByteCount += 5 + num;
				int num4 = recvByteCount + sendByteCount;
				strRecvByteCount = num4 / 1024 + "." + num4 % 1024 / 102 + "Kb";
				if (getKeyComplete)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = readKey(array[i]);
					}
				}
				return new Message(b, array);
			}
			catch (Exception ex)
			{
				Debug.Log(ex.StackTrace.ToString());
			}
			return null;
		}
	}

	protected static Session_ME instance = new Session_ME();

	private static NetworkStream dataStream;

	private static BinaryReader dis;

	private static BinaryWriter dos;

	public static IMessageHandler messageHandler;

	public static bool isMainSession = true;

	private static TcpClient sc;

	public static bool connected;

	public static bool connecting;

	private static Sender sender = new Sender();

	public static Thread initThread;

	public static Thread collectorThread;

	public static Thread sendThread;

	public static int sendByteCount;

	public static int recvByteCount;

	private static bool getKeyComplete;

	public static sbyte[] key = null;

	private static sbyte curR;

	private static sbyte curW;

	private static int timeConnected;

	private long lastTimeConn;

	public static string strRecvByteCount = string.Empty;

	public static bool isCancel;

	private string host;

	private int port;

	private long timeWaitConnect;

	public static int count;

	#region Proxy Settings
	public static string proxyHost = "";
    public static int proxyPort = 0;
    public static string proxyUsername = "";
    public static string proxyPassword = "";
    public static ProxyType proxyType = ProxyType.None;

	public enum ProxyType
    {
        None,
        HTTPS,
        SOCKS5
    }
	#endregion

	public static MyVector recieveMsg = new MyVector();

	public Session_ME()
	{
		Debug.Log("init Session_ME");
	}

	public void clearSendingMessage()
	{
		sender.sendingMessage.Clear();
	}

	public static Session_ME gI()
	{
		if (instance == null)
		{
			instance = new Session_ME();
		}
		return instance;
	}

	public bool isConnected()
	{
		return connected && sc != null && dis != null;
	}

	public void setHandler(IMessageHandler msgHandler)
	{
		messageHandler = msgHandler;
	}

	public void connect(string host, int port)
	{
		if (connected || connecting)
		{
			Debug.Log(">>>return connect ...!" + connected + "  ::  " + connecting);
			return;
		}
		if (mSystem.currentTimeMillis() < timeWaitConnect)
		{
			Debug.LogError(">>>>chặn việc nó kết nối 2 3 lần liên tục");
			return;
		}
		timeWaitConnect = mSystem.currentTimeMillis() + 50;
		if (isMainSession)
		{
			ServerListScreen.testConnect = -1;
		}
		SetProxy();
        this.host = host;
		this.port = port;
		getKeyComplete = false;
		close();
		Debug.Log("connecting...!");
		Debug.Log("host: " + host);
		Debug.Log("port: " + port);
		initThread = new Thread(NetworkInit);
		initThread.Start();
	}

	private void NetworkInit()
	{
		isCancel = false;
		connecting = true;
		Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
		connected = true;
		try
		{
			doConnect(host, port);
			messageHandler.onConnectOK(isMainSession);
		}
		catch (Exception)
		{
			if (messageHandler != null)
			{
				close();
				messageHandler.onConnectionFail(isMainSession);
			}
		}
	}

	public void doConnect(string host, int port)
	{
		if (proxyType == ProxyType.None)
		{
			sc = new TcpClient();
			sc.Connect(host, port);
		}
		else if (proxyType == ProxyType.HTTPS)
        {
            // Kết nối qua HTTPS Proxy
            sc = ConnectThroughHttpsProxy(host, port);
        }
        else if (proxyType == ProxyType.SOCKS5)
        {
            // Kết nối qua SOCKS5 Proxy
            sc = ConnectThroughSocks5Proxy(host, port);
        }
		
		dataStream = sc.GetStream();
		dis = new BinaryReader(dataStream, new UTF8Encoding());
		dos = new BinaryWriter(dataStream, new UTF8Encoding());
		sendThread = new Thread(sender.run);
		sendThread.Start();
		MessageCollector messageCollector = new MessageCollector();
		collectorThread = new Thread(messageCollector.run);
		collectorThread.Start();
		timeConnected = currentTimeMillis();
		connecting = false;
		doSendMessage(new Message(-27));
		key = null;
    }

    #region Proxy Connection

    public static void SetProxy()
    {
        if (!MainTool.isUseProxy || string.IsNullOrEmpty(MainTool.proxyString))
        {
            proxyType = ProxyType.None;
            return;
        }

        try
        {
            // Format: type|host|port|username|password
            string[] proxyParts = MainTool.proxyString.Split('|');

            if (proxyParts.Length < 3)
            {
                Debug.LogError("Invalid proxy format: " + MainTool.proxyString);
                proxyType = ProxyType.None;
                return;
            }

            proxyType = proxyParts[0].ToLower() switch
            {
                "https" => ProxyType.HTTPS,
                "socks5" => ProxyType.SOCKS5,
                _ => ProxyType.None,
            };

            proxyHost = proxyParts[1];
            proxyPort = int.Parse(proxyParts[2]);
            proxyUsername = proxyParts.Length > 3 ? proxyParts[3] : string.Empty;
            proxyPassword = proxyParts.Length > 4 ? proxyParts[4] : string.Empty;
        }
        catch (Exception ex)
        {
            proxyType = ProxyType.None;
        }
    }

    private  TcpClient ConnectThroughHttpsProxy(string targetHost, int targetPort)
	{
		TcpClient tcpClient = new TcpClient();
        
        try
        {
            // Kết nối tới proxy server
            tcpClient.Connect(proxyHost, proxyPort);
            NetworkStream stream = tcpClient.GetStream();
            
            // Tạo HTTP CONNECT request
            string connectRequest = string.Format("CONNECT {0}:{1} HTTP/1.1\r\n", targetHost, targetPort);
            connectRequest += string.Format("Host: {0}:{1}\r\n", targetHost, targetPort);
             
            // Thêm Basic Authentication nếu có username/password
            if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
            {
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(proxyUsername + ":" + proxyPassword));
                connectRequest += string.Format("Proxy-Authorization: Basic {0}\r\n", credentials);
            }
            
            connectRequest += "Proxy-Connection: Keep-Alive\r\n";
            connectRequest += "\r\n";
            
            // Gửi CONNECT request
            byte[] requestBytes = Encoding.ASCII.GetBytes(connectRequest);
            stream.Write(requestBytes, 0, requestBytes.Length);
            
            // Đọc response từ proxy
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            
            // Kiểm tra response status
            if (!response.StartsWith("HTTP/1.1 200") && !response.StartsWith("HTTP/1.0 200"))
            {
                throw new Exception(string.Format("Proxy connection failed: {0}", response.Split('\r')[0]));
            }

            return tcpClient;
        }
        catch
        {
            if (tcpClient != null)
                tcpClient.Close();
            throw;
        }
	}

    private TcpClient ConnectThroughSocks5Proxy(string targetHost, int targetPort)
    {
        TcpClient tcpClient = new TcpClient();
        
        try
        {
            // Kết nối tới proxy server
            tcpClient.Connect(proxyHost, proxyPort);
            NetworkStream stream = tcpClient.GetStream();
            
            // Bước 1: Gửi greeting message
            byte authMethod = string.IsNullOrEmpty(proxyUsername) ? (byte)0x00 : (byte)0x02;
            byte[] greeting = new byte[] { 0x05, 0x02, 0x00, authMethod }; // SOCKS5, 2 methods, No Auth, Username/Password
            stream.Write(greeting, 0, greeting.Length);
            
            // Đọc response
            byte[] buffer = new byte[2];
            stream.Read(buffer, 0, 2);
            
            if (buffer[0] != 0x05)
                throw new Exception("Invalid SOCKS5 response");
                
            // Bước 2: Xác thực (nếu cần)
            if (buffer[1] == 0x02 && !string.IsNullOrEmpty(proxyUsername))
            {
                // Username/Password authentication
                List<byte> authData = new List<byte>();
                authData.Add(0x01); // Version
                authData.Add((byte)proxyUsername.Length);
                authData.AddRange(Encoding.ASCII.GetBytes(proxyUsername));
                authData.Add((byte)proxyPassword.Length);
                authData.AddRange(Encoding.ASCII.GetBytes(proxyPassword));

                byte[] authBytes = authData.ToArray();
                stream.Write(authBytes, 0, authBytes.Length);
                
                // Đọc auth response
                stream.Read(buffer, 0, 2);
                if (buffer[1] != 0x00)
                    throw new Exception("SOCKS5 authentication failed");
            }
            else if (buffer[1] == 0xFF)
            {
                throw new Exception("No acceptable authentication method");
            }
            
            // Bước 3: Gửi connection request
            List<byte> connectRequest = new List<byte>();
            connectRequest.Add(0x05); // SOCKS version
            connectRequest.Add(0x01); // Connect command
            connectRequest.Add(0x00); // Reserved
            
            // Thêm địa chỉ đích
            IPAddress ipAddress;
            if (IPAddress.TryParse(targetHost, out ipAddress))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    connectRequest.Add(0x01); // IPv4
                    connectRequest.AddRange(ipAddress.GetAddressBytes());
                }
                else
                {
                    connectRequest.Add(0x04); // IPv6
                    connectRequest.AddRange(ipAddress.GetAddressBytes());
                }
            }
            else
            {
                connectRequest.Add(0x03); // Domain name
                connectRequest.Add((byte)targetHost.Length);
                connectRequest.AddRange(Encoding.ASCII.GetBytes(targetHost));
            }
            
            // Thêm port
            connectRequest.Add((byte)(targetPort >> 8));
            connectRequest.Add((byte)(targetPort & 0xFF));
            
            byte[] connectBytes = connectRequest.ToArray();
            stream.Write(connectBytes, 0, connectBytes.Length);
            
            // Đọc connect response
            buffer = new byte[10]; // Tối thiểu cho IPv4 response
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            if (buffer[0] != 0x05 || buffer[1] != 0x00)
            {
                string errorMessage;
                switch (buffer[1])
                {
                    case 0x01:
                        errorMessage = "General SOCKS server failure";
                        break;
                    case 0x02:
                        errorMessage = "Connection not allowed by ruleset";
                        break;
                    case 0x03:
                        errorMessage = "Network unreachable";
                        break;
                    case 0x04:
                        errorMessage = "Host unreachable";
                        break;
                    case 0x05:
                        errorMessage = "Connection refused";
                        break;
                    case 0x06:
                        errorMessage = "TTL expired";
                        break;
                    case 0x07:
                        errorMessage = "Command not supported";
                        break;
                    case 0x08:
                        errorMessage = "Address type not supported";
                        break;
                    default:
                        errorMessage = string.Format("Unknown error: {0}", buffer[1]);
                        break;
                }
                throw new Exception(string.Format("SOCKS5 connection failed: {0}", errorMessage));
            }
            
            return tcpClient;
        }
        catch
        {
            if (tcpClient != null)
                tcpClient.Close();
            throw;
        }
    }
	#endregion

	public void sendMessage(Message message)
	{
		count++;
		Res.outz("SEND MSG: " + message.command);
		sender.AddMessage(message);
	}

	private static void doSendMessage(Message m)
	{
		sbyte[] data = m.getData();
		try
		{
			if (getKeyComplete)
			{
				sbyte value = writeKey(m.command);
				dos.Write(value);
			}
			else
			{
				dos.Write(m.command);
			}
			if (data != null)
			{
				int num = data.Length;
				if (getKeyComplete)
				{
					int num2 = writeKey((sbyte)(num >> 8));
					dos.Write((sbyte)num2);
					int num3 = writeKey((sbyte)(num & 0xFF));
					dos.Write((sbyte)num3);
				}
				else
				{
					dos.Write((ushort)num);
				}
				if (getKeyComplete)
				{
					for (int i = 0; i < data.Length; i++)
					{
						sbyte value2 = writeKey(data[i]);
						dos.Write(value2);
					}
				}
				sendByteCount += 5 + data.Length;
			}
			else
			{
				if (getKeyComplete)
				{
					int num4 = 0;
					int num5 = writeKey((sbyte)(num4 >> 8));
					dos.Write((sbyte)num5);
					int num6 = writeKey((sbyte)(num4 & 0xFF));
					dos.Write((sbyte)num6);
				}
				else
				{
					dos.Write((ushort)0);
				}
				sendByteCount += 5;
			}
			dos.Flush();
		}
		catch (Exception ex)
		{
			Debug.Log(ex.StackTrace);
			dos.Flush();
		}
	}

	public static sbyte readKey(sbyte b)
	{
		sbyte[] array = key;
		sbyte num = curR;
		curR = (sbyte)(num + 1);
		sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
		if (curR >= key.Length)
		{
			curR %= (sbyte)key.Length;
		}
		return result;
	}

	public static sbyte writeKey(sbyte b)
	{
		sbyte[] array = key;
		sbyte num = curW;
		curW = (sbyte)(num + 1);
		sbyte result = (sbyte)((array[num] & 0xFF) ^ (b & 0xFF));
		if (curW >= key.Length)
		{
			curW %= (sbyte)key.Length;
		}
		return result;
	}

	public static void onRecieveMsg(Message msg)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			messageHandler.onMessage(msg);
		}
		else
		{
			recieveMsg.addElement(msg);
		}
	}

	public static void update()
	{
		while (recieveMsg.size() > 0)
		{
			Message message = (Message)recieveMsg.elementAt(0);
			if (Controller.isStopReadMessage)
			{
				break;
			}
			if (message == null)
			{
				recieveMsg.removeElementAt(0);
				break;
			}
			messageHandler.onMessage(message);
			recieveMsg.removeElementAt(0);
		}
	}

	public void close()
	{
		cleanNetwork();
	}

	private static void cleanNetwork()
	{
		key = null;
		curR = 0;
		curW = 0;
		Debug.LogError(">>>cleanNetwork ...!");
		try
		{
			connected = false;
			connecting = false;
			if (sc != null)
			{
				sc.Close();
				sc = null;
			}
			if (dataStream != null)
			{
				dataStream.Close();
				dataStream = null;
			}
			if (dos != null)
			{
				dos.Close();
				dos = null;
			}
			if (dis != null)
			{
				dis.Close();
				dis = null;
			}
			if (Thread.CurrentThread.Name == Main.mainThreadName)
			{
				if (sendThread != null)
				{
					sendThread.Abort();
				}
				sendThread = null;
				if (initThread != null)
				{
					initThread.Abort();
				}
				initThread = null;
				if (collectorThread != null)
				{
					collectorThread.Abort();
				}
				collectorThread = null;
			}
			else
			{
				sendThread = null;
				initThread = null;
				collectorThread = null;
			}
			if (isMainSession)
			{
				ServerListScreen.testConnect = 0;
			}
			Controller.isGet_CLIENT_INFO = false;
		}
		catch (Exception)
		{
		}
	}

	public static int currentTimeMillis()
	{
		return Environment.TickCount;
	}

	public static byte convertSbyteToByte(sbyte var)
	{
		if (var > 0)
		{
			return (byte)var;
		}
		return (byte)(var + 256);
	}

	public static byte[] convertSbyteToByte(sbyte[] var)
	{
		byte[] array = new byte[var.Length];
		for (int i = 0; i < var.Length; i++)
		{
			if (var[i] > 0)
			{
				array[i] = (byte)var[i];
			}
			else
			{
				array[i] = (byte)(var[i] + 256);
			}
		}
		return array;
	}

	public bool isCompareIPConnect()
	{
		return true;
	}
}
