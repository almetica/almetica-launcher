using Google.Protobuf;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlmeticaLauncher
{
    class GameLauncher
    {
        public GameLauncher(Configuration config)
        {
            this.Configuration = config;
            this.Client = new AlmeticaClient(config.ServerBaseAddress);
        }

        private readonly Configuration Configuration;
        private readonly AlmeticaClient Client;

        // TODO debug mode in a seperate debug window
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
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    lpszClassName = launcherClassName,
                    lpfnWndProc = WndProc
                };
                RegisterClassEx(ref wndClass);
                var windowHandle = CreateWindowEx(0, launcherClassName, launcherWindowsTitle, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                var pid = Process.Start("Binaries\\TERA.exe", string.Format("-LANGUAGEEXT={0}", this.Configuration.Language));

                // Create and listen to the secret named pipe
                Task.Run(() =>
                {
                    var pipename = string.Format("{0}cout", pid.Id);

                    using NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipename, PipeDirection.In);
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();
                    Debug.WriteLine("TERA connected to the named pipe {0}", pipename);
                    try
                    {
                        using (StreamReader sr = new StreamReader(pipeServer))
                        {
                            string temp;
                            while ((temp = sr.ReadLine()) != null)
                            {
                                Debug.WriteLine("PIPE MESSAGE: {0}", temp);
                            }
                        }
                    }
                    // Catch the IOException that is raised if the pipe is broken or disconnected
                    catch (IOException e)
                    {
                        Debug.WriteLine("PIPE ERROR: {0}", e.Message);
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
                var event_id = copyData.dwData;

                if (copyData.cbData > 0)
                {
                    byte[] managedArray = new byte[copyData.cbData];
                    Marshal.Copy(copyData.lpData, managedArray, 0, copyData.cbData);
                    var hex = BitConverter.ToString(managedArray).Replace("-", "");
                    Debug.WriteLine("LAUNCHER WM_COPYDATA: dwData: {0}, len: {1}, data: {2}", event_id, copyData.cbData, hex);
                }
                else
                {
                    Debug.WriteLine("LAUNCHER WM_COPYDATA: dwData: {0}, len: {1}", event_id, copyData.cbData);
                }

                // Handle requests by the game client
                switch (event_id)
                {
                    case 1:
                        SendResponseMessage(wParam, hWnd, 2, Encoding.Unicode.GetBytes(accountName));
                        break;
                    case 3:
                        SendResponseMessage(wParam, hWnd, 4, this.Client.GetTicket(accountName, password));
                        break;
                    case 5:
                        SendResponseMessage(wParam, hWnd, 6, this.Client.GetServerList().ToByteArray());
                        break;
                }

                return new IntPtr(1);
            }
        }

        private void SendResponseMessage(IntPtr recipient, IntPtr sender, int game_event, byte[] payload)
        {
            var hex = BitConverter.ToString(payload).Replace("-", "");
            Debug.WriteLine("Sending event {0} with data {1} to {2}", game_event,  recipient, hex);

            IntPtr payload_pointer = Marshal.AllocHGlobal(payload.Length);
            Marshal.Copy(payload, 0, payload_pointer, payload.Length);

            var response = new COPYDATASTRUCT
            {
                dwData = game_event,
                cbData = payload.Length,
                lpData = payload_pointer,
            };

            var outgoingDataPointer = Marshal.AllocHGlobal(Marshal.SizeOf<COPYDATASTRUCT>());
            Marshal.StructureToPtr(response, outgoingDataPointer, false);
            SendMessage(recipient, 0x4a, sender, outgoingDataPointer);
            Marshal.FreeHGlobal(outgoingDataPointer);
            Marshal.FreeHGlobal(payload_pointer);
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
        static extern int GetLastError();

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
