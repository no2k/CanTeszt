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
        public event EventHandler<string> MessageSendingEvent; // esemény a válaszhoz
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private SerialPort serialPort;
        private ParamQueue<Parameter> parameters;
        private AutoResetEvent responseWaiter;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cts;
        private string dataBuffer = string.Empty;
        string receivedData = string.Empty;
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
            serialPort.RtsEnable = RTSInvert ? true : false;
            
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
                var rawRdata = serialPort.ReadExisting();
                RawDataEventCalling(rawRdata);
                receivedData += rawRdata;
                // int startindex = rawRdata.IndexOf(StartBit);
                // if (startindex != -1)
                // {
                //     receivedData += rawRdata.Substring(startindex);
                // }
                // else 
                if (receivedData.Length >= 11)
                {
                    int startindex = receivedData.IndexOf(StartBit);
                    int endindex = receivedData.IndexOf(EndBit);
                    var lenght = receivedData.Length - endindex;
                    string data = receivedData.Substring(startindex , endindex);
                //}
                //MessageSendingEventCalling("Adat méret: " + receivedData.Length);
                //if (receivedData.Length >= 11)
                //{
                    if (receivedData[0] == StartBit && receivedData[10] == EndBit)
                    {
                        ProcessResponse(receivedData);
                        receivedData = string.Empty;
                        responseWaiter.Set();
                    }
                    receivedData = string.Empty;
                }
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
                            SendingData(parameter.Param);
                            responseWaiter.WaitOne();
                        }
                        responseWaiter.Reset();
                    }
                }
                catch (Exception ex)
                {
                    responseWaiter.Reset();
                    throw new Exception("Hiba történt a válasz feldolgozása közben.", ex);
                }
            }
        }

        public void SendingData(string sending)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    // byte[] data = Encoding.ASCII.GetBytes(sending);
                    serialPort.RtsEnable = RTSInvert ? false : true;
                    // RawDataEventCalling($"RTS {(serialPort.RtsEnable ? "Write data" : "Read data")}{Environment.NewLine}");
                    Thread.Sleep(2);
                    var byteArr = Encoding.ASCII.GetBytes(sending);
                    for (var i = 0; i < byteArr.Length; i++)
                    {
                        serialPort.BaseStream.WriteByte(byteArr[i]);
                    }
                    //serialPort.Write(sending.ToArray(), 0, sending.Length);
                    Thread.Sleep(2);
                   
                    // RawDataEventCalling($"RTS {(serialPort.RtsEnable ? "Write data" : "Read data")}{Environment.NewLine}");
                    // Thread.Sleep(5);
                    serialPort.RtsEnable = RTSInvert ? true : false;
                    // RawDataEventCalling($"RTS {(serialPort.RtsEnable ? "Write data" : "Read data")}{Environment.NewLine}");
                    DataSendedEvent?.Invoke(this, sending);
                }
            }
            catch (Exception)
            {
                throw;
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

        private void MessageSendingEventCalling(string message)
        {
            MessageSendingEvent?.Invoke(this, message);
        }
    }
}
