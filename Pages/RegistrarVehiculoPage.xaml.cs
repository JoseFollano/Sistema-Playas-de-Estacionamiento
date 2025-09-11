
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
namespace sistemaPlaya.Pages;

public partial class RegistrarVehiculoPage : ContentPage, INotifyPropertyChanged
{
    // Diccionario para controlar vehículos en playa
    private Dictionary<string, VehiculoInfo> _vehiculosEnPlaya = new();

    // Tarifas por tipo de vehículo
    private Dictionary<string, decimal> _tarifasPorTipo = new Dictionary<string, decimal>
    {
        { "Automóvil", 5.00m },
        { "Camioneta", 7.00m },
        { "Combi", 10.00m },
        { "Furgón", 12.00m },
        { "Moto", 3.00m }
    };

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

    public ObservableCollection<string> TiposVehiculo { get; set; } =
        new() { "Automóvil", "Camioneta", "Combi", "Furgón", "Moto" };

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

    public RegistrarVehiculoPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Agregar algunos vehículos de ejemplo para probar la funcionalidad
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
        }
        else if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            EstadoIngreso = "SALIDA DE VEHÍCULO";
            EstadoIngresoColor = Colors.Orange;
            MostrarPago = true;

            // Calcular tiempo y total
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];
            var tiempo = DateTime.Now - vehiculo.HoraIngreso;
            TiempoEstacionado = $"{tiempo.Hours}h {tiempo.Minutes}m";

            var horas = (decimal)Math.Ceiling(tiempo.TotalHours);
            TotalPagar = horas * vehiculo.TarifaHora;


            // Mostrar la observación existente si hay
            Observacion = vehiculo.Observacion;
            // Aqui se actualiza el tipo de vehiculo registrado originalmente
            TipoVehiculo = vehiculo.Tipo;
        }
        else
        {
            EstadoIngreso = "INGRESO DE VEHÍCULO";
            EstadoIngresoColor = Colors.Green;
            MostrarPago = false;
            Observacion = ""; // Limpiar observación para nuevos ingresos
            TipoVehiculo = null; // Limpiar tipo de vehículo para nuevos ingresos
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
            TarifaHora = 0;
        }
    }

    private void OnPlacaCompleted(object sender, EventArgs e)
    {
        // Enfocar el picker de tipo de vehículo después de ingresar la placa
        PickerTipoVehiculo.Focus();
    }

    // ✅ Registrar ingreso o salida
    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Placa) || Placa.Length != 6)
        {
            await DisplayAlert("Error", "Laplaca deve tener exactamente 6 caracters", "OK");
            EntryPlaca.Focus();
            return;
        }

        if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            // Salida de vehículo
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

            _vehiculosEnPlaya[Placa.ToUpper()] = new VehiculoInfo
            {
                NumeroTicket = Guid.NewGuid().ToString().Substring(0, 8), // Ejemplo simple
                HoraIngreso = DateTime.Now,
                TarifaHora = TarifaHora,
                Tipo = TipoVehiculo,
                Observacion = Observacion
            };

            Ocupados++;

            await DisplayAlert("Éxito",
                $"Ingreso de vehículo registrado correctamente\n" +
                $"Placa: {Placa}\nTipo: {TipoVehiculo}\n" +
                $"Hora ingreso: {DateTime.Now:HH:mm}",
                "OK");

            LimpiarFormulario();
        }
    }

    // ✅ Cambiar plan → abre el picker de tipos de vehículo
    private void OnCambiarPlanClicked(object sender, EventArgs e)
    {
        PickerTipoVehiculo.Focus();

    }

    // ✅ Anular operación actual
    private async void OnAnularClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Confirmar", "¿Desea anular la operación actual?", "Sí", "No");
        if (confirmar)
        {
            LimpiarFormulario();
        }
    }

    // ✅ Pagar y finalizar
    private async void OnPagarClicked(object sender, EventArgs e)
    {
        if (_vehiculosEnPlaya.ContainsKey(Placa.ToUpper()))
        {
            var vehiculo = _vehiculosEnPlaya[Placa.ToUpper()];

            await DisplayAlert("Pago realizado",
                $"Pago de S/. {TotalPagar:F2} realizado exitosamente.\n" +
                $"Placa: {Placa}\nTipo: {vehiculo.Tipo}\n" +
                $"Observación: {vehiculo.Observacion}\n" +
                $"Vehículo retirado.",
                "OK");

            _vehiculosEnPlaya.Remove(Placa.ToUpper());
            Ocupados--;
            LimpiarFormulario();
        }
    }

    private void LimpiarFormulario()
    {
        Placa = string.Empty;
        TipoVehiculo = null;
        Observacion = string.Empty;
        MostrarPago = false;
    }

    private async void OnFacturaClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ModalPagoPage(this, NumeroTicket, TotalPagar));
    }

    private async void OnBoletaClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ModalPagoPage(this, NumeroTicket, TotalPagar));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Clase para almacenar información del vehículo
public class VehiculoInfo
{
    public string NumeroTicket { get; set; } // ← Añade esta línea
    public DateTime HoraIngreso { get; set; }
    public decimal TarifaHora { get; set; }
    public string Tipo { get; set; }
    public string Observacion { get; set; }
}