using System.ComponentModel.DataAnnotations.Schema;

namespace dot.Controllers
{

    public enum Sex {
        Male,
        Female
    }

    public enum IncomeType {
        Passive,
        Employee,
        Business,
        Unemployed
    }

    public enum Reason {
        Mortgage,
        Business,
        Vehicle,
        Personal
    }

    public class Calculation {
        public Calculation() {
            PensionAgeMale = 60;
            PensionAgeFemale = 55;
            MaxSumForPassiveIncome = 1;
            MaxSumForEmployeeIncome = 5;
            MaxSumForBusinessIncome = 10;
            MaxSumForM1Rate = 1;
            MaxSumFor0Rate = 5;
            MaxSumFor1PRate = 10;
            BasicPercent = 10;
        }
        
        [NotMapped]
        public int PensionAgeMale { get; set;}
        [NotMapped]
        public int PensionAgeFemale { get; set;}
        [NotMapped]
        public double MaxSumForPassiveIncome { get; set;}
        [NotMapped]
        public double MaxSumForEmployeeIncome { get; set;}
        [NotMapped]
        public double MaxSumForBusinessIncome { get; set;}

        //Credit Rate == -1 (minus 1)
        [NotMapped]
        public double MaxSumForM1Rate { get; set;}

        //Credit Rate == 0
        [NotMapped]
        public double MaxSumFor0Rate { get; set;}

        //Credit Rate >= 1 (1+ / 1 plus)
        [NotMapped]
        public double MaxSumFor1PRate { get; set;}
        [NotMapped]
        public double BasicPercent { get; set;}

        public int Age { get; set; }
        public Sex Sex { get; set; }
        public IncomeType IncomeType { get; set; }
        public double Income { get; set; }
        public int CreditRate { get; set; }
        public double RequestedSum { get; set; }
        public int Term { get; set; }
        public Reason Reason { get; set; }
        public CalculationResponse Response { get; set; }
    }
}
