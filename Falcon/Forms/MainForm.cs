﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Falcon.Com;
using System.IO.Ports;
using Falcon.Forms;
using Falcon.Utils;
using Falcon.Com;
using System.Threading;
using Renci.SshNet;
using System.Text.RegularExpressions;
using Renci.SshNet.Common;
using Falcon.Command;
using System.Net.NetworkInformation;

namespace Falcon
{
    public partial class MainForm : Form
    {
        private AboutForm aboutForm_;
        private GraphForm graphFrom_;
        private PreferencesForm preferencesForm_;

        Ssh ssh_;

        public MainForm()
        {
            InitializeComponent();

            this.SetStyle(
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer, true);

            serialParityCmBx.SelectedIndex = 0;
            serialStopBitsCmBx.SelectedIndex = 1;
            serialBaudCmBx.SelectedIndex = 0;
            UpdateSerialPorts();

            LoadSerialSettigns();
            LoadTcpSettings();
            LoadUdpSettings();
        }

        private void UpdateSerialPorts()
        {
            serialComCmBx.Items.Clear();
            var availablePorts = SerialCom.GetConnectedPorts();
            serialComCmBx.Items.AddRange(availablePorts);
            if (serialComCmBx.Items.Count > 0)
                serialComCmBx.SelectedIndex = 0;
        }

        private void tcpConnectBtn_Click(object sender, EventArgs e)
        {
            ConnectTcp();
        }
        /*
        void HandleKeyEvent(Object sender, AuthenticationPromptEventArgs e)
        {
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = "0035";
                }
            }
        }

        
        private void ConnectSsh()
        {
            var PasswordConnection = new PasswordAuthenticationMethod("submachine", "0035");
            var KeyboardInteractive = new KeyboardInteractiveAuthenticationMethod("submachine");
            KeyboardInteractive.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);
            var ConnectionInfo = new ConnectionInfo("192.168.2.101", 22, "submachine", PasswordConnection, KeyboardInteractive);
            using (var client = new SshClient(ConnectionInfo))
            {
                client.Connect();
                string reply = string.Empty;
                var shellStream = client.CreateShellStream("dumb", 80, 24, 800, 600, 1024);
                client.RunCommand("ls -a");
                reply = shellStream.ReadLine();
                dataInScreenTxt.Text = "Reply: " + reply;
        
            }
      
    
        }*/

        private void ConnectTcp()
        {
            SaveTcpSettings();
            bool connected = false;
            if (tcpServerRdBtn.Checked)
            {
                ConnectionsManager.Inst.InitTcpServer((int)tcpPortTxt.Value);
                if (ConnectionsManager.Inst.TCPServer.Connect())
                {
                    ConnectionsManager.Inst.TCPServer.SubscribeToMsgs(OnTcpByteIn);
                    ConnectionsManager.Inst.TCPServer.NotifyOnNewClient(OnNewTcpClient);
                    tcpClientRdBtn.Enabled = false;
                    connected = true;
                }
            }
            else
            {
                ConnectionsManager.Inst.InitTcpClient();
                if (ConnectionsManager.Inst.TCPClient.ConnectTo(tcpIpTxt.Text, (int)tcpPortTxt.Value))
                {
                    ConnectionsManager.Inst.TCPClient.Subscribe(OnTcpByteIn);
                    tcpServerRdBtn.Enabled = false;
                    connected = true;
                }
                else
                {
                    MsgBox.WarningMsg("TCP Connection Failed", "Wrong IP or port address");
                }
            }
            if (connected)
            {
                tcpConnectBtn.Enabled = false;
                tcpDisconnectBtn.Enabled = true;
                tcpConnectionStateLbl.Text = "Connected";
                tcpConnectionStateLbl.BackColor = Color.LimeGreen;
                tcpIndicatorLbl.BackColor = Color.LimeGreen;
            }
            else
            {
                tcpConnectionStateLbl.Text = "Failed";
                tcpConnectionStateLbl.BackColor = Color.DarkOrange;
            }
        }

        private void SaveTcpSettings()
        {
            Properties.Settings.Default.tcpIp = tcpIpTxt.Text;
            Properties.Settings.Default.tcpPort = (uint)tcpPortTxt.Value;
            Properties.Settings.Default.tcpServerChecked = tcpServerRdBtn.Checked;
            SaveProperties();
        }

