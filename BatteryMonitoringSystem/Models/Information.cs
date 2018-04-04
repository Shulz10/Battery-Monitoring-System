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
        private int Id;
        private double messageNumber;
        private int informationSourceID;
        private DateTime messageDateTime;
        private string message;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID
        {
            get { return Id; }
            set
            {
                Id = value;
                OnPropertyChanged("InformationId");
            }
        }

        [Required]
        public double MessageNumber
        {
            get { return messageNumber; }
            set
            {
                messageNumber = value;
                OnPropertyChanged("MessageNumber");
            }
        }

        [Required]
        public int InformationSourceID
        {
            get { return informationSourceID; }
            set
            {
                informationSourceID = value;
                OnPropertyChanged("InformationSourceID");
            }
        }

        [Required]
        public DateTime MessageDateTime
        {
            get { return messageDateTime; }
            set
            {
                messageDateTime = value;
                OnPropertyChanged("MessageDateTime");
            }
        }

        [Required]
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        public virtual InformationSource InformationSource { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
