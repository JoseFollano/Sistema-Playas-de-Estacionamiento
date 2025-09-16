using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.IO;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.Linq;

namespace sistemaPlaya
{
    public class DetalleCajaItem
    {
        public int IdCaja { get; set; }
        public string Usuario { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public string FPago { get; set; }
        public double Ingreso { get; set; }
        public double Egreso { get; set; }
        public double Total { get; set; }
    }

    public partial class CierreCajaPage : ContentPage
    {
        // private const string BaseApiUrl = "https://localhost:7282/"; // <--- AJUSTA ESTA URL CUANDO TENGAS LAS APIS

        private int _idUsuario;
        private string _nombreUsuario;
        private int _cajaAbiertaId;
        private string _cajaAbiertaCodigo;
        private double _importeInicio;

        public CierreCajaPage()
        {
            InitializeComponent();

            _idUsuario = Preferences.Get("IdUsuario", 0);
            _nombreUsuario = Preferences.Get("UsuarioNombre", string.Empty);
            _cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);
            _cajaAbiertaCodigo = Preferences.Get("CajaAbiertaId", 0).ToString(); // Mostrar el ID de la caja
            _importeInicio = Preferences.Get("CajaImporteInicio", 0.0);

            if (_cajaAbiertaId == 0 || _idUsuario == 0 || string.IsNullOrEmpty(_nombreUsuario))
            {
                DisplayAlert("Error", "No hay una caja abierta o información de usuario para cerrar. Por favor, abra una caja primero.", "OK");
                Navigation.PopAsync();
                return;
            }

            CajaAbiertaLabel.Text = $"Caja: {_cajaAbiertaId} - Usuario: {_nombreUsuario}";
            FechaCierreDatePicker.Date = DateTime.Now;
        }

