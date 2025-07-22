namespace WMS.Domain.DTOs.Services
{
    public class ServiceResultDto
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static ServiceResultDto Success(string message = "")
        {
            return new ServiceResultDto { IsSuccess = true, ErrorMessage = message };
        }

        public static ServiceResultDto Failure(string errorMessage)
        {
            return new ServiceResultDto { IsSuccess = false, ErrorMessage = errorMessage };
        }

        public static ServiceResultDto Success(object data, string message = "")
        {
            return new ServiceResultDto { IsSuccess = true, Data = data, ErrorMessage = message };
        }
    }
}