        private void LoadTcpSettings()
        {
            tcpIpTxt.Text = Properties.Settings.Default.tcpIp;
            tcpPortTxt.Value = (decimal)Properties.Settings.Default.tcpPort;
            tcpServerRdBtn.Checked = Properties.Settings.Default.tcpServerChecked;
            tcpClientRdBtn.Checked = !tcpServerRdBtn.Checked;
            tcpIpTxt.Enabled = tcpClientRdBtn.Checked;
        }

        private void SaveUdpSettings()
        {
            Properties.Settings.Default.udpIp = udpIpTxt.Text;
            Properties.Settings.Default.udpPort = (uint)udpPortTxt.Value;
            Properties.Settings.Default.udpServerChecked = udpServerRdBtn.Checked;
            SaveProperties();
        }

        private void LoadUdpSettings()
        {
            udpIpTxt.Text = Properties.Settings.Default.udpIp;
            udpPortTxt.Value = (decimal)Properties.Settings.Default.udpPort;
            udpServerRdBtn.Checked = Properties.Settings.Default.udpServerChecked;
            udpClientRdBtn.Checked = !udpServerRdBtn.Checked;
            udpIpTxt.Enabled = udpClientRdBtn.Checked;
        }

        private void SaveSerialSettings()
        {
            Properties.Settings.Default.serialBaudRate = serialBaudCmBx.Text;
            Properties.Settings.Default.serialDataBits = (uint)serialDataBitsTxt.Value;
            Properties.Settings.Default.serialParity = serialParityCmBx.Text;
            Properties.Settings.Default.serialStopBits = serialStopBitsCmBx.Text;
            SaveProperties();
        }

        private void LoadSerialSettigns()
        {
            serialBaudCmBx.Text = Properties.Settings.Default.serialBaudRate;
            serialDataBitsTxt.Value = (decimal)Properties.Settings.Default.serialDataBits;
            serialParityCmBx.Text = Properties.Settings.Default.serialParity;
            serialStopBitsCmBx.Text = Properties.Settings.Default.serialStopBits;
        }

