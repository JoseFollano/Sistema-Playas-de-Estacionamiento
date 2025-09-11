using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace sistemaPlaya
{
    public partial class GenerarBoletaPage : ContentPage
    {
        private string _placa;
        private string _tipoDocumento; // "boleta" o "factura"
        private double _importeCalculado;

        public GenerarBoletaPage(string placa, string tipoDocumento)
        {
            InitializeComponent();
            _placa = placa;
            _tipoDocumento = tipoDocumento;

            // Configurar la página según el tipo de documento
            ConfigurarPagina();

            // Establecer fecha actual
            FechaEmisionLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");

            // Generar número de documento simulado
            GenerarNumeroDocumento();

            // Calcular importe simulado
            CalcularImporteSimulado();
        }

        private void ConfigurarPagina()
        {
            if (_tipoDocumento == "factura")
            {
                TituloLabel.Text = "Generar Factura";
                // Para factura, podrías mostrar campos adicionales
            }
            else
            {
                TituloLabel.Text = "Generar Boleta";
            }
        }

        private void GenerarNumeroDocumento()
        {
            // Generar número de documento simulado
            Random random = new Random();
            string serie = "001";
            string numero = random.Next(10000, 99999).ToString();
            NroDocumentoLabel.Text = $"{serie}-{numero}";
        }

        private void CalcularImporteSimulado()
        {
            // Calcular importe basado en la placa (simulación)
            Random random = new Random(_placa.GetHashCode());
            int horas = random.Next(1, 12); // Entre 1 y 12 horas

            // Determinar tarifa según tipo de vehículo (simulado)
            double tarifaPorHora = ObtenerTarifaPorTipoVehiculo(_placa);
            _importeCalculado = horas * tarifaPorHora;

            ImporteTotalLabel.Text = $"S/ {_importeCalculado:F2}";
        }

        private double ObtenerTarifaPorTipoVehiculo(string placa)
        {
            // Simulación de tarifas según placa
            char primerCaracter = placa[0];
            switch (primerCaracter)
            {
                case 'A':
                case 'B':
                case 'C':
                    return 5.00; // Automóvil
                case 'D':
                case 'E':
                case 'F':
                    return 3.00; // Motocicleta
                case 'G':
                case 'H':
                case 'I':
                    return 7.00; // Camioneta
                case 'J':
                case 'K':
                case 'L':
                    return 10.00; // Camión
                default:
                    return 5.00; // Automóvil por defecto
            }
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            string dni = NroDocumentoEntry.Text?.Trim();

            if (string.IsNullOrEmpty(dni) || dni.Length != 8)
            {
                await DisplayAlert("Error", "Por favor, ingrese un DNI válido de 8 dígitos.", "OK");
                return;
            }

            // Validar que solo contenga números
            if (!EsNumero(dni))
            {
                await DisplayAlert("Error", "El DNI solo puede contener números.", "OK");
                return;
            }

            BuscarButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // SIMULACIÓN DE BÚSQUEDA DE CLIENTE
                await Task.Delay(1000); // Simular tiempo de búsqueda

                // Simular resultados de búsqueda
                string nombreCliente = ObtenerNombreClienteSimulado(dni);
                ClienteEntry.Text = nombreCliente;
                DireccionEntry.Text = ObtenerDireccionSimulada(dni);

                await DisplayAlert("Éxito", "Cliente encontrado.", "OK");

                /* DESCOMENTAR CUANDO TENGAS LA API
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"{BaseApiUrl}buscarCliente?dni={dni}";
                    
                    HttpResponseMessage response = await client.GetAsync(requestUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var clienteData = JsonSerializer.Deserialize<ClienteResponse>(jsonResponse,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (clienteData != null)
                        {
                            ClienteEntry.Text = clienteData.Nombre;
                            DireccionEntry.Text = clienteData.Direccion;
                        }
                    }
                    else
                    {
                        await DisplayAlert("No encontrado", "No se encontró cliente con ese DNI.", "OK");
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al buscar cliente: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                BuscarButton.IsEnabled = true;
            }
        }

        private string ObtenerNombreClienteSimulado(string dni)
        {
            // Simulación de nombres basados en DNI
            string[] nombres = {
                "Juan Pérez Gonzales",
                "María López Torres",
                "Carlos García Ruiz",
                "Ana Martínez Silva",
                "Luis Rodríguez Castro",
                "Elena Sánchez Morales",
                "Pedro Fernández Vargas",
                "Carmen Jiménez Herrera"
            };

            Random random = new Random(dni.GetHashCode());
            return nombres[random.Next(nombres.Length)];
        }

        private string ObtenerDireccionSimulada(string dni)
        {
            // Simulación de direcciones basadas en DNI
            string[] direcciones = {
                "Av. Principal 123, Lima",
                "Calle Secundaria 456, Miraflores",
                "Jr. Comercial 789, Surco",
                "Av. Industrial 321, San Isidro",
                "Calle Residencial 654, Barranco"
            };

            Random random = new Random(dni.GetHashCode() + 1);
            return direcciones[random.Next(direcciones.Length)];
        }

        private bool EsNumero(string texto)
        {
            foreach (char c in texto)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrEmpty(NroDocumentoEntry.Text) || NroDocumentoEntry.Text.Length != 8)
            {
                await DisplayAlert("Error", "Por favor, ingrese un DNI válido de 8 dígitos.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(ClienteEntry.Text))
            {
                await DisplayAlert("Error", "Por favor, busque y seleccione un cliente.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(DireccionEntry.Text))
            {
                bool continuar = await DisplayAlert("Advertencia",
                    "No ha ingresado dirección. ¿Desea continuar?", "Sí", "No");
                if (!continuar)
                    return;
            }

            GuardarButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // SIMULACIÓN DE GUARDADO
                await Task.Delay(1500); // Simular tiempo de guardado

                await DisplayAlert("Éxito",
                    $"{(_tipoDocumento == "factura" ? "Factura" : "Boleta")} generada correctamente.", "OK");

                // Generar PDF simulado
                await GenerarDocumentoPDF();

                // Volver a la página anterior
                await Navigation.PopAsync();

                /* DESCOMENTAR CUANDO TENGAS LA API
                using (HttpClient client = new HttpClient())
                {
                    var documentoData = new
                    {
                        tipo = _tipoDocumento,
                        nroDocumento = NroDocumentoLabel.Text,
                        dniCliente = NroDocumentoEntry.Text,
                        cliente = ClienteEntry.Text,
                        direccion = DireccionEntry.Text,
                        observacion = ObservacionEditor.Text,
                        importe = _importeCalculado,
                        fechaEmision = DateTime.Now,
                        placa = _placa
                    };

                    string jsonContent = JsonSerializer.Serialize(documentoData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    string requestUrl = $"{BaseApiUrl}guardarDocumento";
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Éxito", 
                            $"{(_tipoDocumento == "factura" ? "Factura" : "Boleta")} generada correctamente.", "OK");
                        
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo generar el documento.", "OK");
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al generar documento: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                GuardarButton.IsEnabled = true;
            }
        }

        private async Task GenerarDocumentoPDF()
        {
            // Aquí iría la lógica para generar el PDF
            // Similar a lo que hicimos en CierreCajaPage
            await Task.Delay(500); // Simular generación
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert("Confirmar",
                "¿Está seguro que desea cancelar? Los datos no guardados se perderán.", "Sí", "No");

            if (confirmar)
            {
                await Navigation.PopAsync();
            }
        }
    }
}