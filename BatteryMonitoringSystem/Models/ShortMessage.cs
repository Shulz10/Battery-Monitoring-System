﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatteryMonitoringSystem.Models
{
    public class ShortMessage
    {
        #region Private Variable
        private string messageNumber;
        private string sender;
        private DateTime sentDateTime;
        private string message;
        #endregion

        #region Public Properties
        public string MessageNumber
        {
            get { return messageNumber; }
            set { messageNumber = value; }
        }

        public string Sender
        {
            get { return sender; }
            set { sender = value; }
        }

        public DateTime SentDateTime
        {
            get { return sentDateTime; }
            set { sentDateTime = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        #endregion

        #region Constructor
        public ShortMessage(string phoneNumber, string messageBody)
        {
            sender = phoneNumber;
            sentDateTime = new DateTime(2018, 1, 1);
            ParseShortMessageBody(messageBody);
        }
        #endregion
        
        public void ParseShortMessageBody(string messageBody)
        {
            messageNumber = int.Parse(messageBody.Substring(0, 8).TrimStart('0'), System.Globalization.NumberStyles.HexNumber).ToString();

            var t = TimeSpan.FromSeconds(int.Parse(messageBody.Substring(8, 8).TrimStart('0'), System.Globalization.NumberStyles.HexNumber));
            sentDateTime.AddDays(t.Days);
            sentDateTime.AddHours(t.Hours);
            sentDateTime.AddMinutes(t.Minutes);
            sentDateTime.AddSeconds(t.Seconds);

            string[] packages = new string[4];
            for (int i = 0; i < 4; i++)
                packages[i] = messageBody.Substring(15 + i * 24, 24);

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
    }
}