        private void SaveProperties()
        {
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void OnNewTcpClient(uint numOfClients)
        {
            Invoke((MethodInvoker)delegate
            {
                tcpConnectedClientsLbl.Text = numOfClients.ToString();
            });
        }

        private void OnTcpByteIn(byte[] bytes)
        {
            AppendBytesToTerminal(bytes);
        }

        private void tcpDisconnectBtn_Click(object sender, EventArgs e)
        {
            if (tcpServerRdBtn.Checked)
            {
                ConnectionsManager.Inst.TCPServer.Close();
                tcpClientRdBtn.Enabled = true;
            }
            else
            {
                ConnectionsManager.Inst.TCPClient.Kill();
                tcpServerRdBtn.Enabled = true;
            }

            tcpIndicatorLbl.BackColor = SystemColors.Control;
            tcpConnectBtn.Enabled = true;
            tcpConnectionStateLbl.Text = "Disconnected";
            tcpConnectionStateLbl.BackColor = SystemColors.Control; 
        }



        private void sendBtn_Click(object sender, EventArgs e)
        {
            if (textToSendCmBx.Text == "")
                return;

            if (ssh_ != null) /* terminal is in ssh mode (ssh connected) */
            {
                ssh_.RunCommand(textToSendCmBx.Text);
                if (textToSendCmBx.Text == "exit")
                {
                    clearScreenBtn.PerformClick();
                    WriteLnToTerminal("ssh session terminated");
                    ssh_ = null;
                }
                PassOutTxtToHistory();
                return;
            }

            /* if no communication is open, handle text as a command */
            /* line otherwise, send msg on opened communication line */
            if (!ConnectionsManager.Inst.IsSomeConnectionInitiated())
            {
                string [] cmdArgs = new string[5];
                string cmdAnswer = "";
                CommandParser.Type cmdType = CommandParser.Type.NONE;
                bool validCmd;
                validCmd = CommandParser.Parse(textToSendCmBx.Text, ref cmdAnswer, ref cmdType, ref cmdArgs);

                if (validCmd)
                {
                    switch (cmdType)
                    {
                        case CommandParser.Type.AUTO_SCROLL:
                            string flag = cmdArgs[AutoScrollArg.FLAG_INDX];
                            bool value = flag == "on" ? true : false;
                            autoScrollChkBx.Checked = value;
                            break;
                        case CommandParser.Type.RESET:
                            resetBtn.PerformClick();
                            break;
                        case CommandParser.Type.CLEAR:
                            clearScreenBtn.PerformClick();
                            break;
                        case CommandParser.Type.PING:
                            break;
                        case CommandParser.Type.SSH:
                            bool a = ConnectSsh(cmdArgs[SshArg.HOSTADDR_INDX],
                                                   cmdArgs[SshArg.USERNAME_INDX],
                                                   cmdArgs[SshArg.PASS_INDX],
                                                   ref cmdAnswer,
                                                   ref ssh_);
                            break;
                        case CommandParser.Type.NONE:
                            break;
                    }
                }
                WriteLnToTerminal(cmdAnswer);
            }
            else
            {
                var bytes = Encoding.ASCII.GetBytes(textToSendCmBx.Text);
                PassOutTxtToHistory();
                SendMsg(bytes);
            }
        }

        private void PassOutTxtToHistory()
        {
            if (textToSendCmBx.Items.Count > 10) //TODO: MAKE MAX HISTROY ITEMS A SETTING //////////////////////////////////
                textToSendCmBx.Items.RemoveAt(0);
            textToSendCmBx.Items.Add(textToSendCmBx.Text);
            textToSendCmBx.Text = "";
        }

       

        private void SendMsg(byte [] msg)
        {
          
    
            if (ConnectionsManager.Inst.IsTcpServerInitiated())
                ConnectionsManager.Inst.TCPServer.Send(msg);

            if (ConnectionsManager.Inst.IsTcpClientInitiated())
                ConnectionsManager.Inst.TCPClient.Send(msg);

            if (ConnectionsManager.Inst.IsUdpServerInitiated())
                ConnectionsManager.Inst.UDPServer.Send(msg);

            if (ConnectionsManager.Inst.IsUdpClientInitiated())
                ConnectionsManager.Inst.UDPClient.Send(msg);

            if (ConnectionsManager.Inst.IsSerialInitiated())
                ConnectionsManager.Inst.Serial.Send(msg);

            if (ConnectionsManager.Inst.IsSomeConnectionInitiated())
            {
                ConnectionsManager.Inst.BytesOutCounter.Add((uint)msg.Length);
                BytesCounter.MeasureUnit mUnit = ConnectionsManager.Inst.BytesOutCounter.RecomendedMeasureUnit();
                var format = "{0:0}";
                if (mUnit != BytesCounter.MeasureUnit.B)
                    format = "{0:0.00}";
                var processedCounter = String.Format(format, ConnectionsManager.Inst.BytesOutCounter.GetProcessedCounter(mUnit));

                bytesOutLbl.BackColor = Color.LimeGreen;
                bytesInTimer.Stop();
                bytesInTimer.Start();
                bytesOutTimer.Enabled = true;
                bytesOutLbl.Text = processedCounter + " " + BytesCounter.MeasureUnitToString(mUnit);
            }
        }

        private void clearScreenBtn_Click(object sender, EventArgs e)
        {
            dataInScreenTxt.Clear();
        }

        private void clearInHistoryBtn_Click(object sender, EventArgs e)
        {
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void serialConnectBtn_Click(object sender, EventArgs e)
        {
            SaveSerialSettings();
            string port = serialComCmBx.Text;
            if (port == "")
            {
                MsgBox.WarningMsg("Serial Connection Failed", "No port was selected");
                serialConnectionStateLbl.Text = "Failed";
                serialConnectionStateLbl.BackColor = Color.Tomato;
                return;
            }
            int baud = int.Parse(serialBaudCmBx.SelectedItem.ToString());
            StopBits stopBits = SerialCom.StringToStopBits(serialStopBitsCmBx.SelectedItem.ToString());
            int dataBits = (int)serialDataBitsTxt.Value;
            Parity parity = SerialCom.StringToParity(serialParityCmBx.SelectedItem.ToString());

            ConnectionsManager.Inst.InitSerial();
            if (ConnectionsManager.Inst.Serial.Connect(port, baud, parity, dataBits, stopBits))
            {
                serialConnectionStateLbl.Text = "Connected";
                serialConnectionStateLbl.BackColor = Color.LimeGreen;
                serialIndicatorLbl.BackColor = Color.LimeGreen;
                serialDisconnectBtn.Enabled = true;
                serialConnectBtn.Enabled = false;
                ConnectionsManager.Inst.Serial.Subscribe(OnSerialByteIn);
            }
        }

        private void serialDisconnectBtn_Click(object sender, EventArgs e)
        {
            var t = Task.Run(delegate
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                ConnectionsManager.Inst.Serial.CloseMe();
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            });
           
            serialDisconnectBtn.Enabled = false;
            serialConnectBtn.Enabled = true;
            serialConnectionStateLbl.Text = "Disconnected";
            serialConnectionStateLbl.BackColor = SystemColors.Control;
            serialIndicatorLbl.BackColor = SystemColors.Control;
        }

        private void OnSerialByteIn(byte[] bytes)
        {
            AppendBytesToTerminal(bytes);            
        }

        private void dataInHistoryLstBx_SelectedIndexChanged(object sender, EventArgs e)
        {
 
  
        }

        private void dataInHistoryLstBx_SizeChanged(object sender, EventArgs e)
        {

        }

        private void AppendBytesToTerminal(byte[] bytes)
        {

            ConnectionsManager.Inst.BytesInCounter.Add((uint)bytes.Length);
            BytesCounter.MeasureUnit mUnit = ConnectionsManager.Inst.BytesInCounter.RecomendedMeasureUnit();
            var format = "{0:0}";
            if (mUnit != BytesCounter.MeasureUnit.B)
                format = "{0:0.00}";
            var processedCounter = String.Format(format, ConnectionsManager.Inst.BytesInCounter.GetProcessedCounter(mUnit));

            Invoke((MethodInvoker)delegate
            {
                bytesInLbl.BackColor = Color.LimeGreen;
                bytesInTimer.Stop();
                bytesInTimer.Start();
                bytesInTimer.Enabled = true;
                
                bytesInLbl.Text = processedCounter + " " + BytesCounter.MeasureUnitToString(mUnit);

                string bytesString = System.Text.Encoding.UTF8.GetString(bytes);
                if (autoScrollChkBx.Checked)
                    dataInScreenTxt.AppendText(bytesString);
                else
                    dataInScreenTxt.Text += bytesString;
            });
        }

        private void WriteLnToTerminal(string txt)
        {
            Invoke((MethodInvoker)delegate
            {
                dataInScreenTxt.AppendText("> " + txt + "\n");
                PassOutTxtToHistory();
            });
        }

        private void tcpServerRdBtn_CheckedChanged(object sender, EventArgs e)
        {
            tcpIpTxt.Enabled = tcpClientRdBtn.Checked;
            tcpIpLbl.Enabled = tcpClientRdBtn.Checked;
        }

        private void textToSendCmBx_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.KeyChar == (char)Keys.Enter)
            //    sendBtn.PerformClick();
        }

