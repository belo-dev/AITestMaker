using System.Windows;
using System.Windows.Controls;

namespace AI_TestMaker.Views
{
    public partial class PreguntaControl : UserControl
    {
        private Pregunta _pregunta;

        public PreguntaControl(Pregunta pregunta)
        {
            InitializeComponent();
            _pregunta = pregunta;

            EnunciadoText.Text = pregunta.EnunciadoConNumero;
            CargarOpciones();
        }

        private void CargarOpciones()
        {
            OpcionesPanel.Children.Clear();

            for (int i = 0; i < _pregunta.Opciones.Count; i++)
            {
                var opcion = _pregunta.Opciones[i];

                var rb = new RadioButton
                {
                    Content = opcion.Texto,   // Asumiendo que Opcion tiene Texto
                    Margin = new Thickness(5),
                    GroupName = _pregunta.Enunciado,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                };

                int index = i;
                rb.Checked += (s, e) =>
                {
                    _pregunta.RespuestaUsuario = index;
                };

                // Si ya estaba respondida, marcarla
                if (_pregunta.RespuestaUsuario == index)
                    rb.IsChecked = true;

                OpcionesPanel.Children.Add(rb);
            }
        }
    }
}