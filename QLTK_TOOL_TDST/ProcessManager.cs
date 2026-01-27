using IniParser;
using IniParser.Model;
using Newtonsoft.Json.Linq;
using QLTK_TOOL_TDST.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using UnityEngine;
using Path = System.IO.Path;

namespace QLTK_TOOL_TDST
{
    public class ProcessManager
    {
        private CancellationTokenSource _cts;
        private bool _isRunning = false;
        private bool _shutdownRequested = false;
        private readonly object _lock = new object();

        private string pathGame = "Nro_246.exe";
        private string pathFolderHongNgoc = string.Empty;
        private bool isCretedFolder = false;

        public List<Account> Accounts = new List<Account>();
        public TimeSpan RestartDelay;
        public TimeSpan offTimeAfterStart;
        public DateTime? offTime;
        public int stopAtDiamond;

        public event Action<string>? OnProcessStarted;
        public event Action<string>? OnProcessStopped;
        public event Action<string>? OnProcessCrashed;
        public event Action<string>? OnLog;

        private static readonly Lazy<ProcessManager> _instance = new Lazy<ProcessManager>(() => new ProcessManager());
        private readonly ConcurrentDictionary<string, Process> _processes = new();
        public static ProcessManager Instance => _instance.Value;

        private ProcessManager()
        {
        }

        public async Task Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            AsynchronousSocketListener.foundZone = -1;

            Log("- Start new turn\n------------");

            Proxy[] proxies = LoadProxy();
            ReadFolderHongNgoc();

            int indexProxy = 0;
            foreach (var account in Accounts)
            {
                if (account.IsUseProxy)
                {
                    account.Proxy = proxies[indexProxy];
                    indexProxy = (indexProxy+1) % proxies.Length;
                }
                await StartProcess(account);

                Task.Delay(500).Wait(); // tránh khởi động cùng lúc
            }

            _ = Task.Run(() => WatchdogLoop(_cts.Token));
        }

        public async void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            _cts.Cancel();

