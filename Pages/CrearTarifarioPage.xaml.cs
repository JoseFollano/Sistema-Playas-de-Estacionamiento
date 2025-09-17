using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace sistemaPlaya
{
    public partial class CrearTarifarioPage : ContentPage
    {
        private TarifarioItem _tarifario;
        private bool _isEditMode;

        public CrearTarifarioPage(TarifarioItem tarifario)
        {
            InitializeComponent();
            _tarifario = tarifario;
            _isEditMode = tarifario != null;

            LoadTarifarioData();
        }

        private void LoadTarifarioData()
        {
            if (_isEditMode)
            {
                TituloLabel.Text = "Editar Tarifario";
                NombreEntry.Text = _tarifario.TipoVehiculo;
                HoraEntry.Text = _tarifario.Hora.ToString(); // Usar Hora para el precio
                HoraEntry.Text = _tarifario.HoraIncremento;
                IncrementoEntry.Text = _tarifario.Incremento;
                NocheEntry.Text = _tarifario.Noche;

                // Cargar checkboxes (simulación - ajustar según tu modelo)
                CobranzaDiaCheckBox.IsChecked = !string.IsNullOrEmpty(_tarifario.Xdia) && _tarifario.Xdia != "0";
                // CobranzaNocheCheckBox.IsChecked = lógica para noche
            }
            else
            {
                TituloLabel.Text = "Nuevo Tarifario";
                // Valores por defecto
                HoraEntry.Text = "60";
                IncrementoEntry.Text = "0";
                NocheEntry.Text = "0";
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validación solo para el nombre (obligatorio)
            if (string.IsNullOrWhiteSpace(NombreEntry?.Text))
            {
                await DisplayAlert("Error", "Por favor, ingrese el nombre del vehículo.", "OK");
                return;
            }

            GuardarButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            try
            {
                // Obtener valores con manejo de valores vacíos (convertir a 0)
                string nombre = NombreEntry.Text.Trim();
                double horaPrecio = ParseDoubleOrDefault(HoraEntry?.Text);
                string horaIncremento = HoraEntry?.Text ?? "60"; // Valor por defecto
                string incremento = IncrementoEntry?.Text ?? "0";
                string noche = NocheEntry?.Text ?? "0";

                // Convertir checkboxes a valores para guardar
                string xDia = CobranzaDiaCheckBox.IsChecked ? "1" : "0";
                string cobranzaNoche = CobranzaNocheCheckBox.IsChecked ? "1" : "0";

                // Crear o actualizar tarifario
                var tarifario = new TarifarioItem
                {
                    Id = _isEditMode ? _tarifario.Id : new Random().Next(1000, 9999),
                    TipoVehiculo = nombre,
                    Hora = horaPrecio, // Usar Hora para el precio principal
                    HoraIncremento = horaIncremento,
                    Incremento = incremento,
                    Noche = noche,
                    Xdia = xDia // Guardar el estado del checkbox
                };

                // Simular guardado
                await Task.Delay(1000);

                string mensaje = _isEditMode ? "actualizado" : "creado";
                await DisplayAlert("Éxito", $"Tarifario {mensaje} correctamente.", "OK");

                // Volver a la página anterior
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al guardar tarifario: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                GuardarButton.IsEnabled = true;
            }
        }

        private double ParseDoubleOrDefault(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0.0;

            if (double.TryParse(value, out double result))
                return result;

            return 0.0;
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirmar",
                "¿Está seguro que desea cancelar? Los cambios no guardados se perderán.",
                "Sí", "No");

            if (confirm)
            {
                await Navigation.PopAsync();
            }
        }
    }
}