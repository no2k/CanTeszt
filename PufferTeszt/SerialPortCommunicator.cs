using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PufferTeszt
{
    public class SerialPortCommunicator
    {
        public event EventHandler RawDataReceivedEvent;
        public event EventHandler ResponseReceivedEvent; // esemény a válaszhoz
        public event EventHandler DataSendedEvent;
        public event EventHandler MessageSendingEvent; // esemény a válaszhoz
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private SerialPort serialPort;
        private ParamQueue<Parameter> parameters;
        private AutoResetEvent responseWaiter;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cts;
        private string rawData = string.Empty;
        private string receivedData = string.Empty;
        private string sendedData = string.Empty;
        private string msg = string.Empty;
        public bool IsRuningCommunication { get; private set; } = false;

        public bool RTSInvert { get; set; }

        public SerialPortCommunicator(
            SerialPort port, 
            ParamQueue<Parameter> parameters)
        {
            serialPort = port;
            this.parameters = parameters;
            serialPort.DataReceived += OnDataReceived;
            responseWaiter = new AutoResetEvent(false);
            RTSInvert = false;
            this.parameters.ClearParameterEvent += OnClearSendingData;
        }
        
        public void StartCommunication()
        {
            IsRuningCommunication = true;
            serialPort.RtsEnable = RTSInvert ? true : false;
            
            cancellationToken = new CancellationToken();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task.Run(() => ProcessParameterItem(cts.Token)); 
        }

        public string GetReadedData()
        {
            return receivedData;
        }

        public string GetRawData()
        {
            return rawData;
        }
        public string GetSendedData()
        {
            return sendedData;
        }

        public string GetMessage()
        {
            return msg;
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
                rawData = serialPort.ReadExisting();
                RawDataEventCalling();
                receivedData += rawData;
                Thread.Sleep(1);
                // int startindex = rawData.IndexOf(StartBit);
                // if (startindex != -1)
                // {
                //     receivedData += rawData.Substring(startindex);
                // }
                // else 
                if (receivedData.Length >= 11)
                {
                    int startindex = receivedData.IndexOf(StartBit);
                    int endindex = receivedData.IndexOf(EndBit);
                    var lenght = receivedData.Length - endindex;
                    if ( startindex > -1 && endindex > -1)
                    {
                        string data = receivedData.Substring(startindex , endindex);
                        data = data.Substring(0, 11);
                    }
                    if (receivedData[0] == StartBit && receivedData[10] == EndBit)
                    {
                        ProcessResponse();
                        receivedData = string.Empty;
                        responseWaiter.Set();
                    }
                    receivedData = string.Empty;
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                MessageSendingEventCalling();
                responseWaiter.Reset();
                throw;
            }
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
                            if (parameter != null)
                            { 
                                Task.Delay(2);
                                SendingData(parameter.Param);
                                responseWaiter.WaitOne(TimeSpan.FromSeconds(2));
                            }
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
                    serialPort.RtsEnable = RTSInvert ? false : true;
                    Thread.Sleep(10);
                    var byteArr = Encoding.ASCII.GetBytes(sending);
                    for (var i = 0; i < byteArr.Length; i++)
                    {
                        serialPort.BaseStream.WriteByte(byteArr[i]);
                    }
                    Thread.Sleep(2);
                    serialPort.RtsEnable = RTSInvert ? true : false;
                    sendedData = sending;
                    DataSendedEvent?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ProcessResponse()
        {
            ResponseReceivedEvent?.Invoke(this, EventArgs.Empty);
        }
       
        private void RawDataEventCalling()
        {
            RawDataReceivedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void MessageSendingEventCalling()
        {
            MessageSendingEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