        private async void OnCerrarCajaClicked(object sender, EventArgs e)
        {
            // Verificar que haya caja abierta
            int cajaAbiertaId = Preferences.Get("CajaAbiertaId", 0);
            if (cajaAbiertaId == 0)
            {
                await DisplayAlert("Error", "No hay una caja abierta para cerrar.", "OK");
                return;
            }

            if (!double.TryParse(ImporteRecaudadoEntry.Text, out double totalCobrado) || totalCobrado < 0)
            {
                await DisplayAlert("Error", "Por favor, ingrese un importe recaudado válido (mayor o igual a cero).", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirmar Cierre de Caja",
                                               $"¿Está seguro que desea cerrar la caja '{_cajaAbiertaCodigo}' con un importe recaudado de {totalCobrado:C}?",
                                               "Sí", "No");
            if (!confirm)
            {
                return;
            }

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            CerrarCajaButton.IsEnabled = false;

            try
            {
                int idEmpresa = 1; // Cambia esto si tu lógica lo requiere
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://localhost:7211/cerrarCaja?idEmpresa={idEmpresa}&idCaja={_cajaAbiertaId}&idUsuario={_idUsuario}&nombreUsuario={Uri.EscapeDataString(_nombreUsuario)}&totalCobrado={totalCobrado}";
                    var response = await client.PostAsync(url, null);

                    if (response.IsSuccessStatusCode)
                    {
                        // Generar datos simulados de movimientos
                        var movimientos = GenerarMovimientosSimulados();

                        // Recuperar el importe inicial
                        double importeInicio = ObtenerImporteInicialDeMovimientos(movimientos);

                        // Calcular totales
                        var totales = CalcularTotales(movimientos);
                        double totalCobradoSistema = totales.TotalCobrado;
                        double totalAnulado = totales.TotalAnulado;

                        await DisplayAlert("Éxito", $"Caja '{_cajaAbiertaCodigo}' cerrada con éxito.", "OK");

                        // Generar boleta de cierre con datos simulados
                        string pdfPath = await GenerarBoletaCierre(
                            movimientos,
                            importeInicio,
                            totalCobradoSistema,
                            totalAnulado,
                            totalCobrado);

                        if (!string.IsNullOrEmpty(pdfPath))
                        {
                            bool shareReceipt = await DisplayAlert("Boleta de Cierre Generada",
                                "¿Desea compartir la boleta de cierre?", "Sí", "No");

                            if (shareReceipt)
                            {
                                await ShareReceipt(pdfPath);
                            }
                        }

                        // Limpia los datos de la caja abierta de Preferences
                        Preferences.Remove("CajaAbiertaId");
                        Preferences.Remove("CajaAbiertaCodigo");
                        Preferences.Remove("CajaAbiertaEstado");
                        Preferences.Remove("CajaImporteInicio");
                        Preferences.Remove("CajaFechaApertura");

                        await Navigation.PopAsync();
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error de API (Cerrar Caja)", $"No se pudo cerrar la caja. Código: {response.StatusCode}. Detalle: {errorContent}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error al cerrar la caja: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                CerrarCajaButton.IsEnabled = true;
            }
        }

        // Método para generar movimientos simulados
        private List<DetalleCajaItem> GenerarMovimientosSimulados()
        {
            var movimientos = new List<DetalleCajaItem>();

            // Movimiento de apertura
            movimientos.Add(new DetalleCajaItem
            {
                IdCaja = _cajaAbiertaId,
                Usuario = _nombreUsuario,
                Descripcion = "APERTURA DE CAJA",
                Fecha = DateTime.Now.AddHours(-4),
                Ingreso = _importeInicio,
                Egreso = 0,
                Total = _importeInicio
            });

            // Movimientos de cobro simulados
            Random random = new Random();
            for (int i = 1; i <= 5; i++)
            {
                movimientos.Add(new DetalleCajaItem
                {
                    IdCaja = _cajaAbiertaId,
                    Usuario = _nombreUsuario,
                    Descripcion = $"Cobro Estacionamiento V-{random.Next(1000, 9999)}",
                    Fecha = DateTime.Now.AddHours(-4 + i),
                    Ingreso = random.Next(5, 20),
                    Egreso = 0,
                    Total = _importeInicio + movimientos.Where(m => m.Ingreso > 0).Sum(m => m.Ingreso)
                });
            }

            // Movimiento de anulación simulado
            movimientos.Add(new DetalleCajaItem
            {
                IdCaja = _cajaAbiertaId,
                Usuario = _nombreUsuario,
                Descripcion = "ANULACION - Cobro Estacionamiento V-1234",
                Fecha = DateTime.Now.AddHours(-1),
                Ingreso = 0,
                Egreso = 12.00,
                Total = movimientos.Last().Total - 12.00
            });

            return movimientos;
        }

        private double ObtenerImporteInicialDeMovimientos(List<DetalleCajaItem> movimientos)
        {
            var movimientoApertura = movimientos.FirstOrDefault(m => m.Descripcion.Contains("APERTURA DE CAJA"));

            if (movimientoApertura != null)
            {
                return movimientoApertura.Ingreso;
            }

            return Preferences.Get("CajaImporteInicio", 0.0);
        }

        private async Task<List<DetalleCajaItem>> ObtenerDetalleMovimientos()
        {
            var movimientos = new List<DetalleCajaItem>();
            try
            {
                /* DESCOMENTAR CUANDO TENGAS LAS APIS
        using (HttpClient client = new HttpClient())
        {
            string requestUrl = $"{BaseApiUrl}detalleCobranza?idCaja={_cajaAbiertaId}";

            HttpResponseMessage response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                movimientos = JsonSerializer.Deserialize<List<DetalleCajaItem>>(jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        */
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo obtener el detalle de movimientos: {ex.Message}", "OK");
            }
            return movimientos ?? new List<DetalleCajaItem>();
        }

        private (double TotalCobrado, double TotalAnulado) CalcularTotales(List<DetalleCajaItem> movimientos)
        {
            double totalCobrado = 0;
            double totalAnulado = 0;

            foreach (var movimiento in movimientos)
            {
                if (movimiento.Ingreso > 0 && !movimiento.Descripcion.Contains("APERTURA"))
                {
                    totalCobrado += movimiento.Ingreso;
                }

                if (movimiento.Egreso > 0)
                {
                    totalAnulado += movimiento.Egreso;
                }
            }

            return (totalCobrado, totalAnulado);
        }

        private async Task<string> GenerarBoletaCierre(
            List<DetalleCajaItem> movimientos,
            double importeInicio,
            double totalCobradoSistema,
            double totalAnulado,
            double totalCobrado)
        {
            try
            {
                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);

                XFont titleFont = new XFont("Arial", 18, XFontStyle.Bold);
                XFont subtitleFont = new XFont("Arial", 14, XFontStyle.Bold);
                XFont headerFont = new XFont("Arial", 11, XFontStyle.Bold);
                XFont normalFont = new XFont("Arial", 10, XFontStyle.Regular);
                XFont smallFont = new XFont("Arial", 9, XFontStyle.Regular);

                double yPosition = 30;
                double leftMargin = 40;
                double rightMargin = 40;
                double pageWidth = page.Width;
                double lineHeight = 14;

                // Encabezado principal
                gfx.DrawString("BOLETA DE CIERRE DE CAJA", titleFont, XBrushes.Black,
                    new XRect(leftMargin, yPosition, pageWidth - leftMargin - rightMargin, 25),
                    XStringFormats.TopCenter);
                yPosition += 30;

                gfx.DrawString($"Caja #: {_cajaAbiertaCodigo}", normalFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;

                gfx.DrawString($"Cajero: {_nombreUsuario}", normalFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;

                gfx.DrawString($"Fecha de Cierre: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", normalFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight * 1.5;

                // Línea separadora gruesa
                gfx.DrawLine(new XPen(XColors.Black, 2), leftMargin, yPosition, pageWidth - rightMargin, yPosition);
                yPosition += 15;

                // RESUMEN FINANCIERO
                gfx.DrawString("RESUMEN FINANCIERO", subtitleFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight + 5;

                // Crear un cuadro para el resumen
                double boxWidth = pageWidth - leftMargin - rightMargin;
                double boxHeight = lineHeight * 8;
                double boxStartY = yPosition;

                // Dibujar solo el rectángulo exterior primero
                gfx.DrawRectangle(XPens.Black, leftMargin, yPosition, boxWidth, boxHeight);

                // Contenido del resumen financiero
                double contentY = boxStartY + lineHeight;
                double middleX = leftMargin + (boxWidth * 0.7);

                // Calcular total teórico
                double totalTeorico = importeInicio + totalCobradoSistema - totalAnulado;

                // Fila 1 - Importe Inicial
                gfx.DrawString("Importe Inicial:", normalFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(importeInicio.ToString("C"), normalFont, XBrushes.Black, middleX + 10, contentY + 2);
                contentY += lineHeight;

                // Fila 2 - Total Cobrado (Sistema)
                gfx.DrawString("Total Cobrado (Sistema):", normalFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(totalCobradoSistema.ToString("C"), normalFont, XBrushes.Black, middleX + 10, contentY + 2);
                contentY += lineHeight;

                // Fila 3 - Total Anulado
                gfx.DrawString("Total Anulado:", normalFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(totalAnulado.ToString("C"), normalFont, XBrushes.Black, middleX + 10, contentY + 2);
                contentY += lineHeight;

                // Línea separadora doble
                gfx.DrawLine(new XPen(XColors.Black, 1), leftMargin + 5, contentY, middleX - 5, contentY);
                gfx.DrawLine(new XPen(XColors.Black, 1), middleX + 5, contentY, pageWidth - rightMargin - 5, contentY);
                contentY += lineHeight;

                // Fila 4 - Total Teórico
                gfx.DrawString("TOTAL TEÓRICO:", headerFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(totalTeorico.ToString("C"), headerFont, XBrushes.Black, middleX + 10, contentY + 2);
                contentY += lineHeight;

                // Fila 5 - Total Declarado
                gfx.DrawString("TOTAL DECLARADO:", headerFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(totalCobrado.ToString("C"), headerFont, XBrushes.Black, middleX + 10, contentY + 2);
                contentY += lineHeight;

                // Fila 6 - Diferencia
                double diferencia = totalCobrado - totalTeorico;
                string diferenciaTexto = diferencia >= 0 ? $"SOBRANTE: {diferencia:C}" : $"FALTANTE: {Math.Abs(diferencia):C}";
                XBrush diferenciaColor = diferencia >= 0 ? XBrushes.Green : XBrushes.Red;

                gfx.DrawString("DIFERENCIA:", subtitleFont, XBrushes.Black, leftMargin + 10, contentY + 2);
                gfx.DrawString(diferenciaTexto, subtitleFont, diferenciaColor, middleX + 10, contentY + 2);

                // Dibujar líneas internas al final
                gfx.DrawLine(XPens.Black, leftMargin, boxStartY + lineHeight, pageWidth - rightMargin, boxStartY + lineHeight);

                for (int i = 1; i <= 6; i++)
                {
                    gfx.DrawLine(XPens.Black, leftMargin, boxStartY + (lineHeight * i), pageWidth - rightMargin, boxStartY + (lineHeight * i));
                }

                for (int i = 1; i <= 5; i++)
                {
                    double lineY = boxStartY + (lineHeight * i);
                    gfx.DrawLine(XPens.Black, middleX, lineY, middleX, lineY + lineHeight);
                }

                yPosition = boxStartY + boxHeight + 15;

                // Línea separadora gruesa
                gfx.DrawLine(new XPen(XColors.Black, 2), leftMargin, yPosition, pageWidth - rightMargin, yPosition);
                yPosition += 20;

                // DETALLE DE MOVIMIENTOS
                gfx.DrawString("DETALLE DE MOVIMIENTOS", subtitleFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight + 10;

                gfx.DrawString("Fecha/Hora", headerFont, XBrushes.Black, leftMargin, yPosition);
                gfx.DrawString("Descripción Completa", headerFont, XBrushes.Black, leftMargin + 90, yPosition);
                gfx.DrawString("Ingreso", headerFont, XBrushes.Black, pageWidth - rightMargin - 100, yPosition);
                gfx.DrawString("Egreso", headerFont, XBrushes.Black, pageWidth - rightMargin - 50, yPosition);
                yPosition += lineHeight + 2;

                gfx.DrawLine(XPens.Black, leftMargin, yPosition, pageWidth - rightMargin, yPosition);
                yPosition += 5;

                foreach (var movimiento in movimientos)
                {
                    if (yPosition > page.Height - 100)
                    {
                        gfx.Dispose();
                        page = document.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                        yPosition = 40;

                        gfx.DrawString("DETALLE DE MOVIMIENTOS (continuación)", subtitleFont, XBrushes.Black, leftMargin, yPosition);
                        yPosition += lineHeight + 10;

                        gfx.DrawString("Fecha/Hora", headerFont, XBrushes.Black, leftMargin, yPosition);
                        gfx.DrawString("Descripción Completa", headerFont, XBrushes.Black, leftMargin + 90, yPosition);
                        gfx.DrawString("Ingreso", headerFont, XBrushes.Black, pageWidth - rightMargin - 100, yPosition);
                        gfx.DrawString("Egreso", headerFont, XBrushes.Black, pageWidth - rightMargin - 50, yPosition);
                        yPosition += lineHeight + 2;

                        gfx.DrawLine(XPens.Black, leftMargin, yPosition, pageWidth - rightMargin, yPosition);
                        yPosition += 5;
                    }

                    gfx.DrawString(movimiento.Fecha.ToString("dd/MM/yyyy HH:mm"), smallFont, XBrushes.Black, leftMargin, yPosition);

                    string descripcion = movimiento.Descripcion ?? "";
                    if (descripcion.Length > 40)
                    {
                        string linea1 = descripcion.Length > 40 ? descripcion.Substring(0, 40) : descripcion;
                        string linea2 = descripcion.Length > 40 ? descripcion.Substring(40, Math.Min(40, descripcion.Length - 40)) : "";

                        gfx.DrawString(linea1, smallFont, XBrushes.Black, leftMargin + 90, yPosition);
                        if (!string.IsNullOrEmpty(linea2))
                        {
                            yPosition += lineHeight - 3;
                            gfx.DrawString(linea2, smallFont, XBrushes.Black, leftMargin + 90, yPosition);
                        }
                    }
                    else
                    {
                        gfx.DrawString(descripcion, smallFont, XBrushes.Black, leftMargin + 90, yPosition);
                    }

                    gfx.DrawString(movimiento.Ingreso > 0 ? movimiento.Ingreso.ToString("C") : "", smallFont, XBrushes.Black, pageWidth - rightMargin - 100, yPosition);
                    gfx.DrawString(movimiento.Egreso > 0 ? movimiento.Egreso.ToString("C") : "", smallFont, XBrushes.Black, pageWidth - rightMargin - 50, yPosition);

                    yPosition += lineHeight + 2;
                }

                yPosition += 20;

                gfx.DrawString($"Total de movimientos registrados: {movimientos.Count}", normalFont, XBrushes.Black, leftMargin, yPosition);
                yPosition += lineHeight;

                gfx.DrawString($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", smallFont, XBrushes.Black, leftMargin, yPosition);

                string fileName = $"CierreCaja_{_cajaAbiertaCodigo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string folderPath = FileSystem.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                document.Save(filePath);
                document.Close();

                return filePath;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo generar la boleta de cierre: {ex.Message}", "OK");
                return null;
            }
        }

        private async Task ShareReceipt(string filePath)
        {
            try
            {
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir Boleta de Cierre",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo compartir la boleta: {ex.Message}", "OK");
            }
        }
    }
}