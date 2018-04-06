using System;

namespace BatteryMonitoringSystem.Models
{
    class SmsRequest
    {
        private int? startMessageIndex;
        private int? lastMessageIndex;
        private int receivedMessagesNumber;
        private CommandCode commandCode;
        private DateTime requestDateTime;

        public int? StartMessageIndex { get { return startMessageIndex; } }
        public int? LastMessageIndex { get { return lastMessageIndex; } }
        public int ReceivedMessagesNumber { get { return receivedMessagesNumber; } set { receivedMessagesNumber = value; } }
        public int? MessagesNumber
        {
            get
            {
                if (commandCode == CommandCode.LastMessage)
                    return 2;
                else if (LastMessageIndex != null && StartMessageIndex != null)
                    return (LastMessageIndex - StartMessageIndex + 1) * 2;
                else return null;
            }
        }

        public CommandCode CommandCode { get { return commandCode; } }
        public DateTime RequestDateTime
        {
            get { return requestDateTime; }
            set { requestDateTime = value; }
        }
        public TimeSpan DiffRequestTime
        {
            get { return DateTime.Now.Subtract(requestDateTime); }
        }

        public SmsRequest(int startMessageIndex, int lastMessageIndex, DateTime requestDateTime, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex;
            this.lastMessageIndex = lastMessageIndex;
            this.requestDateTime = requestDateTime;
            this.commandCode = commandCode;
            receivedMessagesNumber = 0;
        }

        public SmsRequest(string startMessageIndex, string lastMessageIndex, DateTime requestDateTime, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex == "" ? (int?)null : int.Parse(startMessageIndex);
            this.lastMessageIndex = lastMessageIndex == "" ? (int?)null : int.Parse(lastMessageIndex);
            this.requestDateTime = requestDateTime;
            this.commandCode = commandCode;
            receivedMessagesNumber = 0;
        }
    }
}
