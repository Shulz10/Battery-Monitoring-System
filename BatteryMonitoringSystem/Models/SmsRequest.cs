using System;

namespace BatteryMonitoringSystem.Models
{
    class SmsRequest
    {
        private int? startMessageIndex;
        private int? lastMessageIndex;
        private CommandCode commandCode;

        public int StartMessageIndex { get; }
        public int LastMessageIndex { get; }
        public int MessagesNumber {
            get
            {
                if (commandCode == CommandCode.LastMessage)
                    return 1;
                else return LastMessageIndex - StartMessageIndex + 1;
            }
        }

        public CommandCode CommandCode { get; }

        public SmsRequest(int startMessageIndex, int lastMessageIndex, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex;
            this.lastMessageIndex = lastMessageIndex;
            this.commandCode = commandCode;
        }

        public SmsRequest(string startMessageIndex, string lastMessageIndex, CommandCode commandCode)
        {
            this.startMessageIndex = startMessageIndex == "" ? (int?)null : int.Parse(startMessageIndex);
            this.lastMessageIndex = lastMessageIndex == "" ? (int?)null : int.Parse(lastMessageIndex);
            this.commandCode = commandCode;
        }
    }
}
