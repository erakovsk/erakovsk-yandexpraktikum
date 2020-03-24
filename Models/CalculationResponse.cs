namespace dot.Controllers
{
    public class CalculationResponse {
        public CalculationResponse() {
            Result = true;
            Payment = 0;
            Error = false;
            ErrorMessage = "Constructor message";
        }
        public bool Result { get; set; }
        public double Payment { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }

        public void SetError(string message) {
            this.Error = true;
            this.Result = false;
            this.ErrorMessage = message;
        }

        public void SetPayment(double value) {
            if(!this.Error) {
                this.Payment = value;
            }
        }
    }
}
