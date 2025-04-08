using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
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
        private const string Response = "RSP";
        private const string GetData = "GET";
        private const string SETData = "SET";
        private SerialPort serialPort;
        private ParamQueue<Parameter> parameters;
        private TaskCompletionSource<string> responseTcs; // A válasz megvárásához
        private bool isWaitingForResponse = false;

        public SerialPortCommunicator(SerialPort port, ParamQueue<Parameter> parameters)
        {
            serialPort = port;
            this.parameters = parameters;
            parameters.AddParameterEvent += OnAddParameter;
            serialPort.DataReceived += OnDataReceived;
            responseTcs = new TaskCompletionSource<string>();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedData = serialPort.ReadExisting();
                if (receivedData.Length == 11 && receivedData[0] == '#' && receivedData[10] == '!')
                {
                    ProcessResponse(receivedData);
                    if (isWaitingForResponse)
                    {
                        if (!responseTcs.Task.IsCompleted)
                        {
                            responseTcs.SetResult(receivedData);
                        }
                        isWaitingForResponse = false;
                    }
                    receivedData = null;
                }
                // Ha nem teljes a válasz, tároljuk vagy dolgozzuk fel részlegesen
                // A következő DataReceived eseménykor folytatjuk
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a soros portról érkező adatok feldolgozása során: {ex.Message}");
                if (isWaitingForResponse)
                {
                    responseTcs.SetException(ex);
                    isWaitingForResponse = false;
                }
            }
        }

        private async void OnAddParameter(object sender, EventArgs e)
        {
            await ProcessParameterItem();
        }

        private async Task ProcessParameterItem()
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    while (parameters.Count > 0)
                    {
                        if (!isWaitingForResponse)
                        {
                            var parameter = parameters.Dequeue();
                            var localeTask = new TaskCompletionSource<string>();
                            responseTcs = localeTask;
                            isWaitingForResponse = true;
                            // elküldjük a parancsot...
                            byte[] data = Encoding.ASCII.GetBytes(parameter.Param);
                            serialPort.RtsEnable = true;
                            serialPort.Write(data, 0, data.Length);
                            serialPort.RtsEnable = false;
                            DataSendedEvent?.Invoke(this, parameter.Param);
                        }
                        // várunk a válaszra...
                        //Task<string> responseTask = responseTcs.Task;
                        Task timeoutTask = Task.Delay(10000); // 5 másodperces timeout
                       // Task completedTask = await Task.WhenAny(responseTask, timeoutTask);
                        Task completedTask = await Task.WhenAny(responseTcs.Task, timeoutTask);
                       // várakozni kell
                        isWaitingForResponse = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba a soros porti kommunikáció során: {ex.Message}");
                    isWaitingForResponse = false;
                }
            }
        }

        private void ProcessResponse(string response)
        {
            ResponseReceivedEvent?.Invoke(this, response);
        }
       
    }
}
