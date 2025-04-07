using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PufferTeszt
{
    public class Parameter
    {
        private string param;
        private bool isSetCommand;

        public int QIndex { get; set; } = default;
        
        public string Param { get => param; }

        bool IsSetCommand { get => isSetCommand; }
        
        public Parameter(string param, bool isSetCommand)
        {
            this.param = param;
            this.isSetCommand = isSetCommand;
        }
       
    }
}
