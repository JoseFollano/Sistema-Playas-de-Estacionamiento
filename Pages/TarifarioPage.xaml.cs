using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace sistemaPlaya
{
    // Modelo para el tarifario
    public class TarifarioItem
    {
        public int Id { get; set; }
        public string TipoVehiculo { get; set; }
        public string HoraIncremento { get; set; }
        public string Xdia { get; set; }
        public double Hora { get; set; }
        public string Incremento { get; set; }
        public string Noche { get; set; }
    }

    public partial class TarifarioPage : ContentPage
    {
        public ObservableCollection<TarifarioItem> Tarifarios { get; set; }
        private TarifarioItem _selectedTarifario;

        public TarifarioPage()
        {
            InitializeComponent();
            Tarifarios = new ObservableCollection<TarifarioItem>();
            BindingContext = this;

            LoadTarifarios();
        }

        private void LoadTarifarios()
        {
            // Datos simulados - reemplazar con datos reales cuando tengas la API
            Tarifarios.Clear();

            // Tarifarios simulados
            Tarifarios.Add(new TarifarioItem
            {
                Id = 1,
                TipoVehiculo = "Automóvil",
                HoraIncremento = "60", // minutos
                Xdia = "0",
                Hora = 5.00,
                Incremento = "",
                Noche = ""
            });

            Tarifarios.Add(new TarifarioItem
            {
                Id = 2,
                TipoVehiculo = "Motocicleta",
                HoraIncremento = "60",
                Xdia = "0",
                Hora = 3.00,
                Incremento = "",
                Noche = ""
            });

            Tarifarios.Add(new TarifarioItem
            {
                Id = 3,
                TipoVehiculo = "Camioneta",
                HoraIncremento = "60",
                Xdia = "0",
                Hora = 7.00,
                Incremento = "",
                Noche = ""
            });

            Tarifarios.Add(new TarifarioItem
            {
                Id = 4,
                TipoVehiculo = "Camión",
                HoraIncremento = "60",
                Xdia = "0",
                Hora = 10.00,
                Incremento = "",
                Noche = ""
            });

            TarifariosCollectionView.ItemsSource = Tarifarios;
        }

        private void OnTarifarioSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedTarifario = e.CurrentSelection.FirstOrDefault() as TarifarioItem;
            EliminarButton.IsEnabled = _selectedTarifario != null;
        }

        private async void OnNuevoClicked(object sender, EventArgs e)
        {
            // Navegar a la página de edición/creación
            await Navigation.PushAsync(new CrearTarifarioPage(null));
        }

        private async void OnEditarTarifarioClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var tarifario = button?.CommandParameter as TarifarioItem;

            if (tarifario != null)
            {
                await Navigation.PushAsync(new CrearTarifarioPage(tarifario));
            }
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            if (_selectedTarifario == null)
            {
                await DisplayAlert("Error", "Por favor, seleccione un tarifario para eliminar.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirmar Eliminación",
                $"¿Está seguro que desea eliminar el tarifario para {_selectedTarifario.TipoVehiculo}?",
                "Sí", "No");

            if (confirm)
            {
                // Eliminar tarifario (simulado)
                Tarifarios.Remove(_selectedTarifario);
                _selectedTarifario = null;
                EliminarButton.IsEnabled = false;

                await DisplayAlert("Éxito", "Tarifario eliminado correctamente.", "OK");
            }
        }
    }
}