using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace WMS.Application.Services
{
    public class ToastService
    {
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ToastService(
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public void AddSuccessToast(string message)
        {
            AddToast("success", message);
        }

        public void AddErrorToast(string message)
        {
            AddToast("error", message);
        }

        public void AddWarningToast(string message)
        {
            AddToast("warning", message);
        }

        public void AddInfoToast(string message)
        {
            AddToast("info", message);
        }

        private void AddToast(string type, string message)
        {
            var tempData = _tempDataDictionaryFactory.GetTempData(_httpContextAccessor.HttpContext);
            tempData["Toast"] = $"{type}:{message}";
        }
    }
}