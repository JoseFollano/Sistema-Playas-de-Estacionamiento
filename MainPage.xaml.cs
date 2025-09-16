using Microsoft.Maui.Storage;
using sistemaPlaya.Pages;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace sistemaPlaya
{
    public partial class MainPage : ContentPage
    {
        public MainPage(string nombreUsuario)
        {
            InitializeComponent();
            UserNameLabel.Text = nombreUsuario;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAppStateAndUI();
        }

        private async void LoadAppStateAndUI()
        {
            // Carga el nombre de usuario de Preferences
            string userName = Preferences.Get("UsuarioNombre", "Usuario Desconocido");
            UserNameLabel.Text = $"¡Bienvenido/a, {userName}!";
            CurrentDateLabel.Text = DateTime.Now.ToString("dd MMMM yyyy");

            // Verificar el estado real de la caja con el backend
            await VerificarCajaEnBackendYActualizarUI();

            // Cargar información de estacionamientos
            LoadParkingInfo();

            UpdateNavigationButtonStates();
        }

        private async Task VerificarCajaEnBackendYActualizarUI()
        {
            int idUsuario = Preferences.Get("IdUsuario", 0);
            int idEmpresa = 1; // Cambia esto si tu lógica lo requiere

            if (idUsuario == 0)
            {
                CajaAbiertaInfoLabel.Text = "Usuario no válido";
                CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://localhost:7211/verificarCaja?idEmpresa={idEmpresa}&idUsuario={idUsuario}";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var resultado = JsonSerializer.Deserialize<VerificarCajaResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (resultado != null && resultado.idCaja > 0)
                        {
                            CajaAbiertaInfoLabel.Text = $"Caja abierta (ID: {resultado.idCaja})";
                            CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Green"];
                            Preferences.Set("CajaAbiertaId", resultado.idCaja);
                        }
                        else
                        {
                            CajaAbiertaInfoLabel.Text = "Caja cerrada o no hay caja aperturada";
                            CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                            Preferences.Remove("CajaAbiertaId");
                        }
                    }
                    else
                    {
                        CajaAbiertaInfoLabel.Text = "Error consultando estado de caja";
                        CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                    }
                }
            }
            catch (Exception ex)
            {
                CajaAbiertaInfoLabel.Text = "Error de conexión";
                CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                Console.WriteLine($"Error verificando caja: {ex.Message}");
            }
        }

        // Clase para deserializar la respuesta
        public class VerificarCajaResponse
        {
            public int idCaja { get; set; }
            public DateTime fecha { get; set; }
        }

        private void LoadParkingInfo()
        {
            TotalEstacionamientosLabel.Text = "0";
            OcupadosLabel.Text = "0";
            DisponiblesLabel.Text = "0";
        }

        private void UpdateNavigationButtonStates()
        {
            int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);

            btnAperturarCaja.IsEnabled = cajaAbiertaId == 0;
            btnCerrarCaja.IsEnabled = cajaAbiertaId > 0;
            btnEntradaVehiculo.IsEnabled = true;
            btnConsultarVehiculo.IsEnabled = true;
        }

        private async void OnAperturarCajaClicked(object sender, EventArgs e)
        {
            int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);
            if (cajaAbiertaId > 0)
            {
                await DisplayAlert("Aviso", "Ya hay una caja abierta.", "OK");
                return;
            }

            // Navegar a la página de apertura de caja
            await Navigation.PushAsync(new AperturarCajaPage());
        }

        // Clases para serializar/deserializar
        public class GuardarCajaRequest
        {
            public int idCaja { get; set; }
            public DateTime fechaApertura { get; set; }
            public int idUsuario { get; set; }
            public double importeInicio { get; set; }
            public string estado { get; set; }
            public string nombreUsuario { get; set; }
            public bool nuevo { get; set; }
        }

        public class GuardarCajaResponse
        {
            public int idCaja { get; set; }
            public DateTime fechaApertura { get; set; }
            public int idUsuario { get; set; }
            public double importeInicio { get; set; }
            public string estado { get; set; }
            public string nombreUsuario { get; set; }
            public bool nuevo { get; set; }
        }

        private async void OnCerrarCajaClicked(object sender, EventArgs e)
        {
            // Verificar que haya caja abierta
            int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);
            if (cajaAbiertaId == 0)
            {
                await DisplayAlert("Error", "No hay una caja abierta para cerrar.", "OK");
                return;
            }

            // Navegar a la página de Cierre de Caja
            await Navigation.PushAsync(new CierreCajaPage());


        }

        // Nuevos métodos para estacionamiento
        private async void OnEntradaVehiculoClicked(object sender, EventArgs e)
        {
            // Verificar que haya caja abierta
            //int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);
            //if (cajaAbiertaId == 0)
            //{
            //    await DisplayAlert("Error", "Debe abrir una caja primero.", "OK");
            //    return;
            //}

            // Navegar a la página de entrada de vehículo
            await Navigation.PushAsync(new RegistrarVehiculoPage());
        }

        private async void OnConsultarVehiculoClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("Botón Consultar Vehículo clickeado");

                // Navegar a la página de consulta de vehículo
                // Eliminada la restricción de caja abierta para simulación
                await Navigation.PushAsync(new ConsultaVehiculoPage());

                Console.WriteLine("Navegación exitosa");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await DisplayAlert("Error", $"Error al abrir la página: {ex.Message}", "OK");
            }
        }

        private async void OnCerrarSesionClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Cerrar Sesión", "¿Está seguro que desea cerrar sesión?", "Sí", "No");
            if (confirm)
            {
                // Limpia todas las preferencias relacionadas con la sesión y la caja
                Preferences.Clear(); // Limpia todas las preferencias guardadas

                await DisplayAlert("Cerrar Sesión", "Sesión cerrada.", "OK");
                Application.Current.MainPage = new LoginPage(); // Regresa a la pantalla de login
            }
        }

        private async void OnVerVehiculosClicked(object sender, EventArgs e)
        {
            // Navegar a la página VerVehiculosPage creada en Pages
            try
            {
                await Navigation.PushAsync(new VerVehiculosPage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al navegar a VerVehiculosPage: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo abrir VerVehiculosPage: {ex.Message}", "OK");
            }
        }

    }
}