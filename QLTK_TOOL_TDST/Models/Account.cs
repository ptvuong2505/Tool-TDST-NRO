using Newtonsoft.Json;
using QLTK_TOOL_TDST.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace QLTK_TOOL_TDST.Models
{
    public class Account : INotifyPropertyChanged
    {
        public int _id;

        private bool _isSelected;

        private bool _isUseProxy;

        private string _userName;

        private string _password;

        private int _server;

        private string _status;

        private int _zone;

        private bool isPick;

        private int _diamond;

        private Proxy _proxy;

        public Account() { }


        [JsonIgnore]
        public Process process;

        [JsonIgnore]
        public Socket workSocket;

        [JsonProperty("IsUseProxy")]
        public bool IsUseProxy
        {
            get { return _isUseProxy; }
            set
            {
                _isUseProxy = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Proxy Proxy
        {
            get { return _proxy; }
            set
            {
                _proxy = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public int Diamond
        {
            get { return _diamond; }
            set
            {
                _diamond = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("IsPick")]
        public bool IsPick
        {
            get { return isPick; }
            set
            {
                isPick = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public int Zone
        {
            get { return _zone; }
            set
            {
                _zone = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("Status")]
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("Server")]
        public int Server
        {
            get { return _server; }
            set
            {
                _server = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("Password")]
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("UserName")]
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
