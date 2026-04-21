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
            if (TxtPass.Password != TxtPass2.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden");
                return;
            }

            string nombre = TxtNombre.Text.Trim();
            string usuario = TxtUser.Text.Trim();
            string pass = TxtPass.Password.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Introduce tu nombre real.");
                return;
            }

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
