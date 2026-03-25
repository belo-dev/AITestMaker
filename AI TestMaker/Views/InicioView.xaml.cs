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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AI_TestMaker.Views
{
    public partial class InicioView : UserControl
    {
        public InicioView()
        {
            InitializeComponent();
            CargarHistorial();
            IAComboBox.SelectedIndex = 1;
        }

        
        private void CargarHistorial() 
        { 
            var historial = TopicHistoryManager.ObtenerHistorial(); 
            HistorialComboBox.ItemsSource = historial.Select(x => $"{x.Tema} (x{x.Frecuencia})"); 
        }

        private async void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            string dificultad = ((ComboBoxItem)DifficultyComboBox.SelectedItem).Content.ToString();
            string agente = ((ComboBoxItem)IAComboBox.SelectedItem).Content.ToString();
            string tema = TemaTextBox.Text.Trim();

            if (string.IsNullOrEmpty(tema))
            {
                MessageBox.Show("Por favor, ingresa un tema para el test.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Guardar historial
            TopicHistoryManager.AgregarTema(tema);
            CargarHistorial();

            // Mostrar pantalla de carga
            var loading = new LoadingView();
            OverlayRoot.Children.Add(loading);

            List<Pregunta> preguntas = null;

            try
            {
                preguntas = await AIQuestionGenerator.GenerarPreguntasIA(tema, dificultad, agente);
            }
            finally
            {
                loading.FadeOutAndRemove();
            }

            TimeSpan tiempoMaximo = dificultad switch
            {
                "Fácil" => TimeSpan.FromMinutes(25),
                "Medio" => TimeSpan.FromMinutes(60),
                "Difícil" => TimeSpan.FromMinutes(130),
                _ => TimeSpan.FromMinutes(25)
            };

            Test test = new Test(dificultad, preguntas, tiempoMaximo, tema);

            // Transición suave hacia TestView
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
                // Extraer solo el tema (antes del " (xN)")
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
    }
}
