using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace AlmeticaLauncher
{
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();

            Configuration = JsonConvert.DeserializeObject<Configuration>(
                File.ReadAllText(@"Configuration.json")
            );
            GameLauncher = new GameLauncher(Configuration);
            AccountNameBox.Text = Configuration.DefaultAccount;
            PasswordBox.Password = Configuration.DefaultPassword;
        }

        private Configuration Configuration { get; }
        private GameLauncher GameLauncher { get; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            AccountNameBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;
            StartButton.IsEnabled = false;

            await GameLauncher.LaunchGame(AccountNameBox.Text, PasswordBox.Password);

            AccountNameBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;
            StartButton.IsEnabled = true;
        }
    }
}