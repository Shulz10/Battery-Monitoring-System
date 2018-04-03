using System;

namespace BatteryMonitoringSystem.Models
{
    class SmsRequest
    {
        private int? startMessageIndex;
        private int? lastMessageIndex;
        private int receivedMessagesNumber;
        private CommandCode commandCode;

        public int? StartMessageIndex { get; }
        public int? LastMessageIndex { get; }
        public int ReceivedMessagesNumber { get; set; }
        public int? MessagesNumber {
            get
            {
                if (commandCode == CommandCode.LastMessage)
                    return 2;
                else if(LastMessageIndex != null && StartMessageIndex != null)
                    return (LastMessageIndex - StartMessageIndex + 1) * 2;
                return null;
            }
        }

        public CommandCode CommandCode { get; }

        public SmsRequest(int startMessageIndex, int lastMessageIndex, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex;
            this.lastMessageIndex = lastMessageIndex;
            this.commandCode = commandCode;
            receivedMessagesNumber = 0;
        }

        public SmsRequest(string startMessageIndex, string lastMessageIndex, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex == "" ? (int?)null : int.Parse(startMessageIndex);
            this.lastMessageIndex = lastMessageIndex == "" ? (int?)null : int.Parse(lastMessageIndex);
            this.commandCode = commandCode;
            receivedMessagesNumber = 0;
        }
    }
}
