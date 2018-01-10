using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace BatteryMonitoringSystem.Models
{
    [Table("Informations")]
    public class Information : INotifyPropertyChanged
    {
        //[Key]
        private int messageNumber;
        private int informationSourceID;
        private DateTime messageDateTime;
        private string message;
        private InformationSource informationSource;

        [Key]
        public int MessageNumber
        {
            get { return messageNumber; }
            set
            {
                messageNumber = value;
                OnPropertyChanged("MessageNumber");
            }
        }

        public int InformationSourceID
        {
            get { return informationSourceID; }
            set
            {
                informationSourceID = value;
                OnPropertyChanged("InformationSourceID");
            }
        }

        public DateTime MessageDateTime
        {
            get { return messageDateTime; }
            set
            {
                messageDateTime = value;
                OnPropertyChanged("MessageDateTime");
            }
        }

        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        public virtual InformationSource InformationSource
        {
            get { return informationSource; }
            set { informationSource = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
