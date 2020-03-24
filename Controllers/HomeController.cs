using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using dot.Models;

namespace dot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public string Calculate(string request)
        {
            var data = GetData(request);
            var response = new CalculationResponse();

            //validation
            response = Validation(data,response);

            //sum check
            response = RequestedSumCheck(data,response);

            //percent and payment calculation
            response = PercentAndPaymentCalculation(data,response);

            response = AfterwardsValidation(data,response);
            
            return Newtonsoft.Json.JsonConvert.SerializeObject(response);
        }

        private CalculationResponse Validation(Calculation request, CalculationResponse response) {
            //Age
            if(request.Age < 1) {
                response.SetError("Incorrect age");
                return response;
            }
            
            if(request.Age < 18) {
                response.SetError("You are too young");
                return response;
            }

            //Check pension status
            //Если возраст превышает пенсионный возраст на момент возврата кредита --> кредит не выдаётся
            var creditEndAge = request.Age + request.Term;
            if(request.Sex == Sex.Male){
                if(creditEndAge > request.PensionAgeMale) {
                    response.SetError("You'll be pensioner when credit is over");
                    return response;
                }
            }
            else {
                if(creditEndAge > request.PensionAgeFemale) {
                    response.SetError("You'll be pensioner when credit is over");
                    return response;
                }
            }

            //Check income
            //Если результат деления запрошенной суммы на срок погашения в годах более трети годового дохода --> кредит не выдаётся
            var annualPayment = request.RequestedSum / request.Term;
            if(annualPayment > request.Income/3) {
                response.SetError("You won't have enogh money to pay the bills");
                return response;
            }

            //Check credit rate
            //Если кредитный рейтинг -2 --> кредит не выдаётся
            if(request.CreditRate == -2) {
                response.SetError("Your credit rate is too low");
                return response;
            }

            //Check income type
            //Если в источнике дохода указано "безработный" --> кредит не выдаётся
            if(request.IncomeType == IncomeType.Unemployed) {
                response.SetError("Did you really expect credit approval? You're unemployed!");
                return response;
            }

            return response;
        }

        private CalculationResponse RequestedSumCheck(Calculation request, CalculationResponse response) {
            var possibleSum = request.RequestedSum;
            
            //При пассивном доходе выдаётся кредит на сумму до 1 млн, наёмным работникам - до 5 млн, собственное дело - до 10 млн
            if(request.IncomeType == IncomeType.Passive) {
                possibleSum = (possibleSum > request.MaxSumForPassiveIncome) ? request.MaxSumForPassiveIncome : possibleSum;
            }
            if(request.IncomeType == IncomeType.Employee) {
                possibleSum = (possibleSum > request.MaxSumForEmployeeIncome) ? request.MaxSumForEmployeeIncome : possibleSum;
            }
            if(request.IncomeType == IncomeType.Business) {
                possibleSum = (possibleSum > request.MaxSumForBusinessIncome) ? request.MaxSumForBusinessIncome : possibleSum;
            }

            //При кредитном рейтинге -1 выдаётся кредит на сумму до 1 млн, при 0 - до 5 млн, при 1 или 2 - до 10 млн
            if(request.CreditRate == -1) {
                possibleSum = (possibleSum > request.MaxSumForM1Rate) ? request.MaxSumForM1Rate : possibleSum;
            }
            if(request.CreditRate == 0) {
                possibleSum = (possibleSum > request.MaxSumFor0Rate) ? request.MaxSumFor0Rate : possibleSum;
            }
            if(request.CreditRate > 0) {
                possibleSum = (possibleSum > request.MaxSumFor1PRate) ? request.MaxSumFor1PRate : possibleSum;
            }

            if(possibleSum < request.RequestedSum) {
                response.SetError("Max sum for you: " + possibleSum + "M");
            }
            return response;
        }

        private CalculationResponse PercentAndPaymentCalculation(Calculation request, CalculationResponse response) {
            double modificator = 0;

            //-2% для ипотеки, -0.5% для развития бизнеса, +1.5% для потребительского кредита
            switch(request.Reason) {
                case Reason.Mortgage:
                    modificator -= 2;
                    break;
                case Reason.Business:
                    modificator -= 0.5;
                    break;
                case Reason.Personal:
                    modificator += 1.5;
                    break;
                default:
                    modificator += 0;
                    break;
            }

            //+1.5% для кредитного рейтинга -1, 0% для кредитного рейтинга 0, -0.25% для кредитного рейтинга 1, -0.75% для кредитного рейтинга 2
            switch(request.CreditRate) {
                case -1:
                    modificator += 1.5;
                    break;
                case 0:
                    modificator += 0;
                    break;
                case 1:
                    modificator -= 0.25;
                    break;
                case 2:
                    modificator -= 0.75;
                    break;
                default:
                    modificator += 0;
                    break;
            }

            //Модификатор в зависимости от запрошенной суммы рассчитывается по формуле [-log(sum)]; например, для 0.1 млн изменение ставки составит +1%, для 1 млн - 0%, для 10 млн изменение ставки составит -1%
            modificator += -Math.Log(request.RequestedSum);

            //Для пассивного дохода ставка повышается на 0.5%, для наемных работников ставка снижается на 0.25%, для заемщиков с собственным бизнесом ставка повышается на 0.25%
            switch(request.IncomeType) {
                case IncomeType.Passive:
                    modificator += 0.5;
                    break;
                case IncomeType.Employee:
                    modificator -= 0.25;
                    break;
                case IncomeType.Business:
                    modificator += 0.25;
                    break;
                default:
                    modificator += 0;
                    break;
            }

            //Годовой платеж по кредиту определяется по следующей формуле: 
            //(<сумма кредита> * (1 + <срок погашения> * (<базовая ставка> + <модификаторы>))) / <срок погашения>

            var payment = (request.RequestedSum * (1 + request.Term * (request.BasicPercent/100 + modificator/100))) / request.Term;
            payment = Math.Round(payment*1000)/1000;
            response.SetPayment(payment);
            return response;
        }

        private CalculationResponse AfterwardsValidation(Calculation request, CalculationResponse response) {
            //Check annual payment
            //Если годовой платёж (включая проценты) больше половины дохода --> кредит не выдаётся
            if(response.Payment > request.Income / 2) {
                response.SetError("Your annual payment is more than a half of your annual income. We're sorry about that.");
                return response;
            }
            return response;
        }

        private Calculation GetData(string request) {
            var requestObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Request>>(request);
            var data = new Dictionary<string,string>();
            foreach(var item in requestObject) {
                data.Add(item.name,item.value);
            }
            var rightJson = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<Calculation>(rightJson);
            return response;
        }
    }
}