        private void udpConnectBtn_Click(object sender, EventArgs e)
        {
            ConnectUdp();
        }

        private void ConnectUdp()
        {
            SaveUdpSettings();
            bool connected = false;
            if (udpServerRdBtn.Checked)
            {
                ConnectionsManager.Inst.InitUdpServer((int)udpPortTxt.Value);
                ConnectionsManager.Inst.UDPServer.Subscribe(OnUdpByteIn);
                udpClientRdBtn.Enabled = false;
                connected = true;
            }
            else
            {
                ConnectionsManager.Inst.InitUdpClient();
                if (ConnectionsManager.Inst.UDPClient.ConnectTo(udpIpTxt.Text, (int)udpPortTxt.Value))
                {
                    ConnectionsManager.Inst.UDPClient.Subscribe(OnUdpByteIn);
                    udpServerRdBtn.Enabled = false;
                    connected = true;
                }
                else
                {
                    MsgBox.WarningMsg("UDP Connection Failed", "Wrong IP or port address");
                }
            }
            if (connected)
            {
                udpConnectBtn.Enabled = false;
                udpDisconnectBtn.Enabled = true;
                udpConnectionStateLbl.Text = "Connected";
                udpConnectionStateLbl.BackColor = Color.LimeGreen;
                udpIndicatorLbl.BackColor = Color.LimeGreen;
            }
            else
            {
                udpConnectionStateLbl.Text = "Failed";
                udpConnectionStateLbl.BackColor = Color.DarkOrange;
            }
        }

        private void OnUdpByteIn(byte[] bytes)
        {
            AppendBytesToTerminal(bytes);
        }

