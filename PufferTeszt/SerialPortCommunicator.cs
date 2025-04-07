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
        public event EventHandler<string> ResponseReceived; // esemény a feldolgozáshoz 
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
                        responseTcs.SetResult(receivedData);
                        isWaitingForResponse = false;
                    }
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

        private async void OnAddParameter(object sender, Parameter e)
        {
            await ProcessParameterItem(e);
        }

        private async Task ProcessParameterItem(Parameter parameter)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    // elküldjük a parancsot...
                    byte[] data = Encoding.ASCII.GetBytes(parameter.Param);
                    await serialPort.BaseStream.WriteAsync(data, 0, data.Length);
                    isWaitingForResponse = true;
                    // várunk a válaszra...
                    Task<string> responseTask = responseTcs.Task;
                    Task timeoutTask = Task.Delay(5000); // 5 másodperces timeout
                    Task completedTask = await Task.WhenAny(responseTask, timeoutTask);
                    if (completedTask == responseTask)
                    {
                        // A válasz megérkezett
                        string response = await responseTask;
                        // Feldolgozás...
                        // FIXME: nem feltétlen kell!!!
                        ProcessResponse(response);
                        parameters.Dequeue();
                        // Új TaskCompletionSource létrehozása
                        responseTcs = new TaskCompletionSource<string>();
                        isWaitingForResponse = false;
                    }
                    else
                    {
                        // Timeout történt
                        Console.WriteLine($"Időtúllépés a válaszra: {parameter}");
                        // throw new TimeoutException("A válasz nem érkezett meg időben.");
                        //parameters.Dequeue();
                        responseTcs = new TaskCompletionSource<string>();
                        isWaitingForResponse = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba a soros porti kommunikáció során: {ex.Message}");
                    // Hibakezelés (pl. elem eltávolítása a Q-ból, újrapróbálkozás)
                    // parameters.Dequeue();
                    responseTcs = new TaskCompletionSource<string>();
                    isWaitingForResponse = false;
                }
            }
        }

        private void ProcessResponse(string response)
        {
            ResponseReceived?.Invoke(this, response);
            throw new NotImplementedException("Válasz feldolgozás nincs implementálva!");
        }
    }
}
