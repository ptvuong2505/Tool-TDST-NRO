using IniParser;
using IniParser.Model;
using Newtonsoft.Json;
using QLTK_TOOL_TDST;
using QLTK_TOOL_TDST.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using UnityEngine;
using Debug = System.Diagnostics.Debug;


namespace QLTK_TOOL_TDST
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);


        private const string PathAccountFile = "Data/accounts.json";
        private const string pathGame = "Nro_246.exe";
        private const string pathProxy = "Data/proxies.txt";


        private ObservableCollection<Account> _accounts = new();
        private readonly ProcessManager _manager = ProcessManager.Instance;
        private int _totalDiamond;
        public int TotalDiamond
        {
            get { return _totalDiamond; }
            set 
            {
                if (_totalDiamond != value)
                {
                    _totalDiamond = value;
                    OnPropertyChanged(nameof(TotalDiamond));
                }
            }
        }

        
        public static List<Server> Servers { get; } =
        [
            new Server("Vũ trụ 1", "dragon1.teamobi.com", 14445),
            new Server("Vũ trụ 2", "dragon2.teamobi.com", 14445),
            new Server("Vũ trụ 3", "dragon3.teamobi.com", 14445),
            new Server("Vũ trụ 4", "dragon4.teamobi.com", 14445),
            new Server("Vũ trụ 5", "dragon5.teamobi.com", 14445),
            new Server("Vũ trụ 6", "dragon6.teamobi.com", 14445),
            new Server("Vũ trụ 7", "dragon7.teamobi.com", 14445),
            new Server("Vũ trụ 8", "dragon10.teamobi.com", 14446),
            new Server("Vũ trụ 9", "dragon10.teamobi.com", 14447),
            new Server("Vũ trụ 10", "dragon10.teamobi.com", 14445),
            new Server("Vũ trụ 11", "dragon11.teamobi.com", 14445),
            new Server("Vũ trụ 12", "dragon12.teamobi.com", 14445),
            new Server("Võ đài Liên Vũ Trụ", "dragonwar.teamobi.com", 20000),
            new Server("Universe 1", "dragon.indonaga.com", 14445, 2),
            new Server("Indonaga", "dragon.indonaga.com", 14446, 2),
        ];


        // Import Win32 API để bật console riêng
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // create console
            AllocConsole();
            Console.Title = "LOG TOOL_TDST";
            Console.SetBufferSize(120, 1000);

            LoadData();
            new Thread(() => AsynchronousSocketListener.StartListening())
            {
                IsBackground = true,
                Name = "AsynchronousSocketListener.StartListening"
            }.Start();

            _manager.OnLog += s =>
            {
                Console.WriteLine(s);
            };
            _manager.OnProcessStarted += acc => Console.WriteLine($"Login account {acc}");
            _manager.OnProcessStopped += acc => Console.WriteLine($"Stop account {acc}");
            _manager.OnProcessCrashed += acc => Console.WriteLine($"Crash account {acc}");

            Closed += (s, e) => SaveAccounts();
        }
        private void SaveAccounts()
        {
            try
            {
                string accountsJson = JsonConvert.SerializeObject(_accounts, Formatting.Indented);
                File.WriteAllText(PathAccountFile, accountsJson);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("Error saving accounts: " + ex.Message);
            }

        }

        private void LoadData()
        {
            LoadServers();
            LoadAccounts();
            // Đăng ký sự kiện PropertyChanged cho từng account hiện có
            _accounts.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (Account acc in e.NewItems)
                        acc.PropertyChanged += Account_PropertyChanged;
                }
                if (e.OldItems != null)
                {
                    foreach (Account acc in e.OldItems)
                        acc.PropertyChanged -= Account_PropertyChanged;
                }

                RecalculateTotal(); // Khi thêm hoặc xóa account thì cũng tính lại tổng
            };
            foreach (var account in _accounts)
            {
                account.PropertyChanged += Account_PropertyChanged;
            }


            RecalculateTotal();

        }

        private void Account_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Account.Diamond))
            {
                RecalculateTotal();
            }
        }

        private void RecalculateTotal()
        {
            TotalDiamond = _accounts.Sum(a => a.Diamond);
        }

        private void togglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (password.Visibility == Visibility.Visible)
            {
                // Switch to visible text
                passwordVisible.Text = password.Password;
                password.Visibility = Visibility.Collapsed;
                passwordVisible.Visibility = Visibility.Visible;
                togglePassword.Content = "🔒";
            }
            else
            {
                // Switch to password (hidden)
                password.Password = passwordVisible.Text;
                passwordVisible.Visibility = Visibility.Collapsed;
                password.Visibility = Visibility.Visible;
                togglePassword.Content = "👁";
            }
        }

        private void btnAdd(object sender, RoutedEventArgs e)
        {
            string u = username.Text;
            string p = password.Visibility == Visibility.Visible ? password.Password : passwordVisible.Text;
            int s = ServerComboBox.SelectedIndex;
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p) || s < 0)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin tài khoản!");
                return;
            }
            Account newAccount = new Account
            {
                UserName = u,
                Password = p,
                Server = s,
                Status = "Chưa đăng nhập"
            };
            _accounts.Add(newAccount);
            SaveAccounts();
        }

        private bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        private void selectedAll_Click(object sender, RoutedEventArgs e)
        {
            bool isChecedAll = (sender as CheckBox)?.IsChecked == true;
            foreach (var account in _accounts)
            {
                account.IsSelected = isChecedAll;
            }
        }

        private void useProxyAll_Click(object sender, RoutedEventArgs e)
        {
            bool isUseProxy = (sender as CheckBox)?.IsChecked == true;
            foreach (var account in _accounts)
            {
                account.IsUseProxy = isUseProxy;
            }
        }

        private void pickAll_Click(object sender, RoutedEventArgs e)
        {
            bool isPick = (sender as CheckBox)?.IsChecked == true;
            foreach (var account in _accounts)
            {
                account.IsPick = isPick;
            }
        }

        private List<Account> GetSelectedAccounts()
        {
            List<Account> selectedAccounts = new List<Account>();
            if (_accounts == null || _accounts.Count == 0)
            {
                return selectedAccounts;
            }
            int id = 0;
            foreach (var item in _accounts)
            {
                if (item.IsSelected)
                {
                    item._id = id++;
                    selectedAccounts.Add(item);
                }
            }
            return selectedAccounts;
        }

        private void btnSort(object sender, RoutedEventArgs e)
        {
            Sort();
        }

        private void Sort()
        {
            int x = 10, y = 10;
            int marginX = 5, marginY = 10;
            double screenWidth = SystemParameters.WorkArea.Width;

            foreach (Account account in _manager.Accounts)
            {
                var proc = account.process;
                if (proc == null)
                    continue;

                try
                {
                    proc.WaitForInputIdle();
                    proc.Refresh();

                    IntPtr hWnd = proc.MainWindowHandle;
                    if (hWnd == IntPtr.Zero || !Win32.IsWindowVisible(hWnd))
                        continue;

                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);

                    if (Win32.GetWindowRect(hWnd, out RECT rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;

                        // Nếu vượt qua chiều rộng màn hình → xuống dòng
                        if (x + width > screenWidth)
                        {
                            x = 20;
                            y += height + marginY;
                        }

                        Win32.MoveWindow(hWnd, x, y, width, height, true);
                        x += width + marginX;
                    }

                }
                catch (Exception ex)
                {

                }
            }
        }

        private void deleteBtn(object sender, RoutedEventArgs e)
        {
            Account account = dataAccounts.SelectedItem as Account;
            if (account == null)
            {
                MessageBox.Show("Select an account to delete!");
                return;
            }
            if (MessageBox.Show($"Are you sure to delete account: {account.UserName}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _accounts.Remove(account);
                SaveAccounts();
            }
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            _manager.Accounts = GetSelectedAccounts();

            int offTimeEachTurnValue = int.TryParse(offTimeEachTurn.Text, out var valOffTimeEachTurn) ? valOffTimeEachTurn : 12;
            _manager.RestartDelay = TimeSpan.FromMinutes(offTimeEachTurnValue);

            int offTimeAfterStartValue = int.TryParse(offTimeAfterStart.Text, out var valOffTimeAfterStart) && valOffTimeAfterStart > 0 ? valOffTimeAfterStart : 0;
            _manager.offTimeAfterStart = TimeSpan.FromHours(offTimeAfterStartValue);

            int stopAtDiamondValue = int.TryParse(stopAtDiamond.Text, out var valStopAtDiamond) && valStopAtDiamond > 0 ? valStopAtDiamond : 35;
            _manager.stopAtDiamond = stopAtDiamondValue;

            _manager.offTime = null;

            _manager.Start();
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            _manager.StopHunting();
        }

        #region Load Data

        private void LoadServers()
        {
            ServerComboBox.ItemsSource = Servers;
            ServerComboBox.DisplayMemberPath = "name";
            ServerComboBox.SelectedIndex = 0;
        }
        private void LoadAccounts()
        {
            try
            {
                var directory = Path.GetDirectoryName(PathAccountFile);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                if (!File.Exists(PathAccountFile))
                {
                    File.WriteAllText(PathAccountFile, "[]");
                }

                if (!File.Exists(pathProxy))
                {
                    File.WriteAllText(pathProxy, string.Empty);
                }

                string accountsJson = File.ReadAllText(PathAccountFile);

                var listAccounts = JsonConvert.DeserializeObject<List<Account>>(accountsJson) ?? new List<Account>();

                for (int i = 0; i < listAccounts.Count; i++)
                {
                    listAccounts[i].Id = i;
                }

                _accounts = new ObservableCollection<Account>(listAccounts);
                dataAccounts.ItemsSource = _accounts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading accounts: " + ex.Message);
            }
        }


        #endregion

        private void btnResetPick(object sender, RoutedEventArgs e)
        {
            // Hiển thị hộp thoại xác nhận
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn Reset Pick ngày hôm nay không?",
                "Xác nhận resset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            // Nếu người dùng chọn "Yes" → thực hiện xóa
            if (result == MessageBoxResult.Yes)
            {
                DeleteFolderHongNgoc(); // Gọi hàm xóa bạn đã viết ở trên
                MessageBox.Show("Đã reset thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteFolderHongNgoc()
        {
            var now = DateTime.Now;
            var cutoff = DateTime.Today.AddHours(5);

            // Nếu trước 05:00 thì làm việc với ngày hôm qua, ngược lại hôm nay
            var workingDate = now >= cutoff ? now.Date : now.Date.AddDays(-1);
            string folderToday = workingDate.ToString("dd-MM-yyyy");

            try
            {
                string parentPath = "HongNgoc";
                string fullPath = Path.Combine(parentPath, folderToday);

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true); // true: xóa cả nội dung bên trong
                    _manager.Log($"Reset Pick: {fullPath}");
                }
                else
                {
                    Console.WriteLine($"Không tìm thấy thư mục cần xóa: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý lỗi tùy ý
                Console.WriteLine($"Lỗi khi xóa thư mục: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public static class Win32
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_RESTORE = 9;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    public class ServerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int server)
            {
                return server + 1;
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}