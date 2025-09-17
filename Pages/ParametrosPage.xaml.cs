using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace sistemaPlaya
{
    public partial class ParametrosPage : ContentPage
    {
        public ParametrosPage()
        {
            InitializeComponent();
            LoadParametros();
        }

        private void LoadParametros()
        {
            // Cargar valores por defecto o desde preferencias
            AnchoTicketsEntry.Text = "80";
            // Cargar otros valores cuando tenga la API
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Aqu� ir� la l�gica de guardado cuando tenga la API
            await DisplayAlert("�xito", "Configuraci�n guardada correctamente.", "OK");
        }
    }
}