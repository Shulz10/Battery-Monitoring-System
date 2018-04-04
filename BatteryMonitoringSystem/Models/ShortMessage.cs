using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatteryMonitoringSystem.Models
{
    public class ShortMessage
    {
        #region Private Variable
        private int messageNumberInModemStorage;
        private double messageNumber;
        private string sender;
        private DateTime receivedDateTime;
        private string message;
        #endregion

        #region Public Properties
        public int MessageNumberInModemStorage
        {
            get { return messageNumberInModemStorage; }
            set { messageNumberInModemStorage = value; }
        }

        public double MessageNumber
        {
            get { return messageNumber; }
            set { messageNumber = value; }
        }

        public string Sender
        {
            get { return sender; }
            set { sender = value; }
        }

        public DateTime ReceivedDateTime
        {
            get { return receivedDateTime; }
            set { receivedDateTime = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        #endregion

        #region Constructor
        public ShortMessage(){ }

        public ShortMessage(string phoneNumber, string messageBody)
        {
            sender = phoneNumber;
            receivedDateTime = new DateTime(2018, 1, 1);
            ParseShortMessageBody(messageBody);
        }

        public ShortMessage(string messageNumberInModemStorage, string phoneNumber, string messageBody)
        {
            this.messageNumberInModemStorage = int.Parse(messageNumberInModemStorage);
            sender = phoneNumber;
            receivedDateTime = new DateTime(2018, 1, 1);
            ParseShortMessageBody(messageBody);
        }
        #endregion
        
        private void ParseShortMessageBody(string messageBody)
        {
            messageNumber = int.Parse(messageBody.Substring(0, 8).TrimStart('0'), System.Globalization.NumberStyles.HexNumber);
            messageNumber += messageBody[8] == 0 ? 0.1 : 0.2;

            var t = TimeSpan.FromSeconds(int.Parse(messageBody.Substring(9, 8).TrimStart('0'), System.Globalization.NumberStyles.HexNumber));
            receivedDateTime = receivedDateTime.AddDays(t.Days);
            receivedDateTime = receivedDateTime.AddHours(t.Hours);
            receivedDateTime = receivedDateTime.AddMinutes(t.Minutes);
            receivedDateTime = receivedDateTime.AddSeconds(t.Seconds);

            string[] packages = new string[4];
            for (int i = 0; i < 4; i++)
                packages[i] = messageBody.Substring(16 + i * 24, 24);

            for (int i = 0; i < packages.Length; i++)
            {
                string packageId = "0x" + packages[i].Substring(0, 8).TrimStart('0');
                string data = packages[i].Substring(8);

                var bytes = (from Match m in Regex.Matches(data, ".{2}")
                             select m.Value).ToList();

                message += packageId;
                foreach (var b in bytes)
                    message += "  0x" + b;
                if(i+1 != packages.Length) message += "\r\n";
            }
        }

        public override string ToString()
        {
            return "Сообщение №" + MessageNumber + " получено от источника информации " + Sender + " в " + ReceivedDateTime + "\r\n" + Message;
        }
    }
}
