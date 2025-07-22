using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WMS.WebApp.Validations
{
    /// <summary>
    /// Validates that the input doesn't contain potential SQL injection patterns
    /// </summary>
    public class NoSqlInjectionAttribute : ValidationAttribute
    {
        private readonly string[] _sqlKeywords = {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
            "EXEC", "EXECUTE", "UNION", "SCRIPT", "JAVASCRIPT", "VBSCRIPT",
            "ONLOAD", "ONERROR", "ONCLICK", "ALERT", "CONFIRM", "PROMPT"
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var input = value.ToString().ToUpper();

            foreach (var keyword in _sqlKeywords)
            {
                if (input.Contains(keyword))
                {
                    return new ValidationResult($"Input contains potentially dangerous content: {keyword}");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that the input doesn't contain XSS patterns
    /// </summary>
    public class NoXssAttribute : ValidationAttribute
    {
        private readonly Regex[] _xssPatterns = {
            new(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase),
            new(@"javascript:", RegexOptions.IgnoreCase),
            new(@"on\w+\s*=", RegexOptions.IgnoreCase),
            new(@"<iframe\b", RegexOptions.IgnoreCase),
            new(@"<object\b", RegexOptions.IgnoreCase),
            new(@"<embed\b", RegexOptions.IgnoreCase),
            new(@"<form\b", RegexOptions.IgnoreCase)
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var input = value.ToString();

            foreach (var pattern in _xssPatterns)
            {
                if (pattern.IsMatch(input))
                {
                    return new ValidationResult("Input contains potentially dangerous HTML/JavaScript content");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates file uploads for security
    /// </summary>
    public class SecureFileUploadAttribute : ValidationAttribute
    {
        private readonly string[] _allowedExtensions;
        private readonly long _maxFileSize;
        private readonly string[] _dangerousExtensions = {
            ".exe", ".bat", ".com", ".cmd", ".scr", ".pif", ".vbs", ".js", ".jar",
            ".asp", ".aspx", ".php", ".jsp", ".cgi", ".pl", ".py", ".rb", ".sh"
        };

        public SecureFileUploadAttribute(string allowedExtensions = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx", long maxFileSizeInMB = 5)
        {
            _allowedExtensions = allowedExtensions.Split(',').Select(ext => ext.Trim().ToLower()).ToArray();
            _maxFileSize = maxFileSizeInMB * 1024 * 1024; // Convert MB to bytes
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            // Check file size
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(extension))
            {
                return new ValidationResult("File must have an extension");
            }

            if (!_allowedExtensions.Contains(extension))
            {
                return new ValidationResult($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            if (_dangerousExtensions.Contains(extension))
            {
                return new ValidationResult($"File type '{extension}' is not permitted for security reasons");
            }

            // Check for double extensions (e.g., file.jpg.exe)
            var fileName = file.FileName.ToLower();
            if (_dangerousExtensions.Any(dangerousExt => fileName.Contains(dangerousExt)))
            {
                return new ValidationResult("File contains potentially dangerous extension patterns");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates warehouse/client codes for consistent format
    /// </summary>
    public class SecureCodeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var code = value.ToString();

            // Only allow alphanumeric characters, hyphens, and underscores
            var codePattern = new Regex(@"^[A-Za-z0-9_-]+$");
            if (!codePattern.IsMatch(code))
            {
                return new ValidationResult("Code can only contain letters, numbers, hyphens, and underscores");
            }

            // Prevent codes that might be used for directory traversal
            if (code.Contains("..") || code.Contains("//") || code.Contains("\\"))
            {
                return new ValidationResult("Code contains invalid character sequences");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates email addresses with additional security checks
    /// </summary>
    public class SecureEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var email = value.ToString();

            // Basic email format validation
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailPattern.IsMatch(email))
            {
                return new ValidationResult("Invalid email format");
            }

            // Check for suspicious patterns
            if (email.Contains("..") || email.StartsWith(".") || email.EndsWith("."))
            {
                return new ValidationResult("Email contains invalid character patterns");
            }

            // Prevent extremely long emails (potential DoS)
            if (email.Length > 254)
            {
                return new ValidationResult("Email address is too long");
            }

            return ValidationResult.Success;
        }
    }
}