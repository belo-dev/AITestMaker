using System.Configuration;
using System.Data;
using System.Windows;

namespace AI_TestMaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //var modelosDisponibles = await GroqModelLister.ObtenerModelos("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"); 
            //foreach (var m in modelosDisponibles) MessageBox.Show(m);

            AgentManager.RegistrarAgentes(
                deepseekKey: "sk-1e4cb738e188489b9ea54872b667005f",
                groqKey: "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"
            );
        }
    }

}
