using AI_TestMaker.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using AI_TestMaker.Classes;
using System.Windows.Input;

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

        private List<FrameworkElement> _preguntaContainers = new List<FrameworkElement>();
        private bool _navCollapsed = false;

        // tamaños constantes para los botones de navegación
        private const double NavButtonWidth = 44.0;
        private const double NavButtonHeight = 36.0;

        public TestView(Test test)
        {
            InitializeComponent();
            _test = test ?? throw new ArgumentNullException(nameof(test));
            DataContext = new { Test = _test };

            _db = new DatabaseManager();

            LayoutRoot.SizeChanged += (s, e) => UpdateNavMaxHeight();
            HeaderCard.SizeChanged += (s, e) => UpdateNavMaxHeight();
            FooterPanel.SizeChanged += (s, e) => UpdateNavMaxHeight();

            this.Loaded += (s, e) =>
            {
                UpdateNavMaxHeight();
            };

            CargarPreguntas();
            ConstruirNav();
            IniciarCronometro();
            ActualizarUIInicial();

            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void ActualizarUIInicial()
        {
            TxtCronometro.Text = "00:00:00";
            TxtSub.Text = "Tiempo restante";
            UpdateProgressVisual(0);
            UpdateProgresoText();
            TxtProgresoSide.Text = TxtProgreso.Text;
            PauseIcon.Visibility = Visibility.Visible;
            PlayIcon.Visibility = Visibility.Collapsed;

            UpdateNavMaxHeight();
        }

        #region Cronómetro y progreso circular

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

            TxtCronometro.Text = $"{restante:hh\\:mm\\:ss}";

            double porcentaje = (_tiempoTranscurrido.TotalSeconds / _test.TiempoMaximo.TotalSeconds) * 100;
            UpdateProgressVisual(porcentaje);
            UpdateProgresoText();
        }

        private void UpdateProgresoText()
        {
            int total = _test.Preguntas?.Count ?? 0;
            int respondidas = _test.Preguntas?.Count(p => p.RespuestaUsuario.HasValue) ?? 0;
            TxtProgreso.Text = $"Respondidas: {respondidas}/{total}";
            TxtProgresoSide.Text = TxtProgreso.Text;
        }

        private void UpdateProgressVisual(double porcentaje)
        {
            double angle = porcentaje / 100.0 * 360.0;

            ArcPath.Stroke = porcentaje < 40 ? (Brush)FindResource("TiempoVerde")
                              : porcentaje < 70 ? (Brush)FindResource("TiempoAmarillo")
                              : (Brush)FindResource("TiempoRojo");

            ArcPath.Data = CreateArcGeometry(90, 90, 72, -90, -90 + angle);

            DoubleAnimation anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            ArcPath.BeginAnimation(OpacityProperty, anim);
        }

        private Geometry CreateArcGeometry(double centerX, double centerY, double radius, double startAngle, double endAngle)
        {
            double startRad = startAngle * Math.PI / 180.0;
            double endRad = endAngle * Math.PI / 180.0;

            Point startPoint = new Point(centerX + radius * Math.Cos(startRad), centerY + radius * Math.Sin(startRad));
            Point endPoint = new Point(centerX + radius * Math.Cos(endRad), centerY + radius * Math.Sin(endRad));

            bool isLargeArc = Math.Abs(endAngle - startAngle) > 180;

            var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            var arc = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };
            figure.Segments.Add(arc);

            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            return geo;
        }

        #endregion

        #region Pausa (icono)

        private void BtnPausa_Click(object sender, RoutedEventArgs e) => TogglePausa();
        private void BtnReanudar_Click(object sender, RoutedEventArgs e) => TogglePausa();

        private void TogglePausa()
        {
            _pausado = !_pausado;

            // Animación del radio de desenfoque sobre BackgroundBlur
            double from = _pausado ? 0 : 6;
            double to = _pausado ? 6 : 0;

            var blurAnim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Iniciar animación sobre la propiedad Radius del BlurEffect del LayoutRoot
            var blur = BackgroundBlur; // nombre definido en XAML
            if (blur != null)
            {
                blur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, blurAnim);
            }

            if (_pausado)
            {
                PauseIcon.Visibility = Visibility.Collapsed;
                PlayIcon.Visibility = Visibility.Visible;

                PreguntasPanel.IsEnabled = false;
                PauseOverlay.Visibility = Visibility.Visible;

                // opcional: mover foco fuera para evitar interacción accidental
                Keyboard.ClearFocus();
            }
            else
            {
                PauseIcon.Visibility = Visibility.Visible;
                PlayIcon.Visibility = Visibility.Collapsed;

                PreguntasPanel.IsEnabled = true;
                PauseOverlay.Visibility = Visibility.Collapsed;
                _ultimoTick = DateTime.Now;

                // opcional: restaurar foco a preguntas
                PreguntasPanel.Focus();
            }
        }


        #endregion

        #region Navegación colapsable y construcción

        private void BtnToggleNav_Click(object sender, RoutedEventArgs e)
        {
            // Usar ContentGrid si existe (es donde definiste las columnas), si no, LayoutRoot como fallback
            var grid = this.FindName("ContentGrid") as Grid ?? LayoutRoot;

            // Seguridad: comprobar que hay al menos 2 ColumnDefinitions
            if (grid == null || grid.ColumnDefinitions == null || grid.ColumnDefinitions.Count <= 1)
            {
                // No hay columna de navegación disponible; salir sin hacer nada
                return;
            }

            var colDef = grid.ColumnDefinitions[1];

            // Cancelar animaciones previas sobre la columna
            colDef.BeginAnimation(ColumnDefinition.WidthProperty, null);

            // Alternar estado y fijar ancho final inmediatamente
            if (_navCollapsed)
            {
                colDef.Width = new GridLength(260, GridUnitType.Pixel);
                _navCollapsed = false;
                BtnToggleNav.ToolTip = "Ocultar navegación";
            }
            else
            {
                colDef.Width = new GridLength(0, GridUnitType.Pixel);
                _navCollapsed = true;
                BtnToggleNav.ToolTip = "Mostrar navegación";
            }

            // Liberar captura del ratón si la tuviera (evita estados pressed/hover persistentes)
            if (Mouse.Captured == BtnToggleNav) Mouse.Capture(null);

            // Quitar foco del botón para eliminar estados visuales de foco/hover
            var moved = BtnToggleNav.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            if (!moved)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(PreguntasPanel), PreguntasPanel);
            }

            // Forzar refresco visual y recalcular layout dependiente
            BtnToggleNav.InvalidateVisual();
            UpdateNavMaxHeight();
        }

        private void ConstruirNav()
        {
            NavItems.Items.Clear();

            if (_test.Preguntas == null) return;

            for (int i = 0; i < _test.Preguntas.Count; i++)
            {
                int index = i;
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Width = NavButtonWidth,
                    Height = NavButtonHeight,
                    Margin = new Thickness(4),
                    Tag = index,
                    Background = new SolidColorBrush(Color.FromRgb(20, 30, 45)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btn.Click += (s, e) => ScrollToQuestion(index);
                NavItems.Items.Add(btn);
            }

            NavItems.InvalidateMeasure();
            NavItems.InvalidateArrange();
        }

        #endregion

        #region Preguntas y scroll

        private void CargarPreguntas()
        {
            PreguntasPanel.Children.Clear();
            _preguntaContainers.Clear();

            if (_test.Preguntas == null) return;

            for (int i = 0; i < _test.Preguntas.Count; i++)
            {
                var pregunta = _test.Preguntas[i];

                try { pregunta.Numero = i + 1; } catch { }

                var control = new PreguntaControl(pregunta)
                {
                    Margin = new Thickness(6)
                };

                PreguntasPanel.Children.Add(control);
                _preguntaContainers.Add(control);

                if (pregunta is System.ComponentModel.INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(Pregunta.RespuestaUsuario))
                        {
                            Dispatcher.BeginInvoke(new Action(UpdateProgresoText));
                        }
                    };
                }
            }
        }

        private void ScrollToQuestion(int index)
        {
            if (_preguntaContainers.Count > index)
            {
                var target = _preguntaContainers[index];
                target.BringIntoView(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
                var anim = new DoubleAnimation(1.0, 1.02, TimeSpan.FromMilliseconds(120)) { AutoReverse = true };
                target.BeginAnimation(OpacityProperty, anim);
            }
        }

        #endregion

        #region Finalizar y BD

        private void Finalizar_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _test.Fin = DateTime.Now;
            GuardarTestEnBD();

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

        #endregion

        #region Zoom y layout helpers

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;

            // no cambiamos el tamaño de los botones: permanecen constantes
            UpdateNavMaxHeight();
        }

        private void UpdateNavMaxHeight()
        {
            const double safety = 24.0;

            double total = LayoutRoot.ActualHeight;
            double header = HeaderCard.ActualHeight;
            double footer = FooterPanel.ActualHeight;

            double available = total - header - footer - safety;
            if (available < 80) available = 80;

            NavScroll.MaxHeight = available;
        }

        #endregion
    }
}
