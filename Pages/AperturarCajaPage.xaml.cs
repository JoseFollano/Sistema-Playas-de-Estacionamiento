using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace sistemaPlaya
{
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

    public partial class AperturarCajaPage : ContentPage
    {
        private const string BaseApiUrl = "https://localhost:7211/"; 

        private int _idUsuario;
        private string _nombreUsuario;

        public AperturarCajaPage()
        {
            InitializeComponent();

            _idUsuario = Preferences.Get("IdUsuario", 0);
            _nombreUsuario = Preferences.Get("UsuarioNombre", string.Empty);

            if (_idUsuario == 0 || string.IsNullOrEmpty(_nombreUsuario))
            {
                DisplayAlert("Error de Sesi�n", "No se encontr� informaci�n de usuario. Por favor, inicie sesi�n nuevamente.", "OK");
                return;
            }

            SetNewCajaMode();
        }

        private void SetNewCajaMode()
        {
            CodigoCajaEntry.Text = "";
            CodigoCajaEntry.IsEnabled = true;
            FechaAperturaDatePicker.Date = DateTime.Now;
            FechaAperturaDatePicker.IsEnabled = false;
            ImporteInicialEntry.Text = "";
            ImporteInicialEntry.IsEnabled = true;
            GuardarCajaButton.Text = "Guardar y Abrir Caja";
            GuardarCajaButton.IsEnabled = true;
        }

        private void SetLoadedCajaMode(CajaResponse caja)
        {
            CodigoCajaEntry.Text = caja.Codigo?.ToString() ?? caja.IdCaja.ToString();
            CodigoCajaEntry.IsEnabled = false;
            FechaAperturaDatePicker.Date = caja.FechaApertura;
            FechaAperturaDatePicker.IsEnabled = false;
            ImporteInicialEntry.Text = caja.ImporteInicio.ToString("N2");
            ImporteInicialEntry.IsEnabled = false;
            GuardarCajaButton.Text = "Caja Abierta (No editable)";
            GuardarCajaButton.IsEnabled = false;
        }

        private void OnNuevaCajaClicked(object sender, EventArgs e)
        {
            SetNewCajaMode();
        }

        private async void OnCargarCajaClicked(object sender, EventArgs e)
        {
            if (!int.TryParse(CodigoCajaEntry.Text?.Trim(), out int idCajaACargar) || idCajaACargar <= 0)
            {
                await DisplayAlert("Error", "Por favor, ingrese un C�digo de Caja num�rico v�lido para cargar.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            CodigoCajaEntry.IsEnabled = false;
            GuardarCajaButton.IsEnabled = false;

            try
            {
                // SIMULACI�N SIN API
                await Task.Delay(1000); // Simular tiempo de carga

                // Simulaci�n de carga de caja existente
                var cajaCargada = new CajaResponse
                {
                    IdCaja = idCajaACargar,
                    Codigo = idCajaACargar,
                    FechaApertura = DateTime.Now.AddDays(-1),
                    ImporteInicio = 100.00,
                    Estado = "I", // Caja abierta
                    IdUsuario = _idUsuario,
                    NombreUsuario = _nombreUsuario
                };

                SetLoadedCajaMode(cajaCargada);

                // Guardar informaci�n completa de la caja cargada
                Preferences.Set("CajaAbiertaId", cajaCargada.IdCaja);
                Preferences.Set("CajaAbiertaCodigo", cajaCargada.Codigo?.ToString());
                Preferences.Set("CajaAbiertaEstado", cajaCargada.Estado);
                Preferences.Set("CajaImporteInicio", cajaCargada.ImporteInicio);
                Preferences.Set("CajaFechaApertura", cajaCargada.FechaApertura.ToString("yyyy-MM-dd"));
                Preferences.Set("CajaUsuarioId", cajaCargada.IdUsuario);
                Preferences.Set("CajaUsuarioNombre", cajaCargada.NombreUsuario ?? cajaCargada.Usuario);

                await DisplayAlert("Caja Cargada", $"Caja {cajaCargada.Codigo} cargada con �xito. Estado: Abierta.", "OK");

                
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurri� un error al cargar la caja: {ex.Message}", "OK");
                SetNewCajaMode();
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                CodigoCajaEntry.IsEnabled = true;
                GuardarCajaButton.IsEnabled = true;
            }
        }

        private async void OnGuardarCajaClicked(object sender, EventArgs e)
        {
            if (!double.TryParse(ImporteInicialEntry.Text, out double importeInicial) || importeInicial <= 0)
            {
                await DisplayAlert("Error", "El importe inicial debe ser un n�mero mayor que cero.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            GuardarCajaButton.IsEnabled = false;

            try
            {
                int idEmpresa = Preferences.Get("IdEmpresa", 1); // Ajusta el valor por defecto si es necesario

                // Construir un objeto con valores v�lidos para evitar problemas de binding en el backend
                var cajaData = new
                {
                    idCaja = 0,
                    // Enviar una fecha v�lida en formato ISO 8601 (UTC) para que el model binder del backend pueda parsearla
                    fechaApertura = DateTime.UtcNow.ToString("o"),
                    idUsuario = _idUsuario,
                    importeInicio = importeInicial,
                    // Puedes dejar estado y nombreUsuario vac�os si el backend los genera, pero enviarlos no causa problema
                    estado = "I",
                    nombreUsuario = _nombreUsuario,
                    nuevo = true
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                string jsonContent = JsonSerializer.Serialize(cajaData, options);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"{BaseApiUrl}guardarCaja?idEmpresa={idEmpresa}";

                    // Asegurarse de que el Content-Type est� correctamente establecido (StringContent ya lo hace)
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        // La API devuelve el n�mero (id) como texto; intentamos parsearlo
                        if (int.TryParse(jsonResponse.Replace("\"", ""), out int idCajaGenerado))
                        {
                            string codigoCajaGenerado = idCajaGenerado.ToString();

                            Preferences.Set("CajaAbiertaId", idCajaGenerado);
                            Preferences.Set("CajaAbiertaCodigo", codigoCajaGenerado);
                            Preferences.Set("CajaAbiertaEstado", "I");
                            Preferences.Set("CajaImporteInicio", importeInicial);
                            Preferences.Set("CajaFechaApertura", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                            Preferences.Set("CajaUsuarioId", _idUsuario);
                            Preferences.Set("CajaUsuarioNombre", _nombreUsuario);

                            await DisplayAlert("�xito", $"Caja '{codigoCajaGenerado}' abierta con �xito. Importe inicial: {importeInicial:C}", "OK");

                            // Vuelve a la p�gina principal
                            await Navigation.PopAsync();
                        }
                        else
                        {
                            await DisplayAlert("Error de Datos", $"La API devolvi� un formato inesperado: '{jsonResponse}'. Se esperaba un n�mero entero (IdCaja).", "OK");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error de API (Guardar Caja)", $"No se pudo guardar la caja. C�digo: {response.StatusCode}. Detalle: {errorContent}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurri� un error al guardar la caja: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                GuardarCajaButton.IsEnabled = true;
            }
        }
    }
}