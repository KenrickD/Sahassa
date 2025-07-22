//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Net.Sockets;
//using System.Text.Json;
//using WMS.Infrastructure.Data;

//namespace WMS.WebApp.Controllers
//{
//    [Route("test")]
//    public class TestErrorController : Controller
//    {
//        private readonly ILogger<TestErrorController> _logger;
//        private readonly AppDbContext _context;
//        public TestErrorController(ILogger<TestErrorController> logger, AppDbContext context)
//        {
//            _logger = logger;
//            _context = context;
//        }
//        [Route("unhandled-exception")]
//        public IActionResult UnhandledException()
//        {
//            // Simulate real unhandled exception
//            throw new InvalidOperationException("Test unhandled exception - this simulates a real application error");
//        }

//        [Route("database-error")]
//        public async Task<IActionResult> DatabaseError()
//        {
//            // Simulate database connection/timeout issues
//            _context.Database.SetCommandTimeout(1); // 1 second timeout
//            await _context.Database.ExecuteSqlRawAsync("SELECT pg_sleep(5)");
//            return View();
//        }

//        [Route("memory-pressure")]
//        public IActionResult MemoryPressure()
//        {
//            // Simulate out of memory scenario
//            var largeArrays = new List<byte[]>();
//            for (int i = 0; i < 1000; i++)
//            {
//                largeArrays.Add(new byte[10 * 1024 * 1024]); // 10MB each
//            }
//            return Json(largeArrays.Count);
//        }

//        [Route("null-reference")]
//        public IActionResult NullReference()
//        {
//            // Simulate common null reference exception
//            string nullString = null;
//            return Json(nullString.Length); // Will throw NullReferenceException
//        }

//        [Route("model-binding-error")]
//        public IActionResult ModelBindingError([FromBody] ComplexModel model)
//        {
//            // This will cause model binding issues if called incorrectly
//            return Json(model.RequiredProperty.ToUpper());
//        }

//        [Route("authorization-error")]
//        [Authorize(Roles = "NonExistentRole")]
//        public IActionResult AuthorizationError()
//        {
//            return Json("This should not be accessible");
//        }

//        [Route("custom-error/{statusCode}")]
//        public IActionResult CustomError(int statusCode)
//        {
//            Response.StatusCode = statusCode;
//            if (statusCode == 404)
//                throw new FileNotFoundException("Test 404 error");
//            if (statusCode == 403)
//                throw new UnauthorizedAccessException("Test 403 error");

//            throw new Exception($"Test {statusCode} error");
//        }

//        [Route("view-error")]
//        public IActionResult ViewError()
//        {
//            // Simulate view compilation error
//            ViewBag.NullObject = null;
//            return View("NonExistentView");
//        }

//        [Route("dependency-injection-error")]
//        public IActionResult DependencyInjectionError([FromServices] INonExistentService2 service)
//        {
//            // This will cause DI resolution error
//            return Json(service.GetData());
//        }

//        [Route("logging-error")]
//        public IActionResult LoggingError()
//        {
//            // Test what happens when logging itself fails
//            var problematicObject = new { CircularRef = new object() };
//            _logger.LogError("This might cause logging issues: {@Object}", problematicObject);
//            return Json("Logged successfully");
//        }

//        [Route("async-error")]
//        public async Task<IActionResult> AsyncError()
//        {
//            // Simulate async operation failure
//            await Task.Delay(100);
//            throw new TaskCanceledException("Async operation was cancelled");
//        }

//        [Route("file-not-found")]
//        public IActionResult FileNotFound()
//        {
//            // Simulate file access error
//            var content = System.IO.File.ReadAllText("nonexistent-file.txt");
//            return Json(content);
//        }
//        // ========== EXISTING ERRORS ==========
//        [HttpGet("500")]
//        public IActionResult Test500Error()
//        {
//            throw new Exception("This is a test 500 error to simulate server issues");
//        }

//        [HttpGet("404")]
//        public IActionResult Test404Error()
//        {
//            return NotFound();
//        }

//        [HttpGet("timeout")]
//        public async Task<IActionResult> TestTimeoutError()
//        {
//            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
//            await Task.Delay(5000, cts.Token);
//            return Ok();
//        }

//        // ========== ADDITIONAL CRITICAL ERROR SCENARIOS ==========

//        // Database/Connection Errors
//        [HttpGet("database-timeout")]
//        public IActionResult TestDatabaseTimeout()
//        {
//            throw new TimeoutException("Database operation timed out after 30 seconds");
//        }

//        [HttpGet("database-connection")]
//        public IActionResult TestDatabaseConnection()
//        {
//            throw new InvalidOperationException("Unable to connect to the database. Connection string may be invalid.");
//        }

