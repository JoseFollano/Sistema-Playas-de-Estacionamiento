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
        public MainPage()
        {
            InitializeComponent();
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
            await VerifyCashboxStateWithBackend();

            // Cargar información de estacionamientos
            LoadParkingInfo();

            UpdateNavigationButtonStates();
        }

        private void LoadParkingInfo()
        {
            TotalEstacionamientosLabel.Text = "0";
            OcupadosLabel.Text = "0";
            DisponiblesLabel.Text = "0";
        }

        private async Task VerifyCashboxStateWithBackend()
        {
            try
            {
                int userId = Preferences.Get("IdUsuario", 0);
                int localCashboxId = Preferences.Get("CajaAbiertaId", 0);

                if (userId > 0 && localCashboxId > 0)
                {
                    // Verificar con el backend si la caja realmente está abierta
                    bool isCashboxActuallyOpen = await CheckCashboxStatusFromAPI(localCashboxId);

                    if (!isCashboxActuallyOpen)
                    {
                        // La caja no está abierta en el backend, limpiar preferencias locales
                        Preferences.Remove("CajaAbiertaId");
                        Preferences.Remove("CajaAbiertaCodigo");
                        Preferences.Remove("CajaAbiertaEstado");
                        Preferences.Remove("CajaImporteInicio");
                        Preferences.Remove("CajaFechaApertura");
                        Preferences.Remove("CajaUsuarioId");
                        Preferences.Remove("CajaUsuarioNombre");

                        // Actualizar UI
                        CajaAbiertaInfoLabel.Text = "Caja: Cerrada";
                        CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                    }
                    else
                    {
                        // La caja está abierta, mantener los datos
                        string cajaAbiertaCodigo = Preferences.Get("CajaAbiertaCodigo", string.Empty);
                        CajaAbiertaInfoLabel.Text = $"Caja Abierta: {cajaAbiertaCodigo}";
                        CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Green"];
                        return;
                    }
                }
                else
                {
                    // No hay caja abierta localmente
                    CajaAbiertaInfoLabel.Text = "Caja: Cerrada";
                    CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando estado de caja: {ex.Message}");
                // En caso de error, asumir que no hay caja abierta
                CajaAbiertaInfoLabel.Text = "Caja: Cerrada";
                CajaAbiertaInfoLabel.TextColor = (Color)Application.Current.Resources["Red"];
            }
        }

        private async Task<bool> CheckCashboxStatusFromAPI(int cashboxId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Ajusta esta URL según tu API real
                    string requestUrl = $"https://localhost:7282/cargarCaja?idCaja={cashboxId}";

                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var cajaResponse = JsonSerializer.Deserialize<CajaResponse>(jsonResponse,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // Verificar que la caja existe y está abierta
                        if (cajaResponse != null && cajaResponse.IdCaja > 0 && cajaResponse.Estado == "I")
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando caja en API: {ex.Message}");
            }

            return false;
        }

        // Clase para deserializar la respuesta de cargarCaja
        public class CajaResponse
        {
            public int IdCaja { get; set; }
            public DateTime FechaApertura { get; set; }
            public int IdUsuario { get; set; }
            public double ImporteInicio { get; set; }
            public string Estado { get; set; }
            public string NombreUsuario { get; set; }
            public bool Nuevo { get; set; }
            public int? Codigo { get; set; }
            public bool IsNuevo { get; set; }
            public string Usuario { get; set; }
        }

        private void UpdateNavigationButtonStates()
        {
            // Simplificado para simulación - todos los botones habilitados
            // excepto aperturar caja si ya hay una abierta (simulado)
            int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);

            if (cajaAbiertaId > 0)
            {
                // Hay caja simulada abierta
                btnEntradaVehiculo.IsEnabled = true;
                btnConsultarVehiculo.IsEnabled = true;
                btnAperturarCaja.IsEnabled = false;
                btnCerrarCaja.IsEnabled = true;
            }
            else
            {
                // No hay caja simulada abierta
                btnEntradaVehiculo.IsEnabled = true; // Habilitado para simulación
                btnConsultarVehiculo.IsEnabled = true; // Habilitado para simulación
                btnAperturarCaja.IsEnabled = true;
                btnCerrarCaja.IsEnabled = false;
            }
        }

        private async void OnAperturarCajaClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AperturarCajaPage());
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
    }
}