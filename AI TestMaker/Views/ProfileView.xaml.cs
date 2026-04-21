using AI_TestMaker.DB.Login;
using AI_TestMaker.DB;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AI_TestMaker.Classes;
using System.Windows.Media;

namespace AI_TestMaker.Views
{
    public partial class ProfileView : UserControl
    {
        private readonly DatabaseManager _db;

        public ProfileView()
        {
            InitializeComponent();
            _db = new DatabaseManager();

            CargarDatosUsuario();
            CargarFotoPerfil();
            CargarNombre();
            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void CargarDatosUsuario()
        {
            TxtEstado.Text = Session.IsGuest ? "Invitado" : "Usuario registrado";

            var tests = _db.ListarTests();
            TxtTestsRealizados.Text = tests.Count.ToString();

            if (tests.Count > 0)
            {
                var ultimo = tests.OrderByDescending(t => t.Fecha).First();
                TxtUltimoTest.Text = ultimo.Fecha.ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                TxtUltimoTest.Text = "Ninguno";
            }

            if (Session.IsGuest)
            {
                BannerInvitado.Visibility = Visibility.Visible;
                BtnCrearCuenta.Visibility = Visibility.Visible;

                BtnCambiarPass.Visibility = Visibility.Collapsed;
                BtnEliminarCuenta.Visibility = Visibility.Collapsed;
            }
            else
            {
                BannerInvitado.Visibility = Visibility.Collapsed;
                BtnCrearCuenta.Visibility = Visibility.Collapsed;

                BtnCambiarPass.Visibility = Visibility.Visible;
                BtnEliminarCuenta.Visibility = Visibility.Visible;
            }
        }

        private void CargarNombre()
        {
            TxtUsername.Text = Session.CurrentUser?.Nombre ?? Session.Username;
        }

        private void GuardarNombre(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
                return;

            string nuevoNombre = TxtUsername.Text.Trim();
            if (string.IsNullOrEmpty(nuevoNombre))
                return;

            var repo = new UserRepository();
            repo.ActualizarNombre(Session.UserId, nuevoNombre);

            Session.CurrentUser.Nombre = nuevoNombre;
        }

        private void CargarFotoPerfil()
        {
            var foto = Session.CurrentUser?.Foto;

            if (foto != null && foto.Length > 0)
            {
                SetProfileImage(foto, offsetY: -4);
            }
            else
            {
                // Si existe ProfileImageBrush, limpiarlo; si existe ImgFoto, ocultarlo
                TryClearProfileImage();
            }
        }

        /// <summary>
        /// Asigna la foto de perfil. Soporta dos escenarios:
        /// 1) XAML usa Ellipse + ImageBrush llamado "ProfileImageBrush"
        /// 2) XAML usa Image llamado "ImgFoto"
        /// offsetY mueve la imagen dentro del círculo (negativo sube).
        /// </summary>
        private void SetProfileImage(byte[] fotoBytes, double offsetY = -4)
        {
            if (fotoBytes == null || fotoBytes.Length == 0)
            {
                TryClearProfileImage();
                return;
            }

            var bitmap = ByteArrayToImage(fotoBytes);

            // Intentar usar ImageBrush (Ellipse)
            var brushField = this.FindName("ProfileImageBrush") as ImageBrush;
            if (brushField != null)
            {
                brushField.ImageSource = bitmap;

                // Asegurar que exista un TranslateTransform para desplazar la imagen dentro del brush
                if (brushField.Transform is TranslateTransform tt)
                {
                    tt.Y = offsetY;
                }
                else
                {
                    brushField.Transform = new TranslateTransform(0, offsetY);
                }

                // Ocultar icono fallback si existe
                var icono = this.FindName("IconoFoto") as UIElement;
                if (icono != null) icono.Visibility = Visibility.Collapsed;

                // Si existe ImgFoto (fallback), ocultarlo
                var img = this.FindName("ImgFoto") as Image;
                if (img != null) img.Visibility = Visibility.Collapsed;

                return;
            }

            // Si no hay ImageBrush, intentar con Image (ImgFoto)
            var imgFallback = this.FindName("ImgFoto") as Image;
            if (imgFallback != null)
            {
                imgFallback.Source = bitmap;
                imgFallback.Visibility = Visibility.Visible;

                // Si quieres desplazar visualmente la Image sin recortar, puedes usar RenderTransform,
                // pero recuerda que si tienes un Clip fijo, mover la Image puede recortar partes.
                // Aquí no aplicamos transform por defecto; si quieres, descomenta:
                // imgFallback.RenderTransform = new TranslateTransform(0, -4);

                var icono = this.FindName("IconoFoto") as UIElement;
                if (icono != null) icono.Visibility = Visibility.Collapsed;

                return;
            }

            // Si no se encontró ninguno, nada que hacer (evitar excepciones)
        }

        private void TryClearProfileImage()
        {
            var brushField = this.FindName("ProfileImageBrush") as ImageBrush;
            if (brushField != null)
            {
                brushField.ImageSource = null;
            }

            var imgFallback = this.FindName("ImgFoto") as Image;
            if (imgFallback != null)
            {
                imgFallback.Source = null;
                imgFallback.Visibility = Visibility.Collapsed;
            }

            var icono = this.FindName("IconoFoto") as UIElement;
            if (icono != null) icono.Visibility = Visibility.Visible;
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

        private void CambiarFoto_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Session.IsGuest)
            {
                MessageBox.Show("Los invitados no pueden cambiar la foto de perfil.", "Función limitada",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";

            if (dialog.ShowDialog() == true)
            {
                byte[] bytes = File.ReadAllBytes(dialog.FileName);

                // Mostrar en la UI usando el método robusto
                SetProfileImage(bytes, offsetY: -4);

                // Guardar en sesión
                Session.CurrentUser.Foto = bytes;

                // Guardar en BD
                var repo = new UserRepository();
                repo.ActualizarFoto(Session.UserId, bytes);
            }
        }

        private void IrInicio_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new InicioView();
        }

        private void VerHistorial_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new HistorialView();
        }

        private void CrearCuenta_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new RegisterView();
        }

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();
            ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
        }

        private void CambiarPass_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new CambiarPassWindow();
            ventana.Owner = Application.Current.MainWindow;

            if (ventana.ShowDialog() == true)
            {
                var repo = new UserRepository();
                bool ok = repo.CambiarContraseña(Session.UserId, ventana.ContraseñaActual, ventana.NuevaContraseña);

                if (ok)
                    MessageBox.Show("Contraseña cambiada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("La contraseña actual no es correcta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EliminarCuenta_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Seguro que deseas eliminar tu cuenta? Esta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var repo = new UserRepository();
                bool ok = repo.EliminarCuenta(Session.UserId);

                if (ok)
                {
                    MessageBox.Show("Cuenta eliminada correctamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    Session.Logout();
                    ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
                }
                else
                {
                    MessageBox.Show("Error al eliminar la cuenta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
