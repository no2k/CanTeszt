using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
//using SharedModels.Models;
namespace PufferTeszt
{
    public partial class PufferTeszt : Form
    {
        private ParamQueue<Parameter> parameters = new ParamQueue<Parameter>();
        private SerialPortCommunicator communicator;
       
        private List<int> BaudRates { get; } = new List<int>() { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200 };
        private const char StartBit = '#';
        private const char EndBit = '!';
        private const char Delimiter = ';';
        private const string Response = "RSP";
        private const string GetData = "GET";
        private const string SETData = "SET";
        private int dataIndex = 0;
       
        int lastStatus = 0;
        int lastOpened = 0;
        int lastClosed = 0;
        NumberFormatInfo nfi = new NumberFormatInfo(); 

        public PufferTeszt()
        {
            InitializeComponent();
            try
            {
                InitializePort();
                communicator = new SerialPortCommunicator(serialPort1, parameters);
                communicator.ResponseReceivedEvent += ProcessingResponseData;
                communicator.DataSendedEvent += OnDataSended;
                communicator.RawDataReceivedEvent += OnReceivedRawData;
                nfi.NumberDecimalDigits = 2;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
            catch (Exception)
            {
                throw;
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
                communicator.StartCommunication();
                PortList_Cbx.Enabled = false;
                Baud_Cbx.Enabled = false;
                StateBox.Enabled = true;
                ScannBox.Enabled = true;
                IOTableViewDGV.Rows.Clear();
                ReceivedDataTbx.Clear();
                dataIndex = 0;
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
                timer1.Enabled = false;
                serialPort1.Close();
                DisconnectBtn.Enabled = false;
                ConnectBtn.Enabled = true;
                communicator.StopCommunication();
                parameters.ClearParameters();
                PortList_Cbx.Enabled = true;
                Baud_Cbx.Enabled = true;
                StateBox.Enabled = false;
                ScannBox.Enabled = false;
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
                communicator.StopCommunication();
                serialPort1.Close();
                serialPort1 = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }  

        private void OnDataSended(object sender, string data)
        {
            try
            {
                this.Invoke(new Action<string>(RefreshDGVBoard), data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnReceivedRawData(object sender, string rawData)
        {
           this.Invoke(new Action<string>(RefreshReceivedRawData), rawData);
        }

        private void RefreshReceivedRawData(string rawData)
        {
            ReceivedDataTbx.Text += $"{rawData} -> {string.Join(" ", rawData.Select(x => ((int)x).ToString("X2")))}{Environment.NewLine}";
        }
        private void ProcessingResponseData(object sender, string response)
        {
            try
            {
                this.Invoke(new Action<string>(RefreshDashboard), response);
                this.Invoke(new Action<string>(RefreshDGVBoard), response);
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
                 return new object[] { index, time.ToString("f"), data, dataArr[0], dataArr[1], dataArr[2]};            
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nem sikerült a nyers adatok konvertálása!\n\r" + ex.Message);
                return default;
            }
        }

        private void RefreshDGVBoard(string data)
            => IOTableViewDGV.Rows.Add(ConvertToDGViewParams(DateTime.Now, data, ++dataIndex));
     
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
                        case "KI": 
                            {
                                ActualCloseTxb.Text = dataArr[2];
                                lastClosed = int.Parse(dataArr[2]);
                            }
                            break;
                        case "BE":
                            {
                                ActualOpenTxb.Text = dataArr[2];
                                lastOpened = int.Parse(dataArr[2]);
                            }
                            break;
                        case "ST":
                            {
                                var value = int.Parse(dataArr[2]);
                                ActualStatusTxb.Text = dataArr[2];
                                lastStatus = value;
                                if (value >= 99)
                                {
                                    FullNyitCbx.ForeColor = Color.FromArgb(25, 0, 255, 0);
                                    FullZarCbx.ForeColor = Color.FromArgb(25, 255, 0, 0);
                                }
                                else if (value <= 1)
                                {
                                    FullNyitCbx.ForeColor = Color.FromArgb(25, 255, 0, 0);
                                    FullZarCbx.ForeColor = Color.FromArgb(25, 0, 255, 0);
                                }
                                else
                                {
                                    FullNyitCbx.ForeColor = Color.FromArgb(25, Color.LightYellow);
                                    FullZarCbx.ForeColor = Color.FromArgb(25, Color.LightYellow);
                                }
                            }
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
                if (lastClosed != (int)NewCloseNud.Value)
                {
                    var data = GenerateSendingData(SETData, "KI", (int)NewCloseNud.Value);
                    parameters.Enqueue(new Parameter(data, true));
                }
                if (lastOpened != (int)NewOpenNud.Value)
                {
                    var data = GenerateSendingData(SETData, "BE", (int)NewOpenNud.Value);
                    parameters.Enqueue(new Parameter(data, true));

                }
                if (lastStatus != (int)NewStatusNud.Value)
                {
                    var data = GenerateSendingData(SETData, "ST", (int)NewStatusNud.Value);
                    parameters.Enqueue(new Parameter(data, true));
                }
                if (FullZarCbx.Checked)
                {
                    var data = GenerateSendingData(SETData, "OF", 0);
                    parameters.Enqueue(new Parameter(data, true));
                }
                if (FullNyitCbx.Checked)
                {
                    var data = GenerateSendingData(SETData, "ON", 0);
                    parameters.Enqueue(new Parameter(data, true));
                }
                if (VeszhutesCBx.Checked)
                {
                    var data = GenerateSendingData(SETData, "EM", 0);
                    parameters.Enqueue(new Parameter(data, true));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FetchTimeNud_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)FetchTimeNud.Value*1000;
        }

        public string GenerateSendingData(string command, string id, int value = 0 )
        {
            var data = StartBit
                       + command
                       + Delimiter
                       + id
                       + Delimiter
                       + value.ToString("00")
                       + EndBit;
            return data;
        }

        private void StartFetchBtn_Click(object sender, EventArgs e)
        {
            StartFetchBtn.Enabled = false;
            StopFetchBtn.Enabled = true;
            timer1.Start();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            var data = GenerateSendingData(GetData, "P0");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P1");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P2");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P3");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P4");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P5");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P6");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "P7");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "KI");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "BE");
            parameters.Enqueue(new Parameter(data, true));
            data = GenerateSendingData(GetData, "ST");
            parameters.Enqueue(new Parameter(data, true));
        }

        private void StopFetchBtn_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            StartFetchBtn.Enabled = true;
            StopFetchBtn.Enabled = false;
        }

        private void RTSInvertCbx_CheckedChanged(object sender, EventArgs e)
        {
            communicator.RTSInvert = RTSInvertCbx.Checked;
        }
    }
}
