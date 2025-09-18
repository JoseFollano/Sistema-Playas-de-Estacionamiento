using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace sistemaPlaya.Pages;

public partial class CambiarPlanPage : ContentPage
{
    private List<TipoVehiculoItem> _tiposVehiculo = new();
    public string TipoActual { get; }
    public event Action<string, decimal> OnPlanCambiado; // tipo nuevo, tarifa nueva
    private readonly HttpClient _httpClient = new();
    private readonly string _baseApi = "https://localhost:7211/";

    public CambiarPlanPage(string tipoActual, Dictionary<string, decimal> tarifas)
    {
        InitializeComponent();
        TipoActual = tipoActual;
        CurrentTipoLabel.Text = string.IsNullOrEmpty(TipoActual) ? "(ninguno)" : TipoActual;
        TarifaLabel.Text = "S/. 0.00";
        _ = LoadTiposVehiculoAsync();
        PickerNuevoTipo.SelectedIndexChanged += PickerNuevoTipo_SelectedIndexChanged;
    }

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
            _tiposVehiculo = items;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PickerNuevoTipo.ItemsSource = new List<string>(_tiposVehiculo.ConvertAll(x => x.Display));
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar tipos de vehículo: {ex.Message}", "OK");
        }
    }

    private void PickerNuevoTipo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerNuevoTipo.SelectedIndex >= 0)
        {
            var display = PickerNuevoTipo.Items[PickerNuevoTipo.SelectedIndex];
            // Aquí podrías obtener la tarifa de otra API si es necesario
            TarifaLabel.Text = ""; // No hay tarifa en este endpoint
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
        var display = PickerNuevoTipo.Items[PickerNuevoTipo.SelectedIndex];
        var tipoItem = _tiposVehiculo.Find(x => x.Display == display);
        OnPlanCambiado?.Invoke(display, 0); // Tarifa 0, puedes ajustar si tienes otra fuente
        await Navigation.PopModalAsync();
    }
}
