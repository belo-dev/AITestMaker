using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_TestMaker.Classes
{
    public static class ZoomManager
    {
        public static double Zoom { get; private set; } = Properties.Settings.Default.ZoomLevel;

        public static event Action<double>? ZoomChanged;

        private const double ZoomStep = 0.1;
        private const double ZoomMin = 0.5;
        private const double ZoomMax = 2.5;

        static ZoomManager()
        {
            // Notificar a las vistas al iniciar
            ZoomChanged?.Invoke(Zoom);
        }

        private static void Save()
        {
            Properties.Settings.Default.ZoomLevel = Zoom;
            Properties.Settings.Default.Save();
        }

        public static void Increase()
        {
            Zoom = Math.Min(Zoom + ZoomStep, ZoomMax);
            Save();
            ZoomChanged?.Invoke(Zoom);
        }

        public static void Decrease()
        {
            Zoom = Math.Max(Zoom - ZoomStep, ZoomMin);
            Save();
            ZoomChanged?.Invoke(Zoom);
        }

        public static void Reset()
        {
            Zoom = 1.0;
            Save();
            ZoomChanged?.Invoke(Zoom);
        }
    }

}
