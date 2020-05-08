using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AlmeticaLauncher
{
    internal class GameLauncher
    {
        private readonly AlmeticaClient _client;

        private readonly Configuration _configuration;

        public GameLauncher(Configuration config)
        {
            _configuration = config;
            _client = new AlmeticaClient(config.ServerBaseAddress);
        }

        // TODO debug mode in a separate debug window
        public async Task LaunchGame(string accountName, string password, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(StaThread);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await tcs.Task;

            void StaThread()
            {
                const string launcherClassName = "LAUNCHER_CLASS";
                const string launcherWindowsTitle = "LAUNCHER_WINDOW";
                var wndClass = new WNDCLASSEX
                {
                    cbSize = (uint) Marshal.SizeOf<WNDCLASSEX>(),
                    lpszClassName = launcherClassName,
                    lpfnWndProc = WndProc
                };
                RegisterClassEx(ref wndClass);
                var windowHandle = CreateWindowEx(0, launcherClassName, launcherWindowsTitle, 0, 0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                var pid = Process.Start("Binaries\\TERA.exe",
                    $"-LANGUAGEEXT={_configuration.Language}");

                // Create and listen to the secret named pipe
                Task.Run(() =>
                {
                    var pipename = $"{pid.Id}cout";

                    using var pipeServer = new NamedPipeServerStream(pipename, PipeDirection.In);
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();
                    Debug.WriteLine($"TERA connected to the named pipe {pipename}", "PIPE");
                    try
                    {
                        using var sr = new StreamReader(pipeServer);
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            Debug.WriteLine($"Message: {temp}", "PIPE");
                        }

                    }
                    // Catch the IOException that is raised if the pipe is broken or disconnected
                    catch (IOException e)
                    {
                        Debug.WriteLine($"Error: {e}", "PIPE");
                    }
                }, cancellationToken);

                pid.WaitForExit();

                DefWindowProc(windowHandle, 0x10, IntPtr.Zero, IntPtr.Zero);
                UnregisterClass(launcherClassName, IntPtr.Zero);

                tcs.SetResult(0x0);
            }

            // Handle incomming game events
            IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                // We only care for WM_COPYDATA messages
                const uint wmCopyData = 0x004A;
                if (msg != wmCopyData) return DefWindowProc(hWnd, msg, wParam, lParam);

                var copyData = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                var eventId = copyData.dwData;

                if (copyData.cbData > 0)
                {
                    var managedArray = new byte[copyData.cbData];
                    Marshal.Copy(copyData.lpData, managedArray, 0, copyData.cbData);
                    var hex = BitConverter.ToString(managedArray).Replace("-", "");
                    Debug.WriteLine($"dwData: {eventId}, len: {copyData.cbData}, data: {hex}", "WND_PROC");
                }
                else
                {
                    Debug.WriteLine($"dwData: {eventId}, len: {copyData.cbData}", "WND_PROC");
                }

                // Handle requests by the game client
                switch (eventId)
                {
                    case 1:
                        SendResponseMessage(wParam, hWnd, 2, Encoding.Unicode.GetBytes(accountName));
                        break;
                    case 3:
                        SendResponseMessage(wParam, hWnd, 4, _client.GetTicket(accountName, password));
                        break;
                    case 5:
                        SendResponseMessage(wParam, hWnd, 6, _client.GetServerList().ToByteArray());
                        break;
                }

                return new IntPtr(1);
            }
        }

        private void SendResponseMessage(IntPtr recipient, IntPtr sender, int gameEvent, byte[] payload)
        {
            var hex = BitConverter.ToString(payload).Replace("-", "");
            Debug.WriteLine($"Sending event {gameEvent} to {recipient} with data {hex}", "WM_COPYDATA");

            var payloadPointer = Marshal.AllocHGlobal(payload.Length);
            Marshal.Copy(payload, 0, payloadPointer, payload.Length);

            var response = new COPYDATASTRUCT
            {
                dwData = gameEvent,
                cbData = payload.Length,
                lpData = payloadPointer
            };

            var outgoingDataPointer = Marshal.AllocHGlobal(Marshal.SizeOf<COPYDATASTRUCT>());
            Marshal.StructureToPtr(response, outgoingDataPointer, false);
            SendMessage(recipient, 0x4a, sender, outgoingDataPointer);
            Marshal.FreeHGlobal(outgoingDataPointer);
            Marshal.FreeHGlobal(payloadPointer);
        }

        #region WIN32_API

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public readonly uint style;
            [MarshalAs(UnmanagedType.FunctionPtr)] public WndProcDelegate lpfnWndProc;
            public readonly int cbClsExtra;
            public readonly int cbWndExtra;
            public readonly IntPtr hInstance;
            public readonly IntPtr hIcon;
            public readonly IntPtr hCursor;
            public readonly IntPtr hbrBackground;
            public readonly string lpszMenuName;
            public string lpszClassName;
            public readonly IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public int dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("user32.dll")]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam
        );

        #endregion
    }
}