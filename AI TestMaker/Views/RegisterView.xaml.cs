using AI_TestMaker.Classes;
using AI_TestMaker.DB.Login;
using System.Windows;
using System.Windows.Controls;

namespace AI_TestMaker.Views
{
    public partial class RegisterView : UserControl
    {
        private readonly UserRepository _repo = new UserRepository();

        public RegisterView()
        {
            InitializeComponent();
            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void Registrarse_Click(object sender, RoutedEventArgs e)
        {
            string nombre = TxtNombre.Text.Trim();
            string usuario = TxtUser.Text.Trim();
            string pass = TxtPass.Password.Trim();

            // Validación: nombre real obligatorio
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Introduce tu nombre real.");
                return;
            }

            // Validación: email con regex
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                usuario,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Introduce un correo electrónico válido.");
                return;
            }

            // Validación: contraseña fuerte
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                pass,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$"))
            {
                MessageBox.Show("La contraseña debe tener al menos 8 caracteres, incluir mayúsculas, minúsculas, números y un símbolo.");
                return;
            }

            if (TxtPass.Password != TxtPass2.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden");
                return;
            }

            // Registrar usuario
            bool ok = _repo.Register(usuario, pass, nombre);

            if (!ok)
            {
                MessageBox.Show("El usuario ya existe");
                return;
            }

            MessageBox.Show("Cuenta creada correctamente");

            ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
        }



        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new LoginView();
        }
    }
}
