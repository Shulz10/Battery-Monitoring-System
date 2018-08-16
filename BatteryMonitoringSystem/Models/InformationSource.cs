using SQLite.CodeFirst;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace BatteryMonitoringSystem.Models
{
    [Table("InformationSources")]
    public class InformationSource : INotifyPropertyChanged
    {        
        private int informationSourceID;
        private string phoneOperator;
        private string internationalCode;
        private string phoneNumber;
        private bool isEnable;

        [Key]
        [Autoincrement]
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
        public string Operator
        {
            get { return phoneOperator; }
            set
            {
                phoneOperator = value;
                OnPropertyChanged("Operator");
            }
        }

        [Required]
        public string InternationalCode
        {
            get { return internationalCode; }
            set
            {
                internationalCode = value;
                OnPropertyChanged("InternationalCode");
            }
        }

        [Unique]
        [Required]
        public string PhoneNumber
        {
            get { return phoneNumber; }
            set
            {
                phoneNumber = value;
                OnPropertyChanged("PhoneNumber");
            }
        }

        [Required]
        public bool IsEnable
        {
            get { return isEnable; }
            set
            {
                isEnable = value;
                OnPropertyChanged("IsEnable");
            }
        }

        public virtual ICollection<Information> Informations { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