            foreach (var kv in _processes)
            {
                var key = kv.Key;
                var process = kv.Value;

                try
                {
                    if (process == null) continue;

                    if (!process.HasExited)
                    {
                        // Thử gửi tín hiệu đóng cửa sổ nếu có
                        try { process.CloseMainWindow(); } catch { }

                        // Đợi 0.3 giây xem nó tự thoát không
                        bool exited = process.WaitForExit(300);

                        if (!exited)
                        {
                            // Kill mạnh (bao gồm process con nếu có)
                            try { process.Kill(entireProcessTree: true); } catch { }

                            // Đợi thêm 1 giây xem đã tắt chưa
                            exited = process.WaitForExit(1000);

                            // Fallback cuối cùng — dùng taskkill nếu vẫn chưa tắt
                            if (!exited && !process.HasExited)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/PID {process.Id} /F /T",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    });
                                }
                                catch { }
                            }
                        }

                        OnProcessStopped?.Invoke(key);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stop] Lỗi khi dừng {key}: {ex.Message}");
                }
            }

            _processes.Clear();

            if (offTimeAfterStart > TimeSpan.Zero && offTime == null)
            {
                offTime = DateTime.Now.Add(offTimeAfterStart);
            }
            if(offTime.HasValue && DateTime.Now >= offTime)
            {
                Console.WriteLine($"{DateTime.Now}: Đã đến giờ nghỉ, dừng hoạt động.");
                await StopHunting();
                return;
            }

            Console.WriteLine($"Restart at {DateTime.Now.AddMinutes(RestartDelay.TotalMinutes):HH:mm:ss}...");
            await Task.Delay(RestartDelay);
            Start();
        }

        static DateTime? GetNextStartTime()
        {
            DateTime now = DateTime.Now;
            TimeSpan t = now.TimeOfDay;

            // Các khoảng nghỉ
            var morningStart = new TimeSpan(3, 0, 0);
            var morningEnd = new TimeSpan(5, 0, 0);

            var noonStart = new TimeSpan(11, 0, 0);
            var noonEnd = new TimeSpan(12, 30, 0);

            var eveningStart = new TimeSpan(18, 0, 0);
            var eveningEnd = new TimeSpan(19, 30, 0);

            if (t >= morningStart && t < morningEnd)
                return DateTime.Today.Add(morningEnd);  
            if (t >= noonStart && t < noonEnd)
                return DateTime.Today.Add(noonEnd);     
            if (t >= eveningStart && t < eveningEnd)
                return DateTime.Today.Add(eveningEnd);

            // Không trong các khoảng → null
            return null;
        }

        public async Task StopHunting()
        {
            if (!_isRunning) return;
            _isRunning = false;

            _cts.Cancel();

            foreach (var kv in _processes)
            {
                var key = kv.Key;
                var process = kv.Value;

                try
                {
                    if (process == null) continue;

                    if (!process.HasExited)
                    {
                        // Thử gửi tín hiệu đóng cửa sổ nếu có
                        try { process.CloseMainWindow(); } catch { }

                        // Đợi 2 giây xem nó tự thoát không
                        bool exited = process.WaitForExit(1000);

                        if (!exited)
                        {
                            // Kill mạnh (bao gồm process con nếu có)
                            try { process.Kill(entireProcessTree: true); } catch { }

                            // Đợi thêm 2 giây xem đã tắt chưa
                            exited = process.WaitForExit(1000);

                            // Fallback cuối cùng — dùng taskkill nếu vẫn chưa tắt
                            if (!exited && !process.HasExited)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/PID {process.Id} /F /T",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    });
                                }
                                catch { }
                            }
                        }

                        OnProcessStopped?.Invoke(key);
                    }
                }
                catch (Exception ex)
                {
                    Log($"[Stop] Lỗi khi dừng {key}: {ex.Message}");
                }
            }
            _processes.Clear();
        }

        public void OnDoneSignalReceived()
        {
            lock (_lock)
            {
                if (_shutdownRequested || !_isRunning)
                {
                    return;
                }
                _shutdownRequested = true;
            }

            foreach (var account in Accounts)
            {
                if (account != null && account.process != null)
                {
                    account.sendMessage(new JObject
                    {
                        ["action"] = "writeData"
                    });
                }
            }

            // Chờ account write 2s , Reset lại flag sau 10s
            _ = Task.Run(async () =>
            {
                Thread.Sleep(2000);

                Stop();

                await Task.Delay(10000);
                lock (_lock) _shutdownRequested = false;
            });
        }

        private async Task StartProcess(Account account)
        {
            try
            {
                if (account.process == null || account.process.HasExited)
                {
                    account.Status = "Opening...";

                    if (File.Exists(pathGame) == false)
                    {
                        MessageBox.Show($"Không tìm thấy file game tại đường dẫn: {pathGame}");
                        return;
                    }
                    string proxyArg = "";
                    if (account.IsUseProxy && account.Proxy != null)
                    {
                        // Format: type|host|port|username|password
                        proxyArg = $"--proxyString {account.Proxy.ToString()}";
                    }
                    int jump = Accounts.Count();
                    string arguments = $"--id {account.Id} {proxyArg} --username {account.UserName}  --password  {account.Password}  --server {account.Server} --isPick {account.IsPick} --jump {jump}";
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = pathGame,
                        Arguments = arguments,
                        UseShellExecute = false,
                    };
                    account.process = Process.Start(startInfo);
                    while (account.process.MainWindowHandle == IntPtr.Zero)
                    {
                        await Task.Delay(50);
                    }
                    AsynchronousSocketListener.WaitingAccounts.Add(account);
                    // Đặt tiêu đề cửa sổ
                    SetWindowText(account.process.MainWindowHandle, $"[{account.Id}]{account.UserName}");

                    Sort();

                    _processes[account.UserName] = account.process;
                    OnProcessStarted?.Invoke(account.Id.ToString());

                    account.process.EnableRaisingEvents = true;
                    account.process.Exited += (s, e) =>
                    {
                        if (!_isRunning) return; // đang stop toàn bộ
                        OnProcessCrashed?.Invoke(account.Id.ToString());
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(23000); // chờ để OS giải phóng handle và đủ thời gian login lại
                            if (!_isRunning) return;
                            Log($"[{account.Id}] restarting...");
                            StartProcess(account);
                        });
                    };
                }
            }catch(Exception ex)
            {
                Log($"Error {ex.Message}");
            }


        }

        private Proxy[] LoadProxy()
        {
            try
            {
                string path = "Data/proxies.txt";

                if (!File.Exists(path))
                    throw new FileNotFoundException("Không tìm thấy file proxies.txt");

                var lines = File.ReadAllLines(path);
                var proxies = new List<Proxy>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split(':');
                    if (parts.Length < 4)
                        continue; // sai format thì bỏ qua

                    string host = parts[0];
                    int port = int.Parse(parts[1]);
                    string username = parts[2];
                    string password = parts[3];

                    proxies.Add(new Proxy(
                        ProxyType.Https,   // mặc định dùng HTTPS
                        host,
                        port,
                        username,
                        password
                    ));
                }

                return proxies.ToArray();
            }
            catch
            {
                return Array.Empty<Proxy>();
            }
        }

        private async Task WatchdogLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var kv in _processes.ToArray())
                {
                    var username = kv.Key;
                    var proc = kv.Value;
                    if (proc.HasExited)
                    {
                        var ac = GetAccountByProcess(proc);
                        _processes.TryRemove(ac.UserName, out _);
                        StartProcess(ac);
                    }
                }

                await Task.Delay(3000, ct);
            }
        }

        private Account GetAccountByProcess(Process process)
        {
            return Accounts.FirstOrDefault(a => a.process?.Id == process.Id);
        }

        private void ReadFolderHongNgoc()
        {
            var now = DateTime.Now;
            var cutoff = DateTime.Today.AddHours(5);

            // Nếu trước 05:00 thì lấy ngày hôm qua, ngược lại lấy hôm nay
            var workingDate = now >= cutoff ? now.Date : now.Date.AddDays(-1);
            string folderToday = workingDate.ToString("dd-MM-yyyy");

            try
            {
                string fullPath = Path.Combine("HongNgoc", folderToday);

                // Tạo nếu chưa có (idempotent), đồng thời luôn gán biến đường dẫn
                Directory.CreateDirectory(fullPath);
                pathFolderHongNgoc = fullPath;

                // Đọc file trong thư mục
                ReadFileHongNgoc(pathFolderHongNgoc);
            }
            catch (Exception ex)
            {
                Log($"Lỗi đọc thư mục Hồng Ngọc: {ex.Message}");
            }
        }

        private void ReadFileHongNgoc(string pathFolder)
        {
            if (string.IsNullOrWhiteSpace(pathFolder) || !Directory.Exists(pathFolder))
                return;

            var files = Directory.GetFiles(pathFolder, "*.txt");

            foreach (Account account in Accounts)
            {

                var matchFile = files.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Equals(account.UserName, StringComparison.OrdinalIgnoreCase));

                if (matchFile != null)
                {
                    try
                    {
                        string content = File.ReadAllText(matchFile);

                        var diamond = content
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Select(s => int.TryParse(s, out var v) ? (ok: true, val: v) : (ok: false, val: 0))
                        .Where(x => x.ok)
                        .Select(x => x.val)
                        .ToList();

                        if (diamond.Count > 0)
                        {
                            int first = diamond.First();
                            int last = diamond.Last();

                            // Nếu chênh lệch >= stopAtDiamond thì không pick
                            account.IsPick = Math.Abs(last - first) < stopAtDiamond;
                        }
                        else
                        {
                            account.IsPick = true;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public void Log(string msg)
        {
            OnLog?.Invoke($"{DateTime.Now:HH:mm:ss} {msg}");
        }



        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        private void Sort()
        {
            int x = 10, y = 10;
            int marginX = 5, marginY = 10;
            double screenWidth = SystemParameters.WorkArea.Width;

            foreach (Account account in Accounts)
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
    }
}