        private void udpServerRdBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (udpServerRdBtn.Checked)
                udpIpTxt.Enabled = false;
            else
                udpIpTxt.Enabled = true;
        }

        private void aboutBtn_Click(object sender, EventArgs e)
        {
            if (aboutForm_ == null || aboutForm_.IsDisposed)
            {
                aboutForm_ = new AboutForm();
                aboutForm_.Show();
            }
            else
            {
                aboutForm_.Show();
                aboutForm_.Focus();
            }
        }

        private void udpDisconnectBtn_Click(object sender, EventArgs e)
        {
            if (udpServerRdBtn.Checked)
            {
                ConnectionsManager.Inst.UDPServer.Close();
                udpClientRdBtn.Enabled = true;
            }
            else
            {
                ConnectionsManager.Inst.UDPClient.Kill();
                udpServerRdBtn.Enabled = true;
            }

            udpIndicatorLbl.BackColor = SystemColors.Control;
            udpConnectBtn.Enabled = true;
            udpConnectionStateLbl.Text = "Disconnected";
        }

        private void bytesInTimer_Tick(object sender, EventArgs e)
        {
            bytesInLbl.BackColor = SystemColors.Control;
            bytesInTimer.Enabled = false;
        }

        private void bytesOutTimer_Tick(object sender, EventArgs e)
        {
            bytesOutLbl.BackColor = SystemColors.Control;
            bytesOutTimer.Enabled = false;
        }

        private void udpIpTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                ConnectUdp();
        }

        private void udpPortTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                ConnectUdp();
        }

        private void tcpIpTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                ConnectTcp();
        }

        private void tcpPortTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                ConnectTcp();
        }

        private void graphToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (graphFrom_ == null || graphFrom_.IsDisposed)
            {
                graphFrom_ = new GraphForm(ref dataInScreenTxt);
                graphFrom_.Show();
            }
            else
            {
                graphFrom_.Show();
                graphFrom_.Focus();
            }
        }

        private void bytesRateTimer_Tick(object sender, EventArgs e)
        {
            ulong newBytesCount = ConnectionsManager.Inst.BytesInCounter.GetRawCounter();
            ConnectionsManager.Inst.BytesRateCounter.SetCounter(newBytesCount - ConnectionsManager.Inst.PrevBytesCount);
            ConnectionsManager.Inst.PrevBytesCount = newBytesCount;

            BytesCounter.MeasureUnit mUnit = ConnectionsManager.Inst.BytesRateCounter.RecomendedMeasureUnit();
            var format = "{0:0}";
            if (mUnit != BytesCounter.MeasureUnit.B)
                format = "{0:0.00}";
            var processedCounter = String.Format(format, ConnectionsManager.Inst.BytesRateCounter.GetProcessedCounter(mUnit));
            Invoke((MethodInvoker)delegate
            {
                receivingRateLbl.Text = processedCounter + " " + BytesCounter.MeasureUnitToString(mUnit) + "/s";
            });
        }

        private void serialComRefreshBtn_Click(object sender, EventArgs e)
        {
            UpdateSerialPorts();
        }

        private void preferencesBtn_Click(object sender, EventArgs e)
        {
            if (preferencesForm_ == null || preferencesForm_.IsDisposed)
            {
                preferencesForm_ = new PreferencesForm();
                preferencesForm_.Show();
            }
            else
            {
                preferencesForm_.Show();
                preferencesForm_.Focus();
            }
        }

        private void resetBtn_Click(object sender, EventArgs e)
        {
            bytesInLbl.Text = "0 B";
            bytesOutLbl.Text = "0 B";
            ConnectionsManager.Inst.BytesInCounter.Reset();
            ConnectionsManager.Inst.BytesOutCounter.Reset();
            ConnectionsManager.Inst.PrevBytesCount = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
          
        }

        private bool ConnectSsh(string hostAddrs, string userName, string password, ref string reply, ref Ssh ssh)
        {
            ssh = new Ssh();
            ssh.Subscribe(OnIncomingSsh);
            if (ssh.Connect(hostAddrs, userName, password, ref reply))
            {
                ssh.CreateShellStream("terminal", 80, 24, 800, 600, 1024); //TODO: CHANGE THIS ACCORDING TO WINDOW SIZE, FOR LONG LINES PRINTING
                return true;
            }
            return false;
        }

        private void OnIncomingSsh(string msg)
        {
            Invoke((MethodInvoker)delegate
            {
                if (autoScrollChkBx.Checked)
                    dataInScreenTxt.AppendText(msg);
                else
                    dataInScreenTxt.Text += msg;
            });
        }
    }
}
