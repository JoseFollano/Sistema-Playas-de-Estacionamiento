using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace sistemaPlaya.Pages;

public partial class VerVehiculosPage : ContentPage
{
    public ObservableCollection<VehiculoSalidaInfo> Vehiculos { get; set; } = new();

    public VerVehiculosPage()
    {
        InitializeComponent();
        BindingContext = this;
        FechaInicioPicker.Date = DateTime.Today.AddDays(-7);
        FechaFinPicker.Date = DateTime.Today;
        CargarDatosSimulados();
    }

    private void CargarDatosSimulados()
    {
        Vehiculos.Clear();
        Vehiculos.Add(new VehiculoSalidaInfo {
            Codigo = "001",
            Placa = "ABC123",
            Vehiculo = "Automóvil",
            FechaHoraEntrada = DateTime.Today.AddHours(8),
            FechaHoraSalida = DateTime.Today.AddHours(10).AddMinutes(30),
            Total = 15.00m
        });
        Vehiculos.Add(new VehiculoSalidaInfo {
            Codigo = "002",
            Placa = "XYZ789",
            Vehiculo = "Moto",
            FechaHoraEntrada = DateTime.Today.AddHours(9),
            FechaHoraSalida = DateTime.Today.AddHours(12).AddMinutes(15),
            Total = 5.00m
        });
        // Puedes agregar más datos simulados aquí
    }

    private void OnBuscarClicked(object sender, EventArgs e)
    {
        var inicio = FechaInicioPicker.Date;
        var fin = FechaFinPicker.Date.AddDays(1).AddTicks(-1);

        var filtrados = new ObservableCollection<VehiculoSalidaInfo>();
        foreach (var v in Vehiculos)
        {
            if (v.FechaHoraSalida >= inicio && v.FechaHoraSalida <= fin)
                filtrados.Add(v);
        }

        VehiculosCollectionView.ItemsSource = filtrados;
    }

    private async void OnExportarClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Exportar", "Exportación de datos en desarrollo.", "OK");
    }
}
// este es un commentario de prueba
public class VehiculoSalidaInfo
{
    public string Codigo { get; set; }
    public string Placa { get; set; }
    public string Vehiculo { get; set; }
    public DateTime FechaHoraEntrada { get; set; }
    public DateTime FechaHoraSalida { get; set; }
    public decimal Total { get; set; }
}