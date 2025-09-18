using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace sistemaPlaya.Pages;

public partial class RegistrarVehiculoPage : ContentPage, INotifyPropertyChanged
{
    // Diccionario para controlar vehículos en playa
    private Dictionary<string, VehiculoInfo> _vehiculosEnPlaya = new();

    // Tarifas por tipo de vehículo (fallback local)
    private Dictionary<string, decimal> _tarifasPorTipo = new Dictionary<string, decimal>
    {
        { "Automóvil", 5.00m },
        { "Camioneta", 7.00m },
        { "Combi", 10.00m },
        { "Furgón", 12.00m },
        { "Moto", 3.00m }
    };

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
            _placa = value;
            OnPropertyChanged();
            ActualizarEstadoIngreso();
            // Validar automáticamente si la placa tiene 6 caracteres
            if (!string.IsNullOrWhiteSpace(_placa) && _placa.Length == 6)
            {
                _ = VerificarPlacaAsync();
            }
        }
    }

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

    private Color _estadoIngresoColor = Colors.Black;
    public Color EstadoIngresoColor
    {
        get => _estadoIngresoColor;
        set
        {
            _estadoIngresoColor = value;
            OnPropertyChanged();
        }
    }

    // Inicialmente vacío; se cargará desde la API getComboTipoVehiculos
    public ObservableCollection<string> TiposVehiculo { get; set; } = new();

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
            EstadoIngresoColor = Colors.Black;
            MostrarPago = false;
            TipoVehiculo = null;
            RegistrarHabilitado = true;
        }
        else if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            // Si está en el diccionario local
            EstadoIngreso = "SALIDA DE VEHÍCULO";
            EstadoIngresoColor = Colors.Orange;
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
            EstadoIngresoColor = Colors.Green;
            MostrarPago = false;
            Observacion = ""; // Limpiar observación para nuevos ingresos
            TipoVehiculo = null; // Limpiar tipo de vehículo para nuevos ingresos
            RegistrarHabilitado = true;
        }
    }

    private void ActualizarTarifa()
    {
        if (!string.IsNullOrEmpty(TipoVehiculo) && _tarifasPorTipo.ContainsKey(TipoVehiculo))
        {
            TarifaHora = _tarifasPorTipo[TipoVehiculo];
        }
        else
        {
            // Si tenemos un tipo seleccionado que proviene de la API y no está en _tarifasPorTipo,
            // no cambiar la tarifa aquí: la tarifa puede venir desde la validación de placa.
            TarifaHora = TarifaHora; // no-op para evitar reset
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
            // Obtener idUsuario desde Preferences
            int idUsuario = Preferences.Get("IdUsuario", 0);
            if (idUsuario == 0)
            {
                await DisplayAlert("Error", "No se encontró el usuario. Debe iniciar sesión nuevamente.", "OK");
                return;
            }
            // Llamada a la API con idUsuario real (corregido: sin espacio en ValidaPlacaExiste)
            var placaEsc = Uri.EscapeDataString(Placa);
            var url = $"{_baseApi}ValidaPlacaExiste?placa={placaEsc}&idUsuario={idUsuario}";
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                // Si no responde bien, dejar el estado como ingreso
                return;
            }

            var json = await resp.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ValidaPlacaResponse>(json, opciones);

            if (data == null)
                return;

            // Si la API indica que está parqueado o modo es salida, mostrar datos de salida
            if ((data.Parqueado?.ToUpper() == "S") || (data.Modo?.ToLower().Contains("salida") == true))
            {
                // Mapear datos al UI
                EstadoIngreso = "SALIDA DE VEHÍCULO";
                EstadoIngresoColor = Colors.Orange;
                MostrarPago = true;
                TiempoEstacionado = data.Tiempo ?? "";
                // Usar totalCobrar si viene o calcular con precio
                TotalPagar = data.TotalCobrar != 0 ? data.TotalCobrar : data.Precio * 1; // fallback
                Observacion = data.Observacion ?? "";
                NumeroTicket = data.NroTicket;
                FechaIngreso = data.FechaIngreso;

                // Traducir código de tipo a display si lo tenemos
                if (!string.IsNullOrEmpty(data.TipoVehiculo) && _mapaTipoCodigoADisplay.TryGetValue(data.TipoVehiculo, out var display))
                {
                    TipoVehiculo = display;
                }
                else
                {
                    // Si no tenemos el mapeo, dejar el código para referencia
                    TipoVehiculo = data.TipoVehiculo;
                }

                RegistrarHabilitado = false;
            }
            else
            {
                // No está en sistema → ingreso
                EstadoIngreso = "INGRESO DE VEHÍCULO";
                EstadoIngresoColor = Colors.Green;
                MostrarPago = false;
                Observacion = "";
                TipoVehiculo = null;
                RegistrarHabilitado = true;
            }
        }
        catch (Exception ex)
        {
            // No bloquear al usuario si falla la llamada
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

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Placa) || Placa.Length != 6)
        {
            await DisplayAlert("Error", "Laplaca deve tener exactamente 6 caracters", "OK");
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
            // Ingreso de vehículo
            if (string.IsNullOrEmpty(TipoVehiculo))
            {
                await DisplayAlert("Error", "Debe seleccionar un tipo de vehículo", "OK");
                PickerTipoVehiculo.Focus();
                return;
            }

            // Generar número de ticket secuencial
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

            await DisplayAlert("Éxito",
                $"Ingreso de vehículo registrado correctamente\n" +
                $"Placa: {Placa}\nTipo: {TipoVehiculo}\n" +
                $"Hora ingreso: {DateTime.Now:HH:mm}\nTicket: {ticketSecuencial}",
                "OK");

            LimpiarFormulario();
        }
    }

    private async void OnCambiarPlanClicked(object sender, EventArgs e)
    {
        var cambiarPage = new CambiarPlanPage(TipoVehiculo, _tarifasPorTipo);
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
            // Si se anuló un registro recién hecho, restar el contador y limpiar ticket
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