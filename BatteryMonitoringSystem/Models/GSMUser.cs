namespace BatteryMonitoringSystem.Models
{
    class GSMUser
    {
        private string phoneNumber;
        private string pin;

        public GSMUser(string phoneNumber, string pin)
        {
            PhoneNumber = phoneNumber;
            Pin = pin;
        }

        public string PhoneNumber { get => phoneNumber; set => phoneNumber = value; }
        public string Pin { get => pin; set => pin = value; }
    }
}
