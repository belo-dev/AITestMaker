using AI_TestMaker.Classes;
using AI_TestMaker.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AI_TestMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Content = new LoginView();

            PreviewMouseWheel += MainWindow_PreviewMouseWheel;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }
        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                    ZoomManager.Increase();
                else
                    ZoomManager.Decrease();

                e.Handled = true;
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                    ZoomManager.Increase();

                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                    ZoomManager.Decrease();

                else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
                    ZoomManager.Reset();
            }
        }
    }

}