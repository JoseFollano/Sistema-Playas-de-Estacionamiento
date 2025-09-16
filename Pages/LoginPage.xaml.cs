using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace sistemaPlaya
{
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            LoginButton.IsEnabled = false;

            var usuario = UsernameEntry.Text?.Trim();
            var clave = PasswordEntry.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(clave))
            {
                await DisplayAlert("Error", "Ingrese usuario y contraseña.", "OK");
                ResetLoadingState();
                return;
            }

            try
            {
                var url = $"https://localhost:7211/ValidarUsuario?nombreUsuario={Uri.EscapeDataString(usuario)}&clave={Uri.EscapeDataString(clave)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var usuarioInfo = JsonSerializer.Deserialize<UsuarioInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (usuarioInfo != null && usuarioInfo.IdUsuario > 0)
                    {
                        Preferences.Set("UsuarioNombre", usuarioInfo.Nombre);
                        Preferences.Set("IdUsuario", usuarioInfo.IdUsuario);

                        await DisplayAlert("Bienvenido", $"Hola {usuarioInfo.Nombre}", "OK");
                        await Navigation.PushAsync(new MainPage(usuarioInfo.Nombre));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo conectar al servidor.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
            finally
            {
                ResetLoadingState();
            }
        }

        private void ResetLoadingState()
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoginButton.IsEnabled = true;
        }
    }

    public class UsuarioInfo
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Nombre { get; set; }
    }
}
