using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_TestMaker
{
    public interface IAgent
    {
        string Nombre { get; }
        Task<string> ChatCompletion(string prompt);
    }
}
