using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace BatteryMonitoringSystem.Models
{
    public class Port
    {
        private static Port comPort;
        private SerialPort customSerialPort;
        private AutoResetEvent receiveNow;

        public SerialPort CustomSerialPort { get => customSerialPort; }

        private Port() => customSerialPort = new SerialPort();

        internal static Port GetComPort()
        {
            if (comPort == null)
                comPort = new Port();
            return comPort;
        }

        //Open Com Port
        public bool OpenComPort(string portName, int baudRate)
        {
            receiveNow = new AutoResetEvent(false);

            try
            {
                if (CustomSerialPort.IsOpen) customSerialPort.Close();
                customSerialPort.BaudRate = baudRate;
                customSerialPort.DataBits = 8;

                customSerialPort.StopBits = StopBits.One;
                customSerialPort.Parity = Parity.None;
                customSerialPort.Handshake = Handshake.None;

                customSerialPort.ReadTimeout = 1000;
                customSerialPort.WriteTimeout = 500;

                customSerialPort.Encoding = Encoding.GetEncoding("windows-1251");
                customSerialPort.PortName = portName;

                customSerialPort.DataReceived += SerialPort_DataReceived;
                //customSerialPort.ErrorReceived += SerialPort_ErrorReceived;

                customSerialPort.Open();
            }
            catch (IOException er)
            {
                MessageBox.Show("COM port error " + er.Message, "Error");
                return false;
            }

            return true;
        }

        //Close Com Port
        public void CloseComPort()
        {
            try
            {
                customSerialPort.Close();
                customSerialPort.DataReceived -= SerialPort_DataReceived;
                customSerialPort = null;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //Execute AT Command
        public string ExecuteCommand(string command, int responseTimeout, string errorMessage)
        {
            try
            {
                customSerialPort.DiscardOutBuffer();
                customSerialPort.DiscardInBuffer();
                receiveNow.Reset();
                customSerialPort.Write(command + "\r\n");

                string input = ReadResponse(responseTimeout);
                if ((input.Length == 0) || (!input.EndsWith("\r\n> ") && !input.EndsWith("\r\nOK\r\n")))
                    throw new ApplicationException(errorMessage);
                return input;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Read modem response
        public string ReadResponse(int timeout)
        {
            string buffer = string.Empty;
            try
            {
                do
                {
                    if(receiveNow.WaitOne(timeout, false))
                    {
                        string t = customSerialPort.ReadExisting();
                        buffer += t;
                    }
                    else
                    {
                        if (buffer.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return buffer;
        }

        //Send Message
        public bool SendMessage(string phoneNumber, ref string PIN, string message)
        {
            bool isSend = false;
            string command = "";

            try
            {
                string receivedData = ExecuteCommand("AT", 300, "No phone connected.");                
                receivedData = ExecuteCommand("AT+CPIN?", 300, "");
                if (receivedData.Contains("OK"))
                {
                    if (PIN == "")
                    {
                        InputPINWindow inputPINWindow = new InputPINWindow { Owner = Application.Current.MainWindow };
                        if (inputPINWindow.ShowDialog() == true)
                            PIN = inputPINWindow.PIN.Text;
                        else return isSend;
                    }
                    command = "AT+CPIN=" + PIN + "\r";
                    receivedData = ExecuteCommand(command, 300, "Invalid PIN.");
                }
                receivedData = ExecuteCommand("AT+CMGF=1", 300, "Failed to set message format.");
                command = "AT+CMGS=\"" + phoneNumber + "\"";
                receivedData = ExecuteCommand(command, 300, "Failed to accept phone number.");
                command = message + char.ConvertFromUtf32(26);
                receivedData = ExecuteCommand(command, 3000, "Failed to send message.");
                if (receivedData.EndsWith("\r\nOK\r\n"))
                    isSend = true;
                else if (receivedData.Contains("ERROR"))
                    isSend = false;
                return isSend;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        //{

        //}

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                    receiveNow.Set();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Count SMS
        public int CountSMSMessages(ref string PIN)
        {
            int CountSMSMessages = 0;
            string command = "";
            try
            {
                string receivedData = ExecuteCommand("AT", 500, "No phone connected.");
                receivedData = ExecuteCommand("AT+CPIN?", 300, "");
                if (receivedData.Contains("OK"))
                {
                    if (PIN == "")
                    {
                        InputPINWindow inputPINWindow = new InputPINWindow { Owner = Application.Current.MainWindow };
                        if (inputPINWindow.ShowDialog() == true)                        
                            PIN = inputPINWindow.PIN.Text;                           
                        else return -1;
                    }
                    command = "AT+CPIN=" + PIN + "\r";
                    receivedData = ExecuteCommand(command, 300, "Invalid PIN.");
                }
                receivedData = ExecuteCommand("AT+CMGF=1", 500, "Failed to set message format.");
                //receivedData = ExecuteCommand("AT+CPMS=\"SM\",\"SM\",\"SM\"", 500, "");
                receivedData = ExecuteCommand("AT+CPMS?", 1000, "Failed to count SMS message.");
                if (receivedData.Length >= 45 && receivedData.StartsWith("AT+CPMS?"))
                    CountSMSMessages = Convert.ToInt32(receivedData.Split(',')[1]);
                else if (receivedData.Contains("ERROR"))
                    return -1;

                return CountSMSMessages;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //Read SMS
        public List<ShortMessage> ReadSMS(ref string PIN)
        {
            string command = "";
            List<ShortMessage> messages = null;
            try
            {
                string receivedData = ExecuteCommand("AT", 300, "No phone connected.");
                receivedData = ExecuteCommand("AT+CPIN?", 300, "");
                if (receivedData.Contains("OK"))
                {
                    if (PIN == "")
                    {
                        InputPINWindow inputPINWindow = new InputPINWindow { Owner = Application.Current.MainWindow };
                        if (inputPINWindow.ShowDialog() == true)
                            PIN = inputPINWindow.PIN.Text;
                        else return null;
                    }
                    command = "AT+CPIN=" + PIN + "\r";
                    receivedData = ExecuteCommand(command, 300, "Invalid PIN.");
                }
                receivedData = ExecuteCommand("AT+CMGF=1", 300, "Failed to set message format.");
                receivedData = ExecuteCommand("AT+CPMS=\"SM\",\"SM\",\"SM\"", 500, "");
                receivedData = ExecuteCommand("AT+CSCS=\"PCCP936\"", 300, "Failed to set character set.");
                receivedData = ExecuteCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");
                string input = ExecuteCommand("AT+CMGL=\"REC UNREAD\"", 5000, "Failed to read the messages.");

                #region Parse Messages
                messages = ParseMessages(input);
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (messages != null)
                return messages;
            else
                return null;
        }

        //Parse Messages
        public List<ShortMessage> ParseMessages(string input)
        {
            List<ShortMessage> messages = new List<ShortMessage>();
            try
            {
                Regex regex = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                Match match = regex.Match(input);
                while (match.Success)
                {
                    messages.Add(new ShortMessage() {
                        Index = match.Groups[1].Value,
                        Status = match.Groups[2].Value,
                        Sender = match.Groups[3].Value,
                        Alphabet = match.Groups[4].Value,
                        Sent = match.Groups[5].Value,
                        Message = match.Groups[6].Value
                    });

                    match = match.NextMatch();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return messages;
        }
    }
}
