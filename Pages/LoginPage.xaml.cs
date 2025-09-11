using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace sistemaPlaya
{
    public class LoginResponse
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Nombre { get; set; }
    }

    public partial class LoginPage : ContentPage
    {
        // private const string BaseApiUrl = "https://localhost:7282/"; // <--- AJUSTA ESTA URL CUANDO TENGAS LAS APIS

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Por favor, ingrese usuario y contraseña.", "OK");
                return;
            }

            // Deshabilita el botón y muestra el indicador de carga
            LoginButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // SIMULACIÓN DE LOGIN SIN API - PARA PRUEBAS LOCALES
                await Task.Delay(1000); // Simular tiempo de carga

                // Validación básica para pruebas (sin API)
                if (!string.IsNullOrEmpty(username))
                {
                    await DisplayAlert("Login Exitoso", $"Bienvenido, {username}!", "OK");

                    // Guardar datos de sesión simulados
                    Preferences.Set("IdUsuario", 1);
                    Preferences.Set("UsuarioNombre", username);

                    // Navega a la pantalla principal
                    Application.Current.MainPage = new NavigationPage(new MainPage());
                }
                else
                {
                    await DisplayAlert("Error de Login", "Usuario o contraseña incorrectos.", "OK");
                }

                /* DESCOMENTAR CUANDO TENGAS LAS APIS
                using (HttpClient client = new HttpClient())
                {
                    // Construye la URL completa del endpoint de validación
                    string requestUrl = $"{BaseApiUrl}ValidarUsuario?nombreUsuario={username}&clave={password}";

                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        LoginResponse loginData = JsonSerializer.Deserialize<LoginResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // idUsuario > 0 indica un login válido
                        if (loginData != null && loginData.IdUsuario > 0)
                        {
                            await DisplayAlert("Login Exitoso", $"Bienvenido, {loginData.Nombre}!", "OK");

                            Preferences.Set("IdUsuario", loginData.IdUsuario);
                            Preferences.Set("UsuarioNombre", loginData.Nombre);

                            // Navega a la pantalla principal
                            Application.Current.MainPage = new NavigationPage(new MainPage());
                        }
                        else
                        {
                            await DisplayAlert("Error de Login", "Usuario o contraseña incorrectos. Por favor, verifique sus credenciales.", "OK");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error de API", $"No se pudo validar el usuario. Código: {response.StatusCode}. Detalle: {errorContent}", "OK");
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                LoginButton.IsEnabled = true;
            }
        }
    }
}