using AI_TestMaker.Classes;
using AI_TestMaker.DB;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AI_TestMaker.Views
{
    public partial class TestView : UserControl
    {
        private Test _test;
        private DispatcherTimer _timer;
        private bool _pausado = false;
        private TimeSpan _tiempoTranscurrido = TimeSpan.Zero;
        private DateTime _ultimoTick;

        private DatabaseManager _db;   

        public TestView(Test test)
        {
            InitializeComponent();
            _test = test;

            DataContext = new { Test = _test };

            _db = new DatabaseManager();

            IniciarCronometro();
            CargarPreguntas();
            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void IniciarCronometro()
        {
            _ultimoTick = DateTime.Now;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_pausado) return;

            var ahora = DateTime.Now;
            var delta = ahora - _ultimoTick;
            _ultimoTick = ahora;

            _tiempoTranscurrido += delta;

            var restante = _test.TiempoMaximo - _tiempoTranscurrido;

            if (restante <= TimeSpan.Zero)
            {
                _timer.Stop();
                _test.Fin = DateTime.Now;

                GuardarTestEnBD(); 
                FinalizarAutomaticamente();
                return;
            }

            TxtCronometro.Text = $"Tiempo restante: {restante :hh\\:mm\\:ss}";

            double porcentaje = (_tiempoTranscurrido.TotalSeconds / _test.TiempoMaximo.TotalSeconds) * 100;
            BarraTiempo.Value = porcentaje;

            if (porcentaje < 40)
                BarraTiempo.Foreground = (Brush)FindResource("TiempoVerde");
            else if (porcentaje < 70)
                BarraTiempo.Foreground = (Brush)FindResource("TiempoAmarillo");
            else
                BarraTiempo.Foreground = (Brush)FindResource("TiempoRojo");
        }

        private void BtnPausa_Click(object sender, RoutedEventArgs e)
        {
            _pausado = !_pausado;

            if (_pausado)
            {
                BtnPausa.Content = "Reanudar";
                PreguntasPanel.IsEnabled = false;
            }
            else
            {
                BtnPausa.Content = "Pausar";
                PreguntasPanel.IsEnabled = true;
                _ultimoTick = DateTime.Now;
            }
        }

        private void CargarPreguntas()
        {
            foreach (var pregunta in _test.Preguntas)
                PreguntasPanel.Children.Add(new PreguntaControl(pregunta));
        }

        private void Finalizar_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _test.Fin = DateTime.Now;

            GuardarTestEnBD();   // NUEVO

            ((MainWindow)Application.Current.MainWindow).Content =
                new ResultadosView(_test);
        }

        private void FinalizarAutomaticamente()
        {
            MessageBox.Show("El tiempo ha terminado. El test se ha finalizado automáticamente.",
                            "Tiempo agotado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            ((MainWindow)Application.Current.MainWindow).Content =
                new ResultadosView(_test);
        }

        // ---------------------------------------------------------
        // NUEVO: Guardar test en la base de datos
        // ---------------------------------------------------------
        private void GuardarTestEnBD()
        {
            try
            {
                int id = _db.GuardarTest(_test);
                Console.WriteLine($"Test guardado con ID {id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el test:\n" + ex.Message,
                                "Error BD",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}
