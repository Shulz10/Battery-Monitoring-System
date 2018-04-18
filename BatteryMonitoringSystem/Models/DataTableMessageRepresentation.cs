using System;

namespace BatteryMonitoringSystem.Models
{
    public class DataTableMessageRepresentation
    {
        public double MessageNumber { get; set; }
        public string Sender { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedTime { get; set; }
        public string Message { get; set; } 

        public DataTableMessageRepresentation(double messageNumber, string sender, DateTime receivedDate, string receivedTime, string message)
        {
            MessageNumber = messageNumber;
            Sender = sender;
            ReceivedDate = receivedDate;
            ReceivedTime = receivedTime;
            Message = message;
        }
    }
}
