using System;

namespace ecom.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // New properties for detailed error info
        public string ErrorMessage { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionDetails { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }

        // Add a constructor for easy creation
        public ErrorViewModel() { }

        public ErrorViewModel(Exception ex)
        {
            if (ex != null)
            {
                ErrorMessage = ex.Message;
                ExceptionType = ex.GetType().Name;
                ExceptionDetails = ex.ToString();
                StackTrace = ex.StackTrace;

                if (ex.InnerException != null)
                {
                    InnerException = ex.InnerException.ToString();
                }
            }
        }
    }
}