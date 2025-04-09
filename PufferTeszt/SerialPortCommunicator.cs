using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PufferTeszt
{
    public class SerialPortCommunicator
    {
        public event EventHandler<string> ResponseReceivedEvent; // esemény a válaszhoz
        public event EventHandler<string> DataSendedEvent;
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private SerialPort serialPort;
        private ParamQueue<Parameter> parameters;
        private bool isWaitingForResponse = false;
        private AutoResetEvent responseWaiter;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenSource cts;

        public SerialPortCommunicator(
            SerialPort port, 
            ParamQueue<Parameter> parameters, 
            CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            serialPort = port;
            this.parameters = parameters;
           // parameters.AddParameterEvent +=  OnAddParameter;
            serialPort.DataReceived += OnDataReceived;
            responseWaiter = new AutoResetEvent(false);
        }

        public void StartCommunication()
            => Task.Run(() => ProcessParameterItem(cts.Token));

        public void StopCommunication()
        {
            cts.Cancel(); 
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedData = serialPort.ReadExisting();
                if (receivedData.Length == 11 && receivedData[0] == '#' && receivedData[10] == '!')
                {
                    ProcessResponse(receivedData);
                    receivedData = null;
                }
                responseWaiter.Set();
            }
            catch (Exception ex)
            {
                responseWaiter.Reset();
            }
        }

        private void OnAddParameter(object sender, EventArgs e)
        {
           // ProcessParameterItem();
        }

        private void ProcessParameterItem(CancellationToken cancellation)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        if (parameters.Count > 0)
                        {
                            var parameter = parameters.Dequeue();
                            var localeTask = new TaskCompletionSource<string>();
                            isWaitingForResponse = true;
                            // elküldjük a parancsot...
                            byte[] data = Encoding.ASCII.GetBytes(parameter.Param);
                            serialPort.RtsEnable = true;
                            serialPort.Write(data, 0, data.Length);
                            serialPort.RtsEnable = false;
                            DataSendedEvent?.Invoke(this, parameter.Param);
                            responseWaiter.WaitOne();
                        }
                        responseWaiter.Reset();
                        Task.Delay(1000).Wait(); // várunk egy kicsit, hogy a sorban lévő parancsok feldolgozódjanak
                    }
                }
                catch (Exception ex)
                {
                    responseWaiter.Reset();
                    throw new Exception("Hiba történt a válasz feldolgozása közben.", ex);
                }

            }
        }

        private void ProcessResponse(string response)
        {
            ResponseReceivedEvent?.Invoke(this, response);
        }
       
    }
}
