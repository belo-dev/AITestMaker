using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AI_TestMaker.Views
{
    /// <summary>
    /// Lógica de interacción para CambiarPassWindow.xaml
    /// </summary>
    public partial class CambiarPassWindow : Window
    {
        public string ContraseñaActual => TxtActual.Password;
        public string NuevaContraseña => TxtNueva.Password;

        public CambiarPassWindow()
        {
            InitializeComponent();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNueva.Password != TxtNueva2.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}