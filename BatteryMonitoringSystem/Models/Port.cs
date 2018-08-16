using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        public void OpenComPort()
        {
            receiveNow = new AutoResetEvent(false);

            try
            {
                if (CustomSerialPort.IsOpen) customSerialPort.Close();
                customSerialPort.BaudRate = 115200;
                customSerialPort.DataBits = 8;

                customSerialPort.StopBits = StopBits.One;
                customSerialPort.Parity = Parity.None;
                customSerialPort.Handshake = Handshake.None;

                customSerialPort.ReadTimeout = 1000;
                customSerialPort.WriteTimeout = 500;

                customSerialPort.Encoding = Encoding.GetEncoding("windows-1251");

                customSerialPort.DataReceived += SerialPort_DataReceived;

                EstablishConnectionToComPort();               
            }
            catch(Exception error)
            {
                throw error;
            }
        }

        private void EstablishConnectionToComPort()
        {
            List<string> availableComPorts = new List<string>();
            foreach (string name in SerialPort.GetPortNames())
                availableComPorts.Add(name);
            try
            {
                if (availableComPorts.Count != 0)
                {
                    foreach (var name in availableComPorts)
                    {
                        try
                        {
                            if (!customSerialPort.IsOpen)
                            {
                                customSerialPort.PortName = name;
                                customSerialPort.Open();
                                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                                if (log.Contains("OK"))
                                    return;
                               else customSerialPort.Close();
                            }
                        }
                        catch {
                            customSerialPort.Close();
                            continue;
                        }
                    }
                    throw new ApplicationException("GSM модем не подключен.");
                }
                else
                    throw new ApplicationException("Отсутствуют соединения по COM-портам.");
            }
            catch(Exception ex)
            {
                if(customSerialPort.IsOpen)
                    CloseComPort();
                throw ex;
            }
        }

        //Close Com Port
        public void CloseComPort()
        {
            try
            {
                customSerialPort.Close();
                customSerialPort.DataReceived -= SerialPort_DataReceived;
                comPort = null;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

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

        //Execute AT Command
        public void ExecuteCommand(string command, int responseTimeout, string errorMessage, out string log)
        {
            try
            {
                customSerialPort.DiscardOutBuffer();
                customSerialPort.DiscardInBuffer();
                receiveNow.Reset();
                customSerialPort.Write(command + "\r\n");
                log = DateTime.Now.ToString(new CultureInfo("ru-RU")) + "     " + command + Environment.NewLine;

                string input = ReadResponse(responseTimeout);
                log += DateTime.Now.ToString(new CultureInfo("ru-RU")) + "     " + input + Environment.NewLine;
                if ((input.Length == 0) || (!input.EndsWith("\r\n> ") && !input.Contains("\r\nOK\r\n")))
                    throw new ApplicationException(errorMessage);
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
                        if (buffer.Length == 0)
                            throw new ApplicationException("Данные с модема не были получены.");
                        return buffer;
                    }
                }
                while (!buffer.Contains("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return buffer;
        }

        //Enter Sim card pin code
        public void EnterSimCardPin(ref string PIN)
        {
            try
            {
                ExecuteCommand("AT+CPIN?", 300, "SIM карта не вставлена.", out string log);
                if (PIN == "")
                {
                    InputPINWindow inputPINWindow = new InputPINWindow { Owner = Application.Current.MainWindow };
                    if (inputPINWindow.ShowDialog() == true)
                        PIN = inputPINWindow.PIN.Text;
                    else
                        throw new ApplicationException("PIN код не введен! Отправка запроса отменена.");
                }
                ExecuteCommand($"AT+CPIN={PIN}\r", 300, "Неверный PIN.", out log);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //Send Message
        public void SendMessage(string phoneNumber, string message)
        {
            string log = "", saveLog = "";
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out log);
                ExecuteCommand("AT+CMGF=1", 500, "Не удалось установить формат сообщения.", out log);
                ExecuteCommand($"AT+CMGS=\"{phoneNumber}\"", 500, "Не удалось принять номер телефона.", out log);
                saveLog = log;
                ExecuteCommand(message + char.ConvertFromUtf32(26), 5000, "Не удалось отправить сообщение.", out log);
                log = log.Insert(0, saveLog);

                Task.Run(() => SaveCommunicationWithModemToLog(log));
            }
            catch(Exception ex)
            {
                log = log.Insert(0, saveLog);
                Task.Run(() => SaveCommunicationWithModemToLog(log + "     " + ex.Message + Environment.NewLine));
                throw ex;
            }
        }

        //Count SMS
        public void GetCountSMSMessagesInStorage(out int currentMessageCountInStorage, out int maxMessageCountInStorage)
        {
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                ExecuteCommand("AT+CMGF=1", 500, "Не удалось установить формат сообщения.", out log);
                ExecuteCommand("AT+CPMS=\"ME\",\"ME\",\"ME\"", 500, "Не удалось выбрать место хранения сообщений.", out log);
                ExecuteCommand("AT+CPMS?", 500, "Не удалось подсчитать SMS-сообщения.", out log);
                string[] parsedReceivedData = log.Split(',');
                currentMessageCountInStorage = Convert.ToInt32(parsedReceivedData[1]);
                maxMessageCountInStorage = Convert.ToInt32(parsedReceivedData[2]);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void GetCountSMSMessagesInStorage(out int currentMessageCountInStorage)
        {
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                ExecuteCommand("AT+CMGF=1", 500, "Не удалось установить формат сообщения.", out log);
                ExecuteCommand("AT+CPMS=\"ME\",\"ME\",\"ME\"", 500, "Не удалось выбрать место хранения сообщений.", out log);
                ExecuteCommand("AT+CPMS?", 500, "Не удалось подсчитать SMS-сообщения.", out log);
                string[] parsedReceivedData = log.Split(',');
                currentMessageCountInStorage = Convert.ToInt32(parsedReceivedData[1]);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Read SMS
        public List<ShortMessage> ReadSMS()
        {
            List<ShortMessage> messages = null;
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                ExecuteCommand("AT+CMGF=1", 300, "Не удалось установить формат сообщения.", out log);
                ExecuteCommand("AT+CPMS=\"ME\",\"ME\",\"ME\"", 500, "Не удалось выбрать место хранения сообщений.", out log);
                ExecuteCommand("AT+CSCS=\"PCCP936\"", 300, "Не удалось установить набор символов.", out log);
                ExecuteCommand("AT+CPMS=\"ME\"", 300, "Не удалось выбрать хранилище сообщений.", out log);
                ExecuteCommand("AT+CMGL=\"ALL\"", 30000, "Не удалось прочитать сообщения.", out log);

                Task.Run(() => SaveCommunicationWithModemToLog(log));

                #region Parse Messages
                messages = ParseMessages(log);
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

        //Clear SMS storage
        public void ClearMessageStorage()
        {
            try
            {
                ExecuteCommand($"AT+CMGD=1,4", 500, "Не удалось очистить хранилище сообщений.", out string log);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //Remove SMS by number
        public void RemoveMessagesByNumber(List<ShortMessage> listReadMessages)
        {
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                ExecuteCommand("AT+CMGF=1", 500, "Не удалось установить формат сообщения.", out log);
                for (int i = 0; i < listReadMessages.Count; i++)
                    ExecuteCommand($"AT+CMGD={listReadMessages[i].MessageNumberInModemStorage},0", 300, "Не удалось удалить сообщение из хранилища GSM-модема.", out log);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void RemoveMessagesByNumber(ShortMessage message)
        {
            try
            {
                ExecuteCommand("AT", 300, "GSM модем не подключен.", out string log);
                ExecuteCommand("AT+CMGF=1", 500, "Не удалось установить формат сообщения.", out log);
                ExecuteCommand($"AT+CMGD={message.MessageNumberInModemStorage},0", 300, "Не удалось удалить сообщение из хранилища GSM-модема.", out log);
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                    messages.Add(new ShortMessage(messageNumberInModemStorage: match.Groups[1].Value ,phoneNumber: match.Groups[3].Value, messageBody: match.Groups[6].Value));
                    match = match.NextMatch();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return messages;
        }

        //Write Log file
        public void SaveCommunicationWithModemToLog(string log)
        {
            FileStream fStream = null;
            try
            {
                using (fStream = new FileStream("ModemResponse.log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.Asynchronous))
                {
                    Regex regex = new Regex("(\r\n){2,}");
                    byte[] array = Encoding.Default.GetBytes(regex.Replace(log, "\r\n") + Environment.NewLine);
                    fStream.Write(array, 0, array.Length);
                    fStream.Close();
                }
            }
            finally
            {
                if (fStream != null)
                    fStream.Dispose();
            }
        }
    }
}
