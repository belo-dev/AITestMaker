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
            _db = new DatabaseManager();
            CargarHistorial();
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

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar test como PDF",
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Test_{item.Id}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                PDFExporter.ExportarTest(test, dialog.FileName);
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
