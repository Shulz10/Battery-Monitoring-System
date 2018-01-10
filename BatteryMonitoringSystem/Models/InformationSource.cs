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

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InformationSourceID
        {
            get { return informationSourceID; }
            set
            {
                informationSourceID = value;
                OnPropertyChanged("InformationSourceID");
            }
        }

        public string Operator
        {
            get { return phoneOperator; }
            set
            {
                phoneOperator = value;
                OnPropertyChanged("Operator");
            }
        }

        public string InternationalCode
        {
            get { return internationalCode; }
            set
            {
                internationalCode = value;
                OnPropertyChanged("InternationalCode");
            }
        }

        public string PhoneNumber
        {
            get { return phoneNumber; }
            set
            {
                phoneNumber = value;
                OnPropertyChanged("PhoneNumber");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
