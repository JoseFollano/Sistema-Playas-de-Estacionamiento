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
        // private const string BaseApiUrl = "https://localhost:7282/"; // <--- AJUSTA ESTA URL CUANDO TENGAS LAS APIS

        private int _idUsuario;
        private string _nombreUsuario;

        public AperturarCajaPage()
        {
            InitializeComponent();

            _idUsuario = Preferences.Get("IdUsuario", 0);
            _nombreUsuario = Preferences.Get("UsuarioNombre", string.Empty);

            if (_idUsuario == 0 || string.IsNullOrEmpty(_nombreUsuario))
            {
                DisplayAlert("Error de Sesión", "No se encontró información de usuario. Por favor, inicie sesión nuevamente.", "OK");
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
                await DisplayAlert("Error", "Por favor, ingrese un Código de Caja numérico válido para cargar.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            CodigoCajaEntry.IsEnabled = false;
            GuardarCajaButton.IsEnabled = false;

            try
            {
                // SIMULACIÓN SIN API
                await Task.Delay(1000); // Simular tiempo de carga

                // Simulación de carga de caja existente
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

                // Guardar información completa de la caja cargada
                Preferences.Set("CajaAbiertaId", cajaCargada.IdCaja);
                Preferences.Set("CajaAbiertaCodigo", cajaCargada.Codigo?.ToString());
                Preferences.Set("CajaAbiertaEstado", cajaCargada.Estado);
                Preferences.Set("CajaImporteInicio", cajaCargada.ImporteInicio);
                Preferences.Set("CajaFechaApertura", cajaCargada.FechaApertura.ToString("yyyy-MM-dd"));
                Preferences.Set("CajaUsuarioId", cajaCargada.IdUsuario);
                Preferences.Set("CajaUsuarioNombre", cajaCargada.NombreUsuario ?? cajaCargada.Usuario);

                await DisplayAlert("Caja Cargada", $"Caja {cajaCargada.Codigo} cargada con éxito. Estado: Abierta.", "OK");

                /* DESCOMENTAR CUANDO TENGAS LAS APIS
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = $"{BaseApiUrl}cargarCaja?idCaja={idCajaACargar}";

                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        CajaResponse cajaCargada = JsonSerializer.Deserialize<CajaResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (cajaCargada != null && cajaCargada.IdCaja > 0)
                        {
                            if (cajaCargada.Estado == "I")
                            {
                                SetLoadedCajaMode(cajaCargada);

                                Preferences.Set("CajaAbiertaId", cajaCargada.IdCaja);
                                Preferences.Set("CajaAbiertaCodigo", cajaCargada.Codigo?.ToString());
                                Preferences.Set("CajaAbiertaEstado", cajaCargada.Estado);
                                Preferences.Set("CajaImporteInicio", cajaCargada.ImporteInicio);
                                Preferences.Set("CajaFechaApertura", cajaCargada.FechaApertura.ToString("yyyy-MM-dd"));
                                Preferences.Set("CajaUsuarioId", cajaCargada.IdUsuario);
                                Preferences.Set("CajaUsuarioNombre", cajaCargada.NombreUsuario ?? cajaCargada.Usuario);

                                await DisplayAlert("Caja Cargada", $"Caja {cajaCargada.Codigo} cargada con éxito. Estado: Abierta.", "OK");
                            }
                            else
                            {
                                await DisplayAlert("Advertencia", $"La caja {cajaCargada.Codigo} no está abierta. Estado: {cajaCargada.Estado}.", "OK");
                                SetNewCajaMode();
                            }
                        }
                        else
                        {
                            await DisplayAlert("Error", "Caja no encontrada o datos inválidos en la respuesta.", "OK");
                            SetNewCajaMode();
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error de API (Cargar Caja)", $"No se pudo cargar la caja. Código: {response.StatusCode}. Detalle: {errorContent}", "OK");
                        SetNewCajaMode();
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error al cargar la caja: {ex.Message}", "OK");
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
                await DisplayAlert("Error", "El importe inicial debe ser un número mayor que cero.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            GuardarCajaButton.IsEnabled = false;

            try
            {
                // SIMULACIÓN SIN API
                await Task.Delay(1500); // Simular tiempo de guardado

                // Simulación de guardado de nueva caja
                int idCajaGenerado = new Random().Next(1000, 9999); // Generar ID aleatorio
                string codigoCajaGenerado = idCajaGenerado.ToString();

                // Guardar información de la caja en Preferences
                Preferences.Set("CajaAbiertaId", idCajaGenerado);
                Preferences.Set("CajaAbiertaCodigo", codigoCajaGenerado);
                Preferences.Set("CajaAbiertaEstado", "I");
                Preferences.Set("CajaImporteInicio", importeInicial);
                Preferences.Set("CajaFechaApertura", FechaAperturaDatePicker.Date.ToString("yyyy-MM-dd"));
                Preferences.Set("CajaUsuarioId", _idUsuario);
                Preferences.Set("CajaUsuarioNombre", _nombreUsuario);

                await DisplayAlert("Éxito", $"Caja '{codigoCajaGenerado}' abierta con éxito. Importe inicial: {importeInicial:C}", "OK");

                // Vuelve a la página principal
                await Navigation.PopAsync();

                /* DESCOMENTAR CUANDO TENGAS LAS APIS
                using (HttpClient client = new HttpClient())
                {
                    var cajaData = new
                    {
                        idCaja = 0,
                        fechaApertura = FechaAperturaDatePicker.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        idUsuario = _idUsuario,
                        importeInicio = importeInicial,
                        estado = "I",
                        nombreUsuario = _nombreUsuario,
                        nuevo = true
                    };

                    string jsonContent = JsonSerializer.Serialize(cajaData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync($"{BaseApiUrl}guardarCaja", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        int idCajaGenerado = 0;

                        if (int.TryParse(jsonResponse.Replace("\"", ""), out idCajaGenerado))
                        {
                            string codigoCajaGenerado = idCajaGenerado.ToString();

                            Preferences.Set("CajaAbiertaId", idCajaGenerado);
                            Preferences.Set("CajaAbiertaCodigo", codigoCajaGenerado);
                            Preferences.Set("CajaAbiertaEstado", "I");
                            Preferences.Set("CajaImporteInicio", importeInicial);
                            Preferences.Set("CajaFechaApertura", FechaAperturaDatePicker.Date.ToString("yyyy-MM-dd"));
                            Preferences.Set("CajaUsuarioId", _idUsuario);
                            Preferences.Set("CajaUsuarioNombre", _nombreUsuario);

                            await DisplayAlert("Éxito", $"Caja '{codigoCajaGenerado}' abierta con éxito. Importe inicial: {importeInicial:C}", "OK");

                            await Navigation.PopAsync();
                        }
                        else
                        {
                            await DisplayAlert("Error de Datos", $"La API devolvió un formato inesperado: '{jsonResponse}'. Se esperaba un número entero (IdCaja).", "OK");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error de API (Guardar Caja)", $"No se pudo guardar la caja. Código: {response.StatusCode}. Detalle: {errorContent}", "OK");
                    }
                }
                */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error al guardar la caja: {ex.Message}", "OK");
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