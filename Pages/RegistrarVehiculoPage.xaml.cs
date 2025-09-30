using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace sistemaPlaya.Pages;

public partial class RegistrarVehiculoPage : ContentPage, INotifyPropertyChanged
{
    // Diccionario para controlar vehículos en playa
    private Dictionary<string, VehiculoInfo> _vehiculosEnPlaya = new();

    // Tarifas por tipo de vehículo (fallback local)

    private readonly HttpClient _httpClient = new();
    private readonly string _baseApi = "https://localhost:7211/"; // ajustar si es necesario

    // Mapeo de códigos de tipo (p. ej. "0001") a texto mostrado (p. ej. "AUTOMOVIL")
    private readonly Dictionary<string, string> _mapaTipoCodigoADisplay = new();

    private string _estadoCaja = "ABIERTA";
    public string EstadoCaja
    {
        get => _estadoCaja;
        set
        {
            _estadoCaja = value;
            OnPropertyChanged();
        }
    }

    public int NumeroEstacionamiento { get; set; } = 100;

    private int _ocupados = 15;
    public int Ocupados
    {
        get => _ocupados;
        set
        {
            _ocupados = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Disponibles));
        }
    }

    public int Disponibles => NumeroEstacionamiento - Ocupados;

    private string _placa;
    public string Placa
    {
        get => _placa;
        set
        {
            var upperPlaca = value?.ToUpper();
            if (_placa != upperPlaca)
            {
                _placa = upperPlaca;
                OnPropertyChanged();
                // Notificar también la propiedad que controla el botón Cambiar Plan
                OnPropertyChanged(nameof(CambiarPlanHabilitado));

                ActualizarEstadoIngreso();
                // Validar automáticamente si la placa tiene 6 caracteres
                if (!string.IsNullOrWhiteSpace(_placa) && _placa.Length == 6)
                {
                    _ = VerificarPlacaAsync();
                }
            }
        }
    }

    // Propiedad pública que indica si el botón Cambiar Plan debe estar habilitado
    public bool CambiarPlanHabilitado => !string.IsNullOrWhiteSpace(Placa);

    private string _estadoIngreso = "";
    public string EstadoIngreso
    {
        get => _estadoIngreso;
        set
        {
            _estadoIngreso = value;
            OnPropertyChanged();
        }
    }

    private Microsoft.Maui.Graphics.Color _estadoIngresoColor = Microsoft.Maui.Graphics.Colors.Black;
    public Microsoft.Maui.Graphics.Color EstadoIngresoColor
    {
        get => _estadoIngresoColor;
        set
        {
            _estadoIngresoColor = value;
            OnPropertyChanged();
        }
    }

    // Inicialmente vacío; se cargará desde la API getComboTipoVehiculos
    public ObservableCollection<string> TiposVehiculo { get; set; } = new ObservableCollection<string>();

    private string _tipoVehiculo;
    public string TipoVehiculo
    {
        get => _tipoVehiculo;
        set
        {
            _tipoVehiculo = value;
            OnPropertyChanged();
            ActualizarTarifa();
        }
    }

    private decimal _tarifaHora;
    public decimal TarifaHora
    {
        get => _tarifaHora;
        set
        {
            _tarifaHora = value;
            OnPropertyChanged();
        }
    }

    private string _observacion;
    public string Observacion
    {
        get => _observacion;
        set
        {
            _observacion = value;
            OnPropertyChanged();
        }
    }

    private bool _mostrarPago;
    public bool MostrarPago
    {
        get => _mostrarPago;
        set
        {
            _mostrarPago = value;
            OnPropertyChanged();
        }
    }

    private string _tiempoEstacionado;
    public string TiempoEstacionado
    {
        get => _tiempoEstacionado;
        set
        {
            _tiempoEstacionado = value;
            OnPropertyChanged();
        }
    }

    private decimal _totalPagar;
    public decimal TotalPagar
    {
        get => _totalPagar;
        set
        {
            _totalPagar = value;
            OnPropertyChanged();
        }
    }

    private string _numeroTicket;
    public string NumeroTicket
    {
        get => _numeroTicket;
        set { _numeroTicket = value; OnPropertyChanged(); }
    }

    private DateTime _fechaIngreso;
    public DateTime FechaIngreso
    {
        get => _fechaIngreso;
        set { _fechaIngreso = value; OnPropertyChanged(); }
    }

    // Control de número de ticket secuencial
    private int _contadorTicket = 0;
    private string _ultimoTicketRegistrado = null;

    // Bandera para saber si se cambió el plan antes de pagar
    private bool _planCambiado = false;

    // Propiedad para habilitar/deshabilitar el botón Registrar
    private bool _registrarHabilitado = true;
    public bool RegistrarHabilitado
    {
        get => _registrarHabilitado;
        set { _registrarHabilitado = value; OnPropertyChanged(); }
    }

    public RegistrarVehiculoPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Mostrar estado de caja según Preferences (igual que MainPage)
        int idCaja = Preferences.Get("CajaAbiertaId", 0);
        EstadoCaja = idCaja > 0 ? $"ABIERTA (ID: {idCaja})" : "CERRADA";

        // Cargar tipos de vehiculo desde la API (no bloquear constructor)
        _ = LoadTiposVehiculoAsync();

        // Agregar algunos vehículos de ejemplo para probar la funcionalidad localmente
        _vehiculosEnPlaya["ABC123"] = new VehiculoInfo
        {
            HoraIngreso = DateTime.Now.AddHours(-2),
            TarifaHora = 5.00m,
            Tipo = "Automóvil",
            Observacion = "Vehículo de prueba"
        };
    }

    private void ActualizarEstadoIngreso()
    {
        if (string.IsNullOrWhiteSpace(Placa))
        {
            EstadoIngreso = "";
            EstadoIngresoColor = Microsoft.Maui.Graphics.Colors.Black;
            MostrarPago = false;
            TipoVehiculo = null;
            RegistrarHabilitado = true;
        }
        else if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            // Si está en el diccionario local
            EstadoIngreso = "SALIDA DE VEHÍCULO";
            EstadoIngresoColor = Microsoft.Maui.Graphics.Colors.Orange;
            MostrarPago = true;
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];
            var tiempo = DateTime.Now - vehiculo.HoraIngreso;
            TiempoEstacionado = $"{tiempo.Hours}h {tiempo.Minutes}m";
            var horas = (decimal)Math.Ceiling(tiempo.TotalHours);
            TotalPagar = horas * vehiculo.TarifaHora;
            Observacion = vehiculo.Observacion;
            TipoVehiculo = vehiculo.Tipo;
            RegistrarHabilitado = false; // Deshabilitar botón Registrar si ya está registrado
        }
        else
        {
            // Si no está localmente, esperar a que el usuario complete la placa y luego
            // se hará una verificación contra la API en OnPlacaCompleted.
            EstadoIngreso = "INGRESO DE VEHÍCULO";
            EstadoIngresoColor = Microsoft.Maui.Graphics.Colors.Green;
            MostrarPago = false;
            Observacion = ""; // Limpiar observación para nuevos ingresos
            TipoVehiculo = null; // Limpiar tipo de vehículo para nuevos ingresos
            RegistrarHabilitado = true;
        }
    }

    private void ActualizarTarifa()
    {
        if (!string.IsNullOrEmpty(TipoVehiculo))
        {
            // Buscar el código correspondiente al tipo seleccionado
            string codigoVehiculo = "";
            foreach (var kvp in _mapaTipoCodigoADisplay)
            {
                if (kvp.Value == TipoVehiculo)
                {
                    codigoVehiculo = kvp.Key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(codigoVehiculo))
            {
                // Llamar a la API para obtener la tarifa real
                _ = ObtenerTarifaDesdeAPI(codigoVehiculo);
            }
        }
    }

    private async void OnPlacaCompleted(object sender, EventArgs e)
    {
        // Ya no es necesario validar aquí, solo enfocar el picker
        PickerTipoVehiculo?.Focus();
    }

    // Verifica la placa contra la API ValidaPlacaExiste
    private async Task VerificarPlacaAsync()
    {
        if (string.IsNullOrWhiteSpace(Placa))
            return;

        try
        {
            int idUsuario = Preferences.Get("IdUsuario", 0);
            if (idUsuario == 0)
            {
                await DisplayAlert("Error", "No se encontró el usuario. Debe iniciar sesión nuevamente.", "OK");
                return;
            }
            var placaEsc = Uri.EscapeDataString(Placa);
            var url = $"{_baseApi}ValidaPlacaExiste?placa={placaEsc}&idUsuario={idUsuario}";
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                return;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ValidaPlacaResponse>(json, opciones);

            if (data == null)
                return;

            // Asignar todos los datos recibidos
            if ((data.Parqueado?.ToUpper() == "S") || (data.Modo?.ToLower().Contains("salida") == true))
            {
                EstadoIngreso = "SALIDA DE VEHÍCULO";
                EstadoIngresoColor = Colors.Orange;
                MostrarPago = true;
                TiempoEstacionado = data.Tiempo ?? "";
                TotalPagar = data.TotalCobrar != 0 ? data.TotalCobrar : data.Precio * 1;
                Observacion = data.Observacion ?? "";
                NumeroTicket = data.NroTicket;
                FechaIngreso = data.FechaIngreso;
                // Tarifa por hora
                TarifaHora = data.Precio;
                // Tipo de vehículo
                if (!string.IsNullOrEmpty(data.TipoVehiculo) && _mapaTipoCodigoADisplay.TryGetValue(data.TipoVehiculo, out var display))
                {
                    TipoVehiculo = display;
                }
                else
                {
                    TipoVehiculo = data.TipoVehiculo;
                }
                RegistrarHabilitado = false;
            }
            else
            {
                EstadoIngreso = "INGRESO DE VEHÍCULO";
                EstadoIngresoColor = Colors.Green;
                MostrarPago = false;
                Observacion = "";
                TipoVehiculo = null;
                TarifaHora = 0;
                NumeroTicket = "";
                FechaIngreso = DateTime.MinValue;
                TiempoEstacionado = "";
                TotalPagar = 0;
                RegistrarHabilitado = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al validar placa: {ex.Message}");
        }
    }

    // Cargar tipos de vehiculos desde la API getComboTipoVehiculos
    private async Task LoadTiposVehiculoAsync()
    {
        try
        {
            var url = $"{_baseApi}getComboTipoVehiculos?idEmpresa=1";
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return;

            var json = await resp.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<TipoVehiculoItem>>(json, opciones);
            if (items == null)
                return;

            // Actualizar colección observable en UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TiposVehiculo.Clear();
                _mapaTipoCodigoADisplay.Clear();
                foreach (var it in items)
                {
                    TiposVehiculo.Add(it.Display);
                    if (!string.IsNullOrEmpty(it.Value) && !_mapaTipoCodigoADisplay.ContainsKey(it.Value))
                        _mapaTipoCodigoADisplay[it.Value] = it.Display;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar tipos de vehículo: {ex.Message}");
        }
    }

    private async Task ObtenerTarifaDesdeAPI(string itemVehiculo)
    {
        try
        {
            var url = $"{_baseApi}validarTarifa?itemVehiculo={itemVehiculo}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // La API devuelve solo un número, no un objeto
                if (decimal.TryParse(json, out decimal precio))
                {
                    // Actualizar la propiedad con notificación de cambio
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TarifaHora = precio;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener tarifa: {ex.Message}");
        }
    }

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Placa) || Placa.Length != 6)
        {
            await DisplayAlert("Error", "La placa debe tener exactamente 6 caracteres", "OK");
            EntryPlaca.Focus();
            return;
        }

        // Si el vehículo ya está registrado y el botón está habilitado (tras cambiar plan)
        if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()) && RegistrarHabilitado)
        {
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];
            vehiculo.Tipo = TipoVehiculo;
            vehiculo.TarifaHora = TarifaHora;
            vehiculo.Observacion = Observacion;
            // Actualizar la información mostrada
            ActualizarEstadoIngreso();
            await DisplayAlert("Éxito", "Actualización exitosa: el plan y la tarifa han sido actualizados.", "OK");
            RegistrarHabilitado = false;
            return;
        }

        if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            // Salida de vehículo local
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];
            var tiempo = DateTime.Now - vehiculo.HoraIngreso;
            var horas = (decimal)Math.Ceiling(tiempo.TotalHours);
            var total = horas * vehiculo.TarifaHora;

            // Mostrar confirmación de pago
            bool confirmar = await DisplayAlert("Confirmar salida",
                $"Placa: {Placa}\nTipo: {vehiculo.Tipo}\n Tiempo: {tiempo.Hours}h {tiempo.Minutes}m\n" +
                $"Total a pagar: S/. {total:F2}\n\n¿Confirmar salida?", "Sí", "No");

            if (confirmar)
            {
                _vehiculosEnPlaya.Remove(Placa.ToUpper());
                Ocupados--;
                NumeroTicket = null;
                _ultimoTicketRegistrado = null;
                await DisplayAlert("Éxito", "Salida de vehículo registrada correctamente", "OK");
                LimpiarFormulario();
            }
        }
        else
        {
            // Ingreso de vehículo - INTEGRAR CON LA NUEVA API
            if (string.IsNullOrEmpty(TipoVehiculo))
            {
                await DisplayAlert("Error", "Debe seleccionar un tipo de vehículo", "OK");
                PickerTipoVehiculo.Focus();
                return;
            }

            // Llamar a la API de registro de ingreso
            bool registroExitoso = await RegistrarIngresoEnAPI();
            if (registroExitoso)
            {
                // Generar número de ticket secuencial (como fallback)
                _contadorTicket++;
                var ticketSecuencial = _contadorTicket.ToString("D6");
                NumeroTicket = ticketSecuencial;
                _ultimoTicketRegistrado = ticketSecuencial;

                _vehiculosEnPlaya[Placa.ToUpper()] = new VehiculoInfo
                {
                    NumeroTicket = ticketSecuencial,
                    HoraIngreso = DateTime.Now,
                    TarifaHora = TarifaHora,
                    Tipo = TipoVehiculo,
                    Observacion = Observacion
                };

                Ocupados++;

                // --- Generar ticket PDF ---
                try
                {
                    var pdf = new PdfDocument();
                    var page = pdf.AddPage();
                    page.Width = 300;
                    page.Height = 200;
                    var gfx = XGraphics.FromPdfPage(page);
                    var font = new XFont("Arial", 14, XFontStyle.Bold);
                    var fontSmall = new XFont("Arial", 10, XFontStyle.Regular);

                    // Tipo de vehículo
                    gfx.DrawString($"Tipo de vehículo: {TipoVehiculo}", font, XBrushes.Black, new XRect(10, 20, page.Width, 20), XStringFormats.TopLeft);
                    // Placa
                    gfx.DrawString($"Placa: {Placa}", fontSmall, XBrushes.Black, new XRect(10, 45, page.Width, 20), XStringFormats.TopLeft);
                    // Código de operación
                    gfx.DrawString($"Código de operación: {NumeroTicket}", fontSmall, XBrushes.Black, new XRect(10, 65, page.Width, 20), XStringFormats.TopLeft);

                    // Código de barras para la placa
                    var barcodeWriter = new BarcodeWriterPixelData
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new EncodingOptions
                        {
                            Height = 60,
                            Width = 200,
                            Margin = 2
                        }
                    };
                    var pixelData = barcodeWriter.Write(Placa);
                    using (var ms = new MemoryStream())
                    {
                        // Convertir PixelData a PNG usando ImageSharp
                        using (var image = new Image<Rgba32>(pixelData.Width, pixelData.Height))
                        {
                            // Copy pixel data (ZXing returns BGRA32 or similar; map bytes to ImageSharp pixel)
                            var bytes = pixelData.Pixels;
                            // ZXing PixelData is in BGRA32 order; ImageSharp expects Rgba32
                            for (int y = 0; y < pixelData.Height; y++)
                            {
                                for (int x = 0; x < pixelData.Width; x++)
                                {
                                    int idx = (y * pixelData.Width + x) * 4;
                                    byte b = bytes[idx + 0];
                                    byte g = bytes[idx + 1];
                                    byte r = bytes[idx + 2];
                                    byte a = bytes[idx + 3];
                                    image[x, y] = new Rgba32(r, g, b, a);
                                }
                            }

                            image.Save(ms, PngFormat.Instance);
                            ms.Position = 0;
                            var img = XImage.FromStream(() => ms);
                            gfx.DrawImage(img, 50, 90, 200, 60);
                        }
                    }

                    // Guardar PDF en Documents
                    var fileName = $"Ticket_{Placa}_{NumeroTicket}.pdf";
                    var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var filePath = Path.Combine(documents, fileName);
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        pdf.Save(fs);
                    }
                    await DisplayAlert("Ticket generado", $"El ticket se guardó en: {filePath}", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error al generar ticket", ex.Message, "OK");
                }
                // --- Fin ticket PDF ---

                LimpiarFormulario();
            }
        }
    }

    // Nuevo método para registrar ingreso en la API
    private async Task<bool> RegistrarIngresoEnAPI()
{
    try
    {
        // Obtener datos de la caja abierta desde Preferences
        int idCaja = Preferences.Get("CajaAbiertaId", 0);
        int idUsuario = Preferences.Get("IdUsuario", 0);
            
            if (idCaja == 0)
            {
                await DisplayAlert("Error", "No hay una caja abierta. Debe abrir una caja primero.", "OK");
                return false;
            }

            if (idUsuario == 0)
        {
            await DisplayAlert("Error", "No se encontró el usuario. Debe iniciar sesión nuevamente.", "OK");
            return false;
        }

            string itemTipoVehiculo = "";
            string itemTipoFraccion = "";

            foreach (var kvp in _mapaTipoCodigoADisplay)
            {
                if (kvp.Value == TipoVehiculo)
                {
                    itemTipoVehiculo = kvp.Key;
                    itemTipoFraccion = kvp.Key; // Asumiendo mismo código para ambos
                    break;
                }
            }

            // Preparar datos COMPLETOS para enviar
            var requestData = new
            {
                idLocal = 1, // Usar 1 como en el ejemplo
                placa = Placa.ToUpper(),
                tipoVehiculo = TipoVehiculo ?? "",
                itemTipoVehiculo = itemTipoVehiculo,
                ocupados = 0,
                estacionamientos = 0,
                codCaja = idCaja.ToString(),
                tarifaFraccion = (double)TarifaHora,
                regularizacion = false,
                fechaRegularizacion = DateTime.UtcNow,
                usuarioLogin = Preferences.Get("UsuarioNombre", "") ?? "",
                observacion = Observacion ?? "",
                itemTipoFraccion = itemTipoFraccion,
                abonadoActivo = false,
                fechaSalida = (DateTime?)null
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        // Llamar a la API
        var url = $"{_baseApi}registrarIngreso";
        var response = await _httpClient.PostAsync(url, content);

        Console.WriteLine($"Código de respuesta: {response.StatusCode}");
        Console.WriteLine($"URL llamada: {url}");
        Console.WriteLine($"Datos enviados: {jsonContent}");

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Respuesta exitosa: {jsonResponse}");
            
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<RegistrarIngresoResponse>(jsonResponse, opciones);

            if (apiResponse != null)
            {
                Ocupados = apiResponse.Ocupados;
                NumeroEstacionamiento = apiResponse.Estacionamientos;
                
                await DisplayAlert("Éxito", "Vehículo registrado en el sistema correctamente.", "OK");
                return true;
            }
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error de API: {errorContent}");
            await DisplayAlert("Error", $"No se pudo registrar el ingreso. Código: {response.StatusCode}\nDetalle: {errorContent}", "OK");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Excepción: {ex.Message}");
        await DisplayAlert("Error", $"Error al registrar ingreso: {ex.Message}", "OK");
        return false;
    }

    return false;
}

    private async void OnCambiarPlanClicked(object sender, EventArgs e)
    {
        var cambiarPage = new CambiarPlanPage(TipoVehiculo, null);
        cambiarPage.OnPlanCambiado += (nuevoTipo, nuevaTarifa) =>
        {
            TipoVehiculo = nuevoTipo;
            TarifaHora = nuevaTarifa;
            if (!string.IsNullOrWhiteSpace(Placa) && _vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
            {
                var placaKey = Placa.ToUpper();
                var veh = _vehiculosEnPlaya[placaKey];
                veh.Tipo = nuevoTipo;
                veh.TarifaHora = nuevaTarifa;
            }
            _planCambiado = true;
            RegistrarHabilitado = true; // Habilitar botón Registrar tras cambiar plan
        };
        await Navigation.PushModalAsync(cambiarPage);
    }

    private async void OnAnularClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Confirmar", "¿Desea anular la operación actual?", "Sí", "No");
        if (confirmar)
        {
            if (!string.IsNullOrEmpty(_ultimoTicketRegistrado) && NumeroTicket == _ultimoTicketRegistrado)
            {
                _contadorTicket = Math.Max(0, _contadorTicket - 1);
                NumeroTicket = null;
                _ultimoTicketRegistrado = null;
            }
            LimpiarFormulario();
        }
    }

    private async void OnPagarClicked(object sender, EventArgs e)
    {
        if (_vehiculosEnPlaya.ContainsKey(Placa?.ToUpper()))
        {
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];
            string mensaje;
            if (_planCambiado)
            {
                mensaje = "Registro exitoso";
                _planCambiado = false;
            }
            else
            {
                mensaje = "Salida de vehículo registrada correctamente";
            }
            await DisplayAlert("Pago realizado",
                $"Pago de S/. {TotalPagar:F2} realizado exitosamente.\n" +
                $"Placa: {Placa}\nTipo: {vehiculo.Tipo}\n" +
                $"Observación: {vehiculo.Observacion}\n" +
                mensaje,
                "OK");
            _vehiculosEnPlaya.Remove(Placa.ToUpper());
            Ocupados--;
            NumeroTicket = null;
            _ultimoTicketRegistrado = null;
            LimpiarFormulario();
        }
    }      

    private async void OnFacturaClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ModalPagoPage(this, "Factura", TotalPagar));
    }

    private async void OnBoletaClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ModalPagoPage(this, "Boleta", TotalPagar));
    }

    private void LimpiarFormulario()
    {
        Placa = "";
        EstadoIngreso = "";
        Observacion = "";
        TipoVehiculo = null;
        Ocupados = Math.Max(Ocupados, 0); // Asegurarse de que no sea negativo
        // No limpiar TarifaHora ni TotalPagar, ya que pueden ser necesarios para el siguiente registro
        // Limpiar también el campo de tiempo estacionado
        TiempoEstacionado = "";

        // Notificar cambio en la propiedad que habilita el botón Cambiar Plan
        OnPropertyChanged(nameof(CambiarPlanHabilitado));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VehiculoInfo
{
    public string NumeroTicket { get; set; }
    public DateTime HoraIngreso { get; set; }
    public decimal TarifaHora { get; set; }
    public string Tipo { get; set; }
    public string Observacion { get; set; }
}

// Modelos para deserializar las respuestas de la API
public class ValidaPlacaResponse
{
    public int IdOperacion { get; set; }
    public string Tiempo { get; set; }
    public decimal Precio { get; set; }
    public decimal TotalCobrar { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string NroTicket { get; set; }
    public string Observacion { get; set; }
    public string TipoVehiculo { get; set; }
    public string ItemTipoVehiculo { get; set; }
    public bool Empadronado { get; set; }
    public string NombreEmpadronado { get; set; }
    public decimal TotalCompras { get; set; }
    public string Msj { get; set; }
    public string Modo { get; set; }
    public string Parqueado { get; set; }
    public bool PuedeAnular { get; set; }
    public bool PuedeCambiar { get; set; }
    public bool MostrarVentanaVuelto { get; set; }
}

public class TipoVehiculoItem
{
    public string Value { get; set; }
    public string Display { get; set; }
    public bool Seleccionado { get; set; }
}
public class RegistrarIngresoResponse
{
    public int IdLocal { get; set; }
    public string Placa { get; set; }
    public string TipoVehiculo { get; set; }
    public string ItemTipoVehiculo { get; set; }
    public int Ocupados { get; set; }
    public int Estacionamientos { get; set; }
    public string CodCaja { get; set; }
    public decimal TarifaFraccion { get; set; }
    public bool Regularizacion { get; set; }
    public DateTime FechaRegularizacion { get; set; }
    public string UsuarioLogin { get; set; }
    public string Observacion { get; set; }
    public string ItemTipoFraccion { get; set; }
    public bool AbonadoActivo { get; set; }
}
public class ValidarTarifaResponse
{
    public decimal Precio { get; set; }
    public string Msj { get; set; }
    public bool Result { get; set; }
}