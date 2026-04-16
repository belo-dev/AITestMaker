using AI_TestMaker.Classes;
using AI_TestMaker.DB;
using AI_TestMaker.DB.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AI_TestMaker.Views
{
    public partial class HistorialView : UserControl
    {
        private readonly DatabaseManager _db;
        private List<TestResumen> _tests;

        public HistorialView()
        {
            InitializeComponent();
            if (Session.IsGuest)
            {
                MessageBox.Show("El historial está disponible solo para usuarios registrados.");
                ((MainWindow)Application.Current.MainWindow).Content = new InicioView();
                return;
            }
            _db = new DatabaseManager();
            CargarHistorial();

            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void CargarHistorial()
        {
            _tests = _db.ListarTests();
            ListaTests.ItemsSource = _tests;
        }

        // BUSCADOR POR TEMA
        private void TxtBuscarTema_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TxtBuscarTema.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                ListaTests.ItemsSource = _tests;
                return;
            }

            var filtrados = _tests
                .Where(t => t.Tema != null && t.Tema.ToLower().Contains(filtro))
                .ToList();

            ListaTests.ItemsSource = filtrados;
        }

        private void ListaTests_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListaTests.SelectedItem is TestResumen resumen)
            {
                var test = _db.CargarTest(resumen.Id);

                for (int i = 0; i < test.Preguntas.Count; i++)
                {
                    test.Preguntas[i].Numero = i + 1;
                }
                ((MainWindow)Application.Current.MainWindow).Content = new TestView(test);
            }
        }

        private void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTests.SelectedItem is null)
            {
                MessageBox.Show("Selecciona un test primero.");
                return;
            }

            var item = (TestResumen)ListaTests.SelectedItem;
            var test = _db.CargarTest(item.Id);

            // Preguntar al usuario si quiere incluir soluciones
            var result = MessageBox.Show(
                "¿Quieres incluir las soluciones en el PDF?",
                "Exportar test",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            bool incluirSoluciones = (result == MessageBoxResult.Yes);

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = incluirSoluciones ? "Guardar test con soluciones" : "Guardar test sin soluciones",
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = incluirSoluciones
                    ? $"Test_{item.Id}.pdf"
                    : $"Test_{item.Id}_SinSoluciones.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                PDFExporter.ExportarTest(test, dialog.FileName, incluirSoluciones);
                MessageBox.Show("PDF exportado correctamente.");
            }
        }


        private void BorrarTest_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTests.SelectedItem is TestResumen resumen)
            {
                if (MessageBox.Show("¿Seguro que deseas borrar este test?",
                    "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _db.BorrarTest(resumen.Id);
                    CargarHistorial();
                }
            }
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new InicioView();
        }
    }
}
