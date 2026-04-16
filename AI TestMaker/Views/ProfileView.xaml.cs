using AI_TestMaker.DB.Login;
using AI_TestMaker.DB;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void CargarDatosUsuario()
        {
            // Usuario
            TxtUsername.Text = Session.Username;

            // Estado
            TxtEstado.Text = Session.IsGuest ? "Invitado" : "Usuario registrado";

            // Tests realizados
            var tests = _db.ListarTests();
            TxtTestsRealizados.Text = tests.Count.ToString();

            // Último test
            if (tests.Count > 0)
            {
                var ultimo = tests.OrderByDescending(t => t.Fecha).First();
                TxtUltimoTest.Text = ultimo.Fecha.ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                TxtUltimoTest.Text = "Ninguno";
            }

            // Visibilidad según invitado / registrado
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

        // Navegación
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
