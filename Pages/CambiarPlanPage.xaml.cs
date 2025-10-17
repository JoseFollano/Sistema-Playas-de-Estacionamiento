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
    private decimal _tarifaSeleccionada = 0m;

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
            var _baseApi = AppSettings.ApiUrl;
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

    private async void PickerNuevoTipo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerNuevoTipo.SelectedIndex >= 0)
        {
            var display = PickerNuevoTipo.Items[PickerNuevoTipo.SelectedIndex];
            var tipoItem = _tiposVehiculo.Find(x => x.Display == display);
            if (tipoItem != null && !string.IsNullOrEmpty(tipoItem.Value))
            {
                // Obtener tarifa desde el endpoint validarTarifa
                await ObtenerTarifaDesdeAPIAsync(tipoItem.Value);
            }
            else
            {
                TarifaLabel.Text = "S/. 0.00";
                _tarifaSeleccionada = 0m;
            }
        }
        else
        {
            TarifaLabel.Text = "S/. 0.00";
            _tarifaSeleccionada = 0m;
        }
    }

    private async Task ObtenerTarifaDesdeAPIAsync(string itemVehiculo)
    {
        try
        {
            var _baseApi = AppSettings.ApiUrl;
            var url = $"{_baseApi}validarTarifa?idEmpresa=1&itemVehiculo={Uri.EscapeDataString(itemVehiculo)}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // La API devuelve solo un número (text/plain)
                if (decimal.TryParse(json, out decimal precio))
                {
                    _tarifaSeleccionada = precio;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TarifaLabel.Text = $"S/. {_tarifaSeleccionada:F2}";
                    });
                }
                else
                {
                    // Si la respuesta no se puede parsear, dejar en 0
                    _tarifaSeleccionada = 0m;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TarifaLabel.Text = "S/. 0.00";
                    });
                }
            }
            else
            {
                _tarifaSeleccionada = 0m;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TarifaLabel.Text = "S/. 0.00";
                });
            }
        }
        catch (Exception ex)
        {
            _tarifaSeleccionada = 0m;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TarifaLabel.Text = "S/. 0.00";
            });
            await DisplayAlert("Error", $"No se pudo obtener la tarifa: {ex.Message}", "OK");
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

        // Asegurarnos de tener la tarifa actualizada antes de invocar el evento
        if (tipoItem != null && !string.IsNullOrEmpty(tipoItem.Value))
        {
            await ObtenerTarifaDesdeAPIAsync(tipoItem.Value);
        }

        OnPlanCambiado?.Invoke(display, _tarifaSeleccionada);
        await Navigation.PopModalAsync();
    }
}
