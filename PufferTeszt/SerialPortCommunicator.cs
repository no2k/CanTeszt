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
        public event EventHandler<string> RawDataReceivedEvent;
        public event EventHandler<string> ResponseReceivedEvent; // esemény a válaszhoz
        public event EventHandler<string> DataSendedEvent;
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private SerialPort serialPort;
        private ParamQueue<Parameter> parameters;
        private AutoResetEvent responseWaiter;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cts;
        public bool IsRuningCommunication { get; private set; } = false;

        public bool RTSInvert { get; set; }

        public SerialPortCommunicator(
            SerialPort port, 
            ParamQueue<Parameter> parameters)
        {
            serialPort = port;
            this.parameters = parameters;
           // parameters.AddParameterEvent +=  OnAddParameter;
            serialPort.DataReceived += OnDataReceived;
            responseWaiter = new AutoResetEvent(false);
            RTSInvert = false;
            this.parameters.ClearParameterEvent += OnClearSendingData;
        }
        
       // public void SetRTSInver(bool isInvert) => RTSInvert = isInvert;

        public void StartCommunication()
        {
            IsRuningCommunication = true;
            cancellationToken = new CancellationToken();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task.Run(() => ProcessParameterItem(cts.Token)); 
        }

        public void StopCommunication()
        {
            IsRuningCommunication = false;
            cts.Cancel(); 
            cts.Dispose();
        }

        private void OnClearSendingData(object sender, EventArgs e)
            =>responseWaiter.Reset();
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedData = serialPort.ReadExisting();
                if (receivedData.Length == 11 && receivedData[0] == '#' && receivedData[10] == '!')
                {
                    ProcessResponse(receivedData);
                }
                else
                {
                    RawDataEventCalling(receivedData);
                }
                receivedData = null;
                responseWaiter.Set();
            }
            catch (Exception)
            {
                responseWaiter.Reset();
                throw;
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
                            // elküldjük a parancsot...
                            byte[] data = Encoding.ASCII.GetBytes(parameter.Param);
                            serialPort.RtsEnable = RTSInvert ? false : true;
                            serialPort.Write(data, 0, data.Length);
                            serialPort.RtsEnable = RTSInvert ? true : false;
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
       
        private void RawDataEventCalling(string rawData)
        {
            RawDataReceivedEvent?.Invoke(this, rawData);
        }
    }
}
