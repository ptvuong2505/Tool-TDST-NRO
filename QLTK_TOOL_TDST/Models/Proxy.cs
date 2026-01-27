using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLTK_TOOL_TDST.Models
{
    public class Proxy
    {
        public ProxyType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public Proxy() { }

        public Proxy(ProxyType type, string host, int port, string username = null, string password = null)
        {
            Type = type;
            Host = host;
            Port = port;
            Username = username ?? "";
            Password = password ?? "";
        }

        public override string ToString()
        {
            // Format: type|host|port|username|password
            return $"{Type.ToString().ToLower()}|{Host}|{Port}|{Username}|{Password}";
        }
    }
    public enum ProxyType
    {
        Https,
        Socks5
    }
}
