using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Users;

namespace WMS.Application.Helpers
{
    public class MemoryHelper
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private readonly string _cacheKeyPrefix = "userProfile";
        public MemoryHelper(IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IUserService userService)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _userService = userService;
        }

        public async Task<UserDto?> GetUserProfileAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return null;

            var cacheKey = $"{_cacheKeyPrefix}_{userId}";

            // Try to get from cache first
            if (!_cache.TryGetValue(cacheKey, out UserDto? userDto))
            {
                var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));

                if (user == null)
                    return null;

                // Create profile with needed properties
                userDto = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-profile.png",
                    ClientId = user.ClientId,
                    WarehouseId = user.WarehouseId,
                    Roles = user.Roles
                };

                // Cache for 30 minutes (adjust as needed)
                _cache.Set(cacheKey, userDto, TimeSpan.FromMinutes(30));
            }
            return userDto;
        }
        public void ClearCache()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var cacheKey = $"{_cacheKeyPrefix}_{userId}";
                _cache.Remove(cacheKey);
            }
        }
    }
}
