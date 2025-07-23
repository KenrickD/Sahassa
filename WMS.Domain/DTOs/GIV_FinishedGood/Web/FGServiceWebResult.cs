using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FGServiceWebResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? ValidationDetails { get; set; }
        public List<string> Messages { get; set; } = new List<string>();

        public static FGServiceWebResult SuccessResult(string? message = null)
        {
            return new FGServiceWebResult
            {
                Success = true,
                ErrorMessage = message
            };
        }

        public static FGServiceWebResult ErrorResult(string errorMessage, object? validationDetails = null)
        {
            return new FGServiceWebResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ValidationDetails = validationDetails
            };
        }
    }
}
