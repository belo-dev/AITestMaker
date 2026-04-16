using AI_TestMaker.Classes;
using System.Windows;
using System.Windows.Controls;

namespace AI_TestMaker.Views
{
    public partial class ResultadosView : UserControl
    {
        private Test _test;

        public ResultadosView(Test test)
        {
            InitializeComponent();
            _test = test;
            CargarResultados();

            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void CargarResultados()
        {
            TxtDificultad.Text = $"Dificultad: {_test.Dificultad}";
            TxtAciertos.Text = $"Aciertos: {_test.CalcularAciertos()}";
            TxtFallos.Text = $"Fallos: {_test.CalcularFallos()}";
            TxtTiempo.Text = $"Tiempo empleado: {_test.TiempoEmpleado.TotalMinutes:F2} minutos";
            TxtNota.Text = $"Nota final: {_test.CalcularNota():F2}";
        }

        private void VolverInicio_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content =
                new InicioView();
        }
    }
}