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
    }
}
