using AI_TestMaker.Classes;
using AI_TestMaker.DB.Login;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AI_TestMaker.Views
{
    public partial class LoginView : UserControl
    {
        private readonly UserRepository _repo = new UserRepository();

        public LoginView()
        {
            InitializeComponent();
            ZoomManager.ZoomChanged += OnZoomChanged;
        }

        private void OnZoomChanged(double zoom)
        {
            LocalZoom.ScaleX = zoom;
            LocalZoom.ScaleY = zoom;
        }

        private void Entrar_Click(object sender, RoutedEventArgs e)
        {
            var user = _repo.Login(TxtUser.Text, TxtPass.Password);

            if (user == null)
            {
                MessageBox.Show("Usuario o contraseña incorrectos");
                return;
            }

            Session.UserId = user.Id;
            Session.Username = user.Username;
            Session.IsGuest = false;
            Session.CurrentUser = user;

            ((MainWindow)Application.Current.MainWindow).Content = new InicioView();
        }

        private void LoginView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Entrar_Click(null, null);
            }
        }

        private void CrearCuenta_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Content = new RegisterView();
        }

        private void Invitado_Click(object sender, RoutedEventArgs e)
        {
            Session.StartGuest();
            ((MainWindow)Application.Current.MainWindow).Content = new InicioView();
        }
    }
}
