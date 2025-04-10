using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PufferTeszt
{
    public class ParamQueue<T> where T : class
    {
        public event EventHandler AddParameterEvent;
       
        public event EventHandler RemoveParameterEvent;

        public event EventHandler ClearParameterEvent;

        public int Count => parameters.Count;
       
        private Queue<T> parameters = new Queue<T>();

        public void Enqueue(T parameter)
        {
           // parameter.QIndex = parameters.Count + 1;
            parameters.Enqueue(parameter);
            AddParameterEvent?.Invoke(this, EventArgs.Empty);
        }

        public void ClearParameters()
        {
            parameters.Clear();
            ClearParameterEvent?.Invoke(this, EventArgs.Empty);
        }

        public T Dequeue()
        {
            if (parameters.Count > 0)
            {
                var param = parameters.Dequeue();
                RemoveParameterEvent?.Invoke(this, EventArgs.Empty);
                parameters.TrimExcess();
                return param;
            }
            return null;
        }

    }
}
