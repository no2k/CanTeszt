using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PufferTeszt
{
    public class ParameterQueue
    {
        public event EventHandler AddParameterEvent;
        public event EventHandler RemoveParameterEvent;

        private Queue<Parameter> parameters = new Queue<Parameter>();

        public void EnqueueParameter(Parameter parameter)
        {
           // parameter.QIndex = parameters.Count + 1;
            parameters.Enqueue(parameter);
            AddParameterEvent?.Invoke(this, EventArgs.Empty);
        }

        public Parameter DequeueParameter()
        {
            if (parameters.Count > 0)
            {
                RemoveParameterEvent?.Invoke(this, EventArgs.Empty);
                var param = parameters.Dequeue().QIndex =parameters.Count;
                parameters.TrimExcess();
                return parameters.Dequeue();
            }
            return null;
        }

        //private void UpdateQueueElementIndex(Queue<Parameter> oldQueue)
        //{
        //    if (queue.Count > 0)
        //    {
        //        queue.Dequeue(); // Eltávolítjuk az első elemet

        //        Queue<MyQueueItem> tempQueue = new Queue<MyQueueItem>();
        //        int newIndex = 0;
        //        while (queue.Count > 0)
        //        {
        //            MyQueueItem item = queue.Dequeue();
        //            item.Index = newIndex++;
        //            tempQueue.Enqueue(item);
        //        }

        //        // Az ideiglenes queue tartalmának visszamásolása az eredetibe
        //        while (tempQueue.Count > 0)
        //        {
        //            queue.Enqueue(tempQueue.Dequeue());
        //        }
        //    }
        //}

    }
}
