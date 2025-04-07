using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using SharedModels.Models;
namespace PufferTeszt
{
    public partial class PufferTeszt : Form
    {
        
        private List<int> BaudRates { get; } = new List<int>() { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200 };
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private const string Response = "RSP";
        private const string GetData = "GET";
        private const string SETData = "SET";
        private int dataIndex = 0;
        string receiveBuffer = "";
        int lastStatus = 0;
        int lastOpened = 0;
        int lastClosed = 0;
        ParameterQueue CommandList = new ParameterQueue();
        private TaskCompletionSource<string> responseTcs; // A válasz megvárásához

        public PufferTeszt()
        {
            InitializeComponent();
            InitializePort();
            CommandList.AddParameterEvent += QueryQueueParametersProcessorAsync;
            responseTcs = new TaskCompletionSource<string>();
        }

        private void InitializePort()
        {
            try
            {
                Baud_Cbx.DataSource = BaudRates;
                Baud_Cbx.SelectedIndex = BaudRates.IndexOf(9600);
                PortList_Cbx.DataSource = SerialPort.GetPortNames();
                serialPort1.Encoding = Encoding.UTF8;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Parity = Parity.None;
                serialPort1.DataBits = 8;
                serialPort1.WriteBufferSize = 1024;
                serialPort1.ReadBufferSize = 1024;
                serialPort1.Handshake = Handshake.None; // RTS szoftveres vezérlés
                serialPort1.ReadTimeout = 5000;
                serialPort1.WriteTimeout = 5000;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PortList_Cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName =  PortList_Cbx.SelectedItem.ToString();
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.Open();
                ConnectBtn.Enabled = false;
                DisconnectBtn.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Baud_Cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            
           serialPort1.BaudRate = (int)Baud_Cbx.SelectedItem;
           
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.Close();
                DisconnectBtn.Enabled = false;
                ConnectBtn.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PufferTeszt_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                serialPort1.Close();
                serialPort1 = null;
               // serialPort1.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }  

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.Invoke(new EventHandler(ProcessingData));
        }

        private void ProcessingData(object sender, EventArgs e)
        {
            try
            {
                //  SerialPort sp = (SerialPort)sender;
                // string indata = sp.ReadExisting();
               string receiveBuffer = serialPort1.ReadExisting();
               // receiveBuffer += indata;
                if (receiveBuffer.Length == 11)
                {
                    IOTableViewDGV.Rows.Add(ConvertToDGViewParams(DateTime.Now, receiveBuffer, ++dataIndex));
                    RefreshDashboard(receiveBuffer);
                    receiveBuffer = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private object[] ConvertToDGViewParams(DateTimeOffset time, string data, int index) 
        {
            try
            {
                 string[] dataArr = SeparateData(data);
                 return new object[] { index, time.ToString(), data, dataArr[0], dataArr[1], dataArr[2]};            
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nem sikerült a nyers adatok konvertálása!\n\r" + ex.Message);
                return default;
            }
        }

        private void RefreshDashboard(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; };
            if ( data.First() == StartBit && data.Last() == EndBit)
            {
                var dataArr = SeparateData(data);
                if (dataArr.Length > 0 && dataArr[0] == Response) {
                    switch (dataArr[1])
                    {
                        case "P0": ActualEloreTxb.Text = dataArr[2];
                            break;
                        case "P1": ActualVisszaTxb.Text = dataArr[2];
                            break;
                        case "P2": ActualTEloreTxb.Text = dataArr[2];
                            break;
                        case "P3": ActualTVisszaTxb.Text = dataArr[2];
                            break;
                        case "P4": ActualTFelsoTxb.Text = dataArr[2];
                            break;
                        case "P5": ActualTKozepTxB.Text = dataArr[2];
                            break;
                        case "P6": ActualTAlsoTxb.Text = dataArr[2];
                            break;
                        case "P7": BelsoTxb.Text = dataArr[2];
                            break;
                        case "KI": ActualOpenTxb.Text = dataArr[2];
                            break;
                        case "BE": ActualCloseTxb.Text = dataArr[2];
                            break;
                        case "ST": ActualStatusTxb.Text = dataArr[2];
                            break;
                    }      
                }
            }        
        }

        private string[] SeparateData(string data)
        {
            if (data.Length == 0) { return Array.Empty<string>(); }
           return data.TrimStart(StartBit)
                .TrimEnd(EndBit)
                .Split(Delimiter);
        }

        private void BealllitBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if(lastClosed != (int)NewCloseNud.Value)
                {
                    string numeric = NewCloseNud.Value.ToString("D2");
                    var data = StartBit 
                        + SETData 
                        + Delimiter 
                        + "KI" 
                        + Delimiter 
                        + numeric 
                        + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
                if(lastOpened != (int)NewOpenNud.Value)
                {
                    string numeric = NewOpenNud.Value.ToString("D2");
                    var data = StartBit
                       + SETData
                       + Delimiter
                       + "BE"
                       + Delimiter
                       + numeric
                       + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
                if (lastStatus != (int)NewStatusNud.Value)
                {
                    string numeric = NewStatusNud.Value.ToString("D2");
                    var data = StartBit
                       + SETData
                       + Delimiter
                       + "ST"
                       + Delimiter
                       + numeric
                       + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
                if (FullZarCbx.Checked) 
                {
                    var data = StartBit
                       + SETData
                       + Delimiter
                       + "OF"
                       + Delimiter
                       + "00"
                       + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
                if (FullNyitCbx.Checked)
                {
                    var data = StartBit
                       + SETData
                       + Delimiter
                       + "ON"
                       + Delimiter
                       + "00"
                       + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
                if (VeszhutesCBx.Checked)
                {
                    var data = StartBit
                       + SETData
                       + Delimiter
                       + "EM"
                       + Delimiter
                       + "00"
                       + EndBit;
                    SendData(data);
                    System.Threading.Thread.Sleep(100);
                    //FIXME: visszaellenőrzés?
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SendData(string data)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.RtsEnable = true;
                    serialPort1.Write(data);
                    serialPort1.RtsEnable = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void FetchTimeNud_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)FetchTimeNud.Value*1000;
        }

        private void StartFetchBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serialPort1.IsOpen)
                {
                    switch
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void QueryQueueParametersProcessorAsync(object sender, EventArgs e)
        {
            // amikor feldolgozunk egy elemet megvárjuk a választ,
            // ha megjött a válasz akkor aszinkron kiíratjuk az eredményét
            // majd elküldjük a következő kérést.
            Parameter parameter;
            while
        }

        private async void RefreshDashboardAsync(string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                RefreshDashboard(data);
                var actualRowIndex = IOTableViewDGV.Rows.Count; 
                IOTableViewDGV.Rows.Add(ConvertToDGViewParams(DateTime.Now, data, actualRowIndex + 1));
            }
        }
    }
}