//        [HttpGet("deadlock")]
//        public IActionResult TestDeadlock()
//        {
//            throw new InvalidOperationException("Transaction (Process ID 52) was deadlocked on lock resources with another process and has been chosen as the deadlock victim.");
//        }

//        // Memory and Resource Issues
//        [HttpGet("out-of-memory")]
//        public IActionResult TestOutOfMemory()
//        {
//            // Simulate memory pressure
//            try
//            {
//                var arrays = new List<byte[]>();
//                for (int i = 0; i < 1000; i++)
//                {
//                    arrays.Add(new byte[10_000_000]); // 10MB each
//                }
//                return Ok();
//            }
//            catch (OutOfMemoryException)
//            {
//                throw new OutOfMemoryException("Insufficient memory to continue the execution of the program.");
//            }
//        }

//        [HttpGet("stackoverflow")]
//        public IActionResult TestStackOverflow()
//        {
//            return TestStackOverflow(); // Infinite recursion
//        }

//        // File System Errors
//        [HttpGet("file-not-found")]
//        public IActionResult TestFileNotFound()
//        {
//            var content = System.IO.File.ReadAllText("nonexistent-file.txt");
//            return Ok(content);
//        }

//        [HttpGet("directory-not-found")]
//        public IActionResult TestDirectoryNotFound()
//        {
//            var files = Directory.GetFiles("/nonexistent/directory/");
//            return Ok(files);
//        }

//        [HttpGet("access-denied")]
//        public IActionResult TestAccessDenied()
//        {
//            // Try to access a protected system file
//            var content = System.IO.File.ReadAllText("/etc/shadow"); // Linux
//            return Ok(content);
//        }

//        [HttpGet("disk-full")]
//        public IActionResult TestDiskFull()
//        {
//            throw new IOException("There is not enough space on the disk.");
//        }

//        [HttpGet("file-in-use")]
//        public IActionResult TestFileInUse()
//        {
//            throw new IOException("The process cannot access the file because it is being used by another process.");
//        }

//        // Network Errors
//        [HttpGet("network-unreachable")]
//        public async Task<IActionResult> TestNetworkUnreachable()
//        {
//            using var client = new HttpClient();
//            await client.GetAsync("http://192.0.2.1/unreachable"); // Reserved IP that should be unreachable
//            return Ok();
//        }

//        [HttpGet("dns-failure")]
//        public async Task<IActionResult> TestDnsFailure()
//        {
//            using var client = new HttpClient();
//            await client.GetAsync("http://this-domain-does-not-exist-12345.com");
//            return Ok();
//        }

//        [HttpGet("connection-refused")]
//        public IActionResult TestConnectionRefused()
//        {
//            var tcpClient = new TcpClient();
//            tcpClient.Connect("127.0.0.1", 12345); // Non-existent port
//            return Ok();
//        }

//        [HttpGet("ssl-error")]
//        public async Task<IActionResult> TestSslError()
//        {
//            using var client = new HttpClient();
//            await client.GetAsync("https://self-signed.badssl.com/");
//            return Ok();
//        }

//        // Configuration Errors
//        [HttpGet("config-missing")]
//        public IActionResult TestConfigMissing()
//        {
//            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
//            var value = config["NonExistentRequiredSetting"] ?? throw new InvalidOperationException("Required configuration 'NonExistentRequiredSetting' is missing");
//            return Ok(value);
//        }

//        [HttpGet("service-not-registered")]
//        public IActionResult TestServiceNotRegistered()
//        {
//            var service = HttpContext.RequestServices.GetRequiredService<INonExistentService>();
//            return Ok();
//        }

//        // Serialization Errors
//        [HttpGet("json-serialization")]
//        public IActionResult TestJsonSerialization()
//        {
//            var circularRef = new CircularReference();
//            circularRef.Self = circularRef;
//            return Json(circularRef); // Will cause circular reference error
//        }

//        [HttpGet("json-deserialization")]
//        public IActionResult TestJsonDeserialization()
//        {
//            var invalidJson = "{ invalid json content }";
//            var obj = JsonSerializer.Deserialize<object>(invalidJson);
//            return Ok(obj);
//        }

//        // Security Errors
//        [HttpGet("security-exception")]
//        public IActionResult TestSecurityException()
//        {
//            throw new System.Security.SecurityException("Access to the requested resource is forbidden");
//        }

//        [HttpGet("cryptographic-exception")]
//        public IActionResult TestCryptographicException()
//        {
//            throw new System.Security.Cryptography.CryptographicException("Invalid cryptographic operation");
//        }

