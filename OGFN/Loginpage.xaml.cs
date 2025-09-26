using Helix;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;

namespace WpfApp1 // MUST MATCH the XAML namespace
{
    public partial class LoginPage : Window
    {
        private readonly HttpClient httpClient = new HttpClient();
        private const string UserFilePath = "user.json";

        public LoginPage()
        {
            InitializeComponent();

            // Auto-login if user file exists
            if (File.Exists(UserFilePath))
            {
                var savedData = File.ReadAllText(UserFilePath);
                dynamic user = JsonConvert.DeserializeObject(savedData);
                string username = user.username;
                string password = user.password;  
                OpenMainWindow(username);
            }
        }

        private void InputField_TextChanged(object sender, RoutedEventArgs e)
        {
  
            LoadingText.Text = "Logging In...";
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            ShowLoading(true);

            var loginData = new { email, password };
            string json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://127.0.0.1:3551/api/launcher/login", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    string username = result.username;


                    var userData = JsonConvert.SerializeObject(new { username, email, password });
                    File.WriteAllText(UserFilePath, userData);

                    OpenMainWindow(username);
                }
                else
                {
                    dynamic err = JsonConvert.DeserializeObject(responseBody);
                    string message = err.errorMessage ?? "Login failed";
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowLoading(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Network error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenMainWindow(string username)
        {
            MainWindow mainWindow = new MainWindow(username);
            mainWindow.Show();
            this.Close();
        }
    }
}
