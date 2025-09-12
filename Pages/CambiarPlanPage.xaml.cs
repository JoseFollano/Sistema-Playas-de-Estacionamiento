using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace sistemaPlaya.Pages;

public partial class CambiarPlanPage : ContentPage
{
    private readonly Dictionary<string, decimal> _tarifasPorTipo;
    public string TipoActual { get; }

    public event Action<string, decimal> OnPlanCambiado; // tipo nuevo, tarifa nueva

    public CambiarPlanPage(string tipoActual, Dictionary<string, decimal> tarifas)
    {
        InitializeComponent();

        TipoActual = tipoActual;
        _tarifasPorTipo = tarifas ?? new Dictionary<string, decimal>();

        CurrentTipoLabel.Text = string.IsNullOrEmpty(TipoActual) ? "(ninguno)" : TipoActual;

        // Llenar picker con tipos
        PickerNuevoTipo.ItemsSource = new List<string>(_tarifasPorTipo.Keys);

        PickerNuevoTipo.SelectedIndexChanged += PickerNuevoTipo_SelectedIndexChanged;

        // Inicializar tarifa label
        TarifaLabel.Text = "S/. 0.00";
    }

    private void PickerNuevoTipo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerNuevoTipo.SelectedIndex >= 0)
        {
            var tipo = PickerNuevoTipo.Items[PickerNuevoTipo.SelectedIndex];
            if (_tarifasPorTipo.TryGetValue(tipo, out var tarifa))
            {
                TarifaLabel.Text = $"S/. {tarifa:F2}";
            }
            else
            {
                TarifaLabel.Text = "S/. 0.00";
            }
        }
        else
        {
            TarifaLabel.Text = "S/. 0.00";
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCambiarPlanClicked(object sender, EventArgs e)
    {
        if (PickerNuevoTipo.SelectedIndex < 0)
        {
            await DisplayAlert("Error", "Debe seleccionar un nuevo tipo de vehículo", "OK");
            return;
        }

        var nuevoTipo = PickerNuevoTipo.Items[PickerNuevoTipo.SelectedIndex];
        if (!_tarifasPorTipo.TryGetValue(nuevoTipo, out var tarifa)) tarifa = 0;

        OnPlanCambiado?.Invoke(nuevoTipo, tarifa);

        await Navigation.PopModalAsync();
    }
}
