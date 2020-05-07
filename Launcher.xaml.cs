using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AlmeticaLauncher
{
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();

            this.Configuration = JsonConvert.DeserializeObject<Configuration>(
                File.ReadAllText(@"Configuration.json")
            );
            this.GameLauncher = new GameLauncher(this.Configuration);
            AccountNameBox.Text = Configuration.DefaultAccount;
            PasswordBox.Password = Configuration.DefaultPassword;
        }

        private Configuration Configuration { get; set; }
        private GameLauncher GameLauncher { get; set; }

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
