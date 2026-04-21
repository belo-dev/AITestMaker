using AI_TestMaker.Classes;
using AI_TestMaker.DB.Login;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace AI_TestMaker.Views
{
    public partial class InicioView : UserControl
    {
        public InicioView()
        {
            InitializeComponent();
            CargarHistorial();
            IAComboBox.SelectedIndex = 1;
            ZoomManager.ZoomChanged += OnZoomChanged;

            ConfigurarBotonSuperior();
        }

        private void ConfigurarBotonSuperior()
        {
            if (Session.IsGuest)
            {
                // Invitado → icono login
                BtnTopRightIcon.Text = "";
                BtnTopRightText.Text = "Iniciar sesión";

                BtnTopRightEllipse.Visibility = Visibility.Collapsed;

                BtnTopRight.Click += (s, e) =>
                {
                    ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
                };

                UserMenu.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Usuario registrado → mostrar foto + nombre
                BtnTopRightText.Text = Session.CurrentUser?.Nombre ?? Session.Username;

                ActualizarAvatarSuperior(Session.CurrentUser?.Foto);

                BtnTopRight.Click += (s, e) =>
                {
                    UserMenu.PlacementTarget = BtnTopRight;
                    UserMenu.IsOpen = true;
                };

                UserMenu.Visibility = Visibility.Visible;
            }
        }

        private void ActualizarAvatarSuperior(byte[] foto)
        {
            if (foto != null && foto.Length > 0)
            {
                var bitmap = ByteArrayToImage(foto);

                var brush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };
                BtnTopRightFotoBorder.BorderThickness = new Thickness(0);
                BtnTopRightEllipse.Fill = brush;
                BtnTopRightEllipse.Visibility = Visibility.Visible;
                BtnTopRightIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnTopRightEllipse.Visibility = Visibility.Collapsed;
                BtnTopRightIcon.Visibility = Visibility.Visible;
            }
        }

        private BitmapImage ByteArrayToImage(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void CargarHistorial()
        {
            var historial = TopicHistoryManager.ObtenerHistorial();
            HistorialComboBox.ItemsSource = historial.Select(x => $"{x.Tema} (x{x.Frecuencia})");
        }

        private async void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest && Session.GuestTestsUsed >= 1)
            {
                MessageBox.Show("Has alcanzado el límite de tests en modo invitado. Regístrate para continuar.");
                ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
                return;
            }

            if (Session.IsGuest)
                Session.GuestTestsUsed++;

            string dificultad = ((ComboBoxItem)DifficultyComboBox.SelectedItem).Content.ToString();
            string agente = ((ComboBoxItem)IAComboBox.SelectedItem).Content.ToString();
            string tema = TemaTextBox.Text.Trim();

            if (string.IsNullOrEmpty(tema))
            {
                MessageBox.Show("Por favor, ingresa un tema para el test.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TopicHistoryManager.AgregarTema(tema);
            CargarHistorial();

            var loading = new LoadingView();
            OverlayRoot.Children.Add(loading);

            var preguntas = await AIQuestionGenerator.GenerarPreguntasIA(tema, dificultad, agente);

            loading.FadeOutAndRemove();

            TimeSpan tiempoMaximo = dificultad switch
            {
                "Fácil" => TimeSpan.FromMinutes(25),
                "Medio" => TimeSpan.FromMinutes(60),
                "Difícil" => TimeSpan.FromMinutes(130),
                _ => TimeSpan.FromMinutes(25)
            };

            Test test = new Test(dificultad, preguntas, tiempoMaximo, tema);

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fade.Completed += (s, e2) =>
            {
                ((MainWindow)Application.Current.MainWindow).Content = new TestView(test);
            };

            this.BeginAnimation(OpacityProperty, fade);
        }

        private void HistorialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistorialComboBox.SelectedItem is string item)
            {
                string tema = item.Split("(x")[0].Trim();
                TemaTextBox.Text = tema;
            }
        }

        private void BorrarHistorial_Click(object sender, RoutedEventArgs e)
        {
            TopicHistoryManager.BorrarHistorial();
            CargarHistorial();
            MessageBox.Show("Historial borrado correctamente.");
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content =
              new HistorialView();
        }

        private void MenuPerfil_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new ProfileView();
        }

        private void MenuCambiarPass_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new CambiarPassWindow();
            ventana.Owner = Application.Current.MainWindow;
            ventana.ShowDialog();
        }

        private void MenuLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();
            ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
        }

        private void BtnTopRight_Click(object sender, RoutedEventArgs e)
        {
            var mouseEvent = e as MouseButtonEventArgs;

            // Si es invitado → clic izquierdo abre login
            if (Session.IsGuest)
            {
                ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
                return;
            }

            // Si NO es invitado → solo abrir menú con clic derecho
            if (mouseEvent != null && mouseEvent.ChangedButton == MouseButton.Right)
            {
                UserMenu.PlacementTarget = BtnTopRight;
                UserMenu.IsOpen = true;
            }
            else
            {
                // Clic izquierdo en usuario registrado → abrir perfil
                ((MainWindow)Application.Current.MainWindow).Content = new ProfileView();
            }
        }
    }
}