//        // External Service Dependencies
//        [HttpGet("external-service-down")]
//        public async Task<IActionResult> TestExternalServiceDown()
//        {
//            using var client = new HttpClient();
//            client.Timeout = TimeSpan.FromSeconds(5);

//            // Simulate calling an external service that's down
//            var response = await client.GetAsync("https://httpstat.us/503");
//            response.EnsureSuccessStatusCode();
//            return Ok();
//        }

//        [HttpGet("rate-limit-exceeded")]
//        public async Task<IActionResult> TestRateLimitExceeded()
//        {
//            using var client = new HttpClient();
//            var response = await client.GetAsync("https://httpstat.us/429");
//            response.EnsureSuccessStatusCode();
//            return Ok();
//        }

//        // Concurrency Issues
//        [HttpGet("race-condition")]
//        public async Task<IActionResult> TestRaceCondition()
//        {
//            var counter = 0;
//            var tasks = new List<Task>();

//            for (int i = 0; i < 100; i++)
//            {
//                tasks.Add(Task.Run(() => {
//                    // Simulate race condition
//                    var temp = counter;
//                    Thread.Sleep(1);
//                    counter = temp + 1;
//                }));
//            }

//            await Task.WhenAll(tasks);

//            if (counter != 100)
//            {
//                throw new InvalidOperationException($"Race condition detected: expected 100, got {counter}");
//            }

//            return Ok(counter);
//        }

//        [HttpGet("deadlock-simulation")]
//        public async Task<IActionResult> TestDeadlockSimulation()
//        {
//            var lock1 = new object();
//            var lock2 = new object();

//            var task1 = Task.Run(() => {
//                lock (lock1)
//                {
//                    Thread.Sleep(100);
//                    lock (lock2) { }
//                }
//            });

//            var task2 = Task.Run(() => {
//                lock (lock2)
//                {
//                    Thread.Sleep(100);
//                    lock (lock1) { }
//                }
//            });

//            await Task.WhenAll(task1, task2);
//            return Ok();
//        }

//        [HttpGet("cpu-intensive")]
//        public IActionResult TestCpuIntensive()
//        {
//            // CPU intensive operation that might timeout
//            var result = 0;
//            for (long i = 0; i < 10_000_000_000; i++)
//            {
//                result += (int)(i % 1000);
//            }
//            return Ok(result);
//        }

//        // Cloud/AWS Specific Errors
//        [HttpGet("lambda-timeout")]
//        public async Task<IActionResult> TestLambdaTimeout()
//        {
//            // Simulate Lambda timeout
//            await Task.Delay(TimeSpan.FromMinutes(16)); // Exceeds Lambda max execution time
//            return Ok();
//        }

//        [HttpGet("cloud-service-unavailable")]
//        public IActionResult TestCloudServiceUnavailable()
//        {
//            throw new HttpRequestException("AWS service is temporarily unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable);
//        }

//        // Application-Specific Business Logic Errors
//        [HttpGet("business-rule-violation")]
//        public IActionResult TestBusinessRuleViolation()
//        {
//            throw new InvalidOperationException("Business rule violation: Cannot process order for inactive customer");
//        }

//        [HttpGet("data-corruption")]
//        public IActionResult TestDataCorruption()
//        {
//            throw new InvalidDataException("Data corruption detected: Checksum mismatch");
//        }

//        [HttpGet("version-mismatch")]
//        public IActionResult TestVersionMismatch()
//        {
//            throw new InvalidOperationException("API version mismatch: Client version 1.0 is not compatible with server version 2.0");
//        }

//        // Edge Cases
//        [HttpGet("unicode-error")]
//        public IActionResult TestUnicodeError()
//        {
//            var invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };
//            var invalidString = System.Text.Encoding.UTF8.GetString(invalidUtf8);
//            return Ok(invalidString);
//        }

//        [HttpGet("format-exception")]
//        public IActionResult TestFormatException()
//        {
//            var invalidNumber = int.Parse("not-a-number");
//            return Ok(invalidNumber);
//        }

//        [HttpGet("overflow-exception")]
//        public IActionResult TestOverflowException()
//        {
//            checked
//            {
//                int max = int.MaxValue;
//                int overflow = max + 1; // Will throw OverflowException
//                return Ok(overflow);
//            }
//        }
//    }
//    public class CircularReference
//    {
//        public CircularReference Self { get; set; }
//    }

//    public interface INonExistentService
//    {
//        void DoSomething();
//    }
//    public class ComplexModel
//    {
//        public string RequiredProperty { get; set; }
//    }

//    public interface INonExistentService2
//    {
//        string GetData();
//    }
//}
