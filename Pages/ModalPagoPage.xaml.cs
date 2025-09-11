using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace sistemaPlaya.Pages
{
    public partial class ModalPagoPage : ContentPage, INotifyPropertyChanged
    {
        private readonly RegistrarVehiculoPage _mainPage;
        private readonly string _tipoDocumento;
        private readonly decimal _importeTotal;
        private string _numeroDocumento;
        private string _cliente;
        private string _direccion;
        private string _observacion;
        private bool _sinDatosCliente;

        public ModalPagoPage(RegistrarVehiculoPage mainPage, string tipoDocumento, decimal importeTotal)
        {
            InitializeComponent();
            _mainPage = mainPage;
            _tipoDocumento = tipoDocumento;
            _importeTotal = importeTotal;

            BindingContext = this;
        }

        public string TituloModal => _tipoDocumento;
        public decimal ImporteTotal => _importeTotal;
        public DateTime FechaEmision => DateTime.Now;
        public bool EsFactura => _tipoDocumento == "Factura";
        public bool NoEsFactura => !EsFactura;

        public string NumeroDocumento
        {
            get => _numeroDocumento;
            set
            {
                _numeroDocumento = value;
                OnPropertyChanged();
            }
        }

        public string Cliente
        {
            get => _cliente;
            set
            {
                _cliente = value;
                OnPropertyChanged();
            }
        }

        public string Direccion
        {
            get => _direccion;
            set
            {
                _direccion = value;
                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set
            {
                _observacion = value;
                OnPropertyChanged();
            }
        }

        public bool SinDatosCliente
        {
            get => _sinDatosCliente;
            set
            {
                _sinDatosCliente = value;
                OnPropertyChanged();

                if (value && !EsFactura)
                {
                    Cliente = "Cliente varios";
                    Direccion = "";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnBuscarClicked(object sender, EventArgs e)
        {
            // Simulaci?n: busca por RUC o DNI
            if (string.IsNullOrWhiteSpace(NumeroDocumento))
            {
                Cliente = "";
                Direccion = "";
                DisplayAlert("Error", "Ingrese un n?mero de documento v?lido.", "OK");
                return;
            }

            // Ejemplo de b?squeda simulada
            if (NumeroDocumento == "20123456789") // RUC
            {
                Cliente = "EMPRESA S.A.C.";
                Direccion = "Av. Principal 123, Lima";
            }
            else if (NumeroDocumento == "12345678") // DNI
            {
                Cliente = "Juan P?rez";
                Direccion = "Jr. Secundario 456, Arequipa";
            }
            else
            {
                Cliente = "Cliente no encontrado";
                Direccion = "";
                DisplayAlert("No encontrado", "No se encontr? informaci?n para el documento ingresado.", "OK");
            }

            // Notificar cambios si usas MVVM
            OnPropertyChanged(nameof(Cliente));
            OnPropertyChanged(nameof(Direccion));
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (EsFactura && string.IsNullOrEmpty(NumeroDocumento))
            {
                await DisplayAlert("Error", "Para factura debe ingresar un RUC", "OK");
                return;
            }

            var facturacion = new Facturacion
            {
                TipoDocumento = _tipoDocumento,
                NumeroDocumento = NumeroDocumento,
                Cliente = Cliente,
                Direccion = Direccion,
                Observacion = Observacion,
                ImporteTotal = _importeTotal
            };

            // Cerrar modal y procesar pago
            await Navigation.PopModalAsync();
            //_mainPage.ProcesarPago(facturacion);
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirmar = await DisplayAlert("Confirmar", "?Cancelar la operaci?n?", "S?", "No");
            if (confirmar)
            {
                await Navigation.PopModalAsync();
            }
        }
    }

    public class Facturacion
    {
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Cliente { get; set; }
        public string Direccion { get; set; }
        public string Observacion { get; set; }
        public decimal ImporteTotal { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
    }
}