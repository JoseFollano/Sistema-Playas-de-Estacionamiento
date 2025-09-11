using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace sistemaPlaya
{
    public partial class ConsultaVehiculoPage : ContentPage
    {
        public ConsultaVehiculoPage()
        {
            InitializeComponent();
        }

        private void OnPlacaTextChanged(object sender, TextChangedEventArgs e)
        {
            // Convertir a mayúsculas automáticamente
            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                string upperText = e.NewTextValue.ToUpper();
                if (PlacaEntry.Text != upperText)
                {
                    PlacaEntry.Text = upperText;
                    // Mover el cursor al final
                    PlacaEntry.CursorPosition = PlacaEntry.Text.Length;
                }
            }
        }

        private void OnPlacaCompleted(object sender, EventArgs e)
        {
            // Cuando se completa la entrada de placa, consultar automáticamente
            if (PlacaEntry.Text?.Length == 6)
            {
                // Llamar al método de consulta con el sender y e originales
                var buttonSender = new object();
                OnConsultarVehiculoClicked(buttonSender, e);
            }
        }

        private async void OnConsultarVehiculoClicked(object sender, EventArgs e)
        {
            string placa = PlacaEntry.Text?.Trim().ToUpper();

            if (string.IsNullOrEmpty(placa) || placa.Length != 6)
            {
                await DisplayAlert("Error", "Por favor, ingrese una placa válida de 6 caracteres.", "OK");
                return;
            }

            // Validar que solo contenga letras y números
            if (!IsValidPlaca(placa))
            {
                await DisplayAlert("Error", "La placa solo puede contener letras y números.", "OK");
                return;
            }

            ConsultarButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // SIMULACIÓN SIN API
                await Task.Delay(1000); // Simular tiempo de consulta

                // Mostrar resultados simulados
                MostrarResultadosSimulados(placa);

                /* DESCOMENTAR CUANDO TENGAS LAS APIS
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"{BaseApiUrl}consultarVehiculo?placa={placa}";

                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var vehiculoData = JsonSerializer.Deserialize<VehiculoResponse>(jsonResponse, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (vehiculoData != null)
                        {
                            MostrarResultados(vehiculoData);
                        }
                        else
                        {
                            await DisplayAlert("No encontrado", "No se encontró información para la placa ingresada.", "OK");
                            ResultadosFrame.IsVisible = false;
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo consultar la información del vehículo.", "OK");
                        ResultadosFrame.IsVisible = false;
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
                ResultadosFrame.IsVisible = false;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                ConsultarButton.IsEnabled = true;
            }
        }

        private bool IsValidPlaca(string placa)
        {
            foreach (char c in placa)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }

        private void MostrarResultadosSimulados(string placa)
        {
            // Datos simulados basados en la placa
            string tipoVehiculo = ObtenerTipoVehiculoSimulado(placa);
            string tarifa = ObtenerTarifaSimulada(tipoVehiculo);
            DateTime fechaIngreso = ObtenerFechaIngresoSimulada(placa);
            string tiempoEnPlaya = CalcularTiempoEnPlaya(fechaIngreso);

            PlacaResultadoLabel.Text = placa;
            TipoVehiculoLabel.Text = tipoVehiculo;
            TarifaLabel.Text = tarifa;
            FechaIngresoLabel.Text = fechaIngreso.ToString("dd/MM/yyyy HH:mm");
            TiempoLabel.Text = tiempoEnPlaya;

            // Colorear el tiempo según sea necesario
            if (tiempoEnPlaya.Contains("horas") && tiempoEnPlaya.Contains("minutos"))
            {
                // Extraer número de horas para colorear
                var horasStr = tiempoEnPlaya.Split(' ')[0];
                if (int.TryParse(horasStr, out int horas) && horas > 24)
                {
                    TiempoLabel.TextColor = (Color)Application.Current.Resources["Red"]; // Vehículo con mucho tiempo
                }
                else
                {
                    TiempoLabel.TextColor = (Color)Application.Current.Resources["Green"]; // Vehículo normal
                }
            }

            ResultadosFrame.IsVisible = true;
            AccionesButton.IsVisible = true;
        }

        private string ObtenerTipoVehiculoSimulado(string placa)
        {
            // Lógica simulada para determinar tipo de vehículo
            char primerCaracter = placa[0];
            switch (primerCaracter)
            {
                case 'A':
                case 'B':
                case 'C':
                    return "Automóvil";
                case 'D':
                case 'E':
                case 'F':
                    return "Motocicleta";
                case 'G':
                case 'H':
                case 'I':
                    return "Camioneta";
                case 'J':
                case 'K':
                case 'L':
                    return "Camión";
                default:
                    return "Automóvil";
            }
        }

        private string ObtenerTarifaSimulada(string tipoVehiculo)
        {
            switch (tipoVehiculo)
            {
                case "Automóvil":
                    return "S/ 5.00";
                case "Motocicleta":
                    return "S/ 3.00";
                case "Camioneta":
                    return "S/ 7.00";
                case "Camión":
                    return "S/ 10.00";
                default:
                    return "S/ 5.00";
            }
        }

        private DateTime ObtenerFechaIngresoSimulada(string placa)
        {
            // Generar una fecha de ingreso simulada basada en la placa
            // Hace entre 1 y 48 horas
            Random random = new Random(placa.GetHashCode());
            int horasAtras = random.Next(1, 49);
            return DateTime.Now.AddHours(-horasAtras);
        }

        private string CalcularTiempoEnPlaya(DateTime fechaIngreso)
        {
            TimeSpan diferencia = DateTime.Now - fechaIngreso;

            if (diferencia.TotalHours < 1)
            {
                return $"{diferencia.Minutes} minutos";
            }
            else if (diferencia.TotalHours < 24)
            {
                return $"{(int)diferencia.TotalHours} horas {diferencia.Minutes} minutos";
            }
            else
            {
                return $"{(int)diferencia.TotalDays} días {diferencia.Hours} horas";
            }
        }

        private async void OnAccionesClicked(object sender, EventArgs e)
        {
            string placa = PlacaResultadoLabel.Text;
            string tiempo = TiempoLabel.Text;

            // Simular que el vehículo está en playa
            bool estaEnPlaya = !tiempo.Contains("días") || (tiempo.Contains("días") && !tiempo.StartsWith("0"));

            if (estaEnPlaya)
            {
                // ActionSheet para vehículo en playa
                string action = await DisplayActionSheet("Opciones para vehículo en playa", "Cancelar", null,
                    "Pagar Tickets", "Factura", "Boleta");

                switch (action)
                {
                    case "Pagar Tickets":
                        await DisplayAlert("Pagar Tickets", "Abriendo pantalla de pago de tickets...", "OK");
                        // Aquí iría la navegación a la página de pago
                        break;
                    case "Factura":
                        await DisplayAlert("Factura", "Generando factura...", "OK");
                        // Aquí iría la lógica para generar factura
                        break;
                    case "Boleta":
                        await Navigation.PushAsync(new GenerarBoletaPage(placa, "boleta"));
                        break;
                }
            }
            else
            {
                // ActionSheet para vehículo fuera de playa
                string action = await DisplayActionSheet("Opciones para vehículo fuera de playa", "Cancelar", null,
                    "Registrar Entrada", "Ver Historial");

                switch (action)
                {
                    case "Registrar Entrada":
                        await DisplayAlert("Entrada", $"Registrando entrada del vehículo {placa}", "OK");
                        break;
                    case "Ver Historial":
                        await DisplayAlert("Historial", $"Historial del vehículo {placa}", "OK");
                        break;
                }
            }
        }
    }
}