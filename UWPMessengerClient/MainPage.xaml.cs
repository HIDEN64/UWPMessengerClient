using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Net.Http;

// O modelo de item de Página em Branco está documentado em https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x416

namespace UWPMessengerClient
{
    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = Email_box.Text;
            string password = Password_box.Password;
            NotificationServerConnection notificationServerConnection = new NotificationServerConnection(email, password);
            string[] output_buffer_array = await notificationServerConnection.login_to_messengerAsync();
            string output_buffer = "";
            foreach (string outputLine in output_buffer_array)
            {
                output_buffer += outputLine;
            }
            set_output_text(output_buffer);
        }

        public void set_output_text(string OutputText)
        {
            Output.Text = OutputText;
        }

        public string get_output_text()
        {
            return Output.Text;
        }
    }
}
