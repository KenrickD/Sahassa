using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Permissions;
using WMS.WebApp.Models.Users;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IClientService _clientService;
        private readonly IWarehouseService _warehouseService;
        private readonly IRoleService _roleService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IClientService clientService,
            IWarehouseService warehouseService,
            IRoleService roleService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _clientService = clientService;
            _warehouseService = warehouseService;
            _roleService = roleService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.USER_READ}")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GetUsers(DataTablesRequest request)
        {
            var result = await _userService.GetPaginatedUsers(
                request.Search?.Value,
                request.Start,
                request.Length,
                request.Order?.FirstOrDefault()?.Column ?? 0,
                request.Order?.FirstOrDefault()?.Dir == "asc"
            );

            result.Items.ForEach(x =>
            {
                x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.USER_WRITE);
                x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.USER_DELETE);
            });

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        /// <summary>
        /// Display the create user form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.USER_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new UserFormViewModel
            {
                IsActive = true
            };

            await PopulateFormDropdowns(viewModel);

            return View("Form", viewModel);
        }

        /// <summary>
        /// Process the create user form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username or email already exists
                    if (!await _userService.IsUsernameAvailableAsync(viewModel.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists");
                        await PopulateFormDropdowns(viewModel);
                        return View("Form", viewModel);
                    }

                    if (!await _userService.IsEmailAvailableAsync(viewModel.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists");
                        await PopulateFormDropdowns(viewModel);
                        return View("Form", viewModel);
                    }

                    // Map view model to DTO
                    var userCreateDto = new UserCreateDto
                    {
                        Username = viewModel.Username,
                        Email = viewModel.Email,
                        FirstName = viewModel.FirstName,
                        LastName = viewModel.LastName,
                        PhoneNumber = viewModel.PhoneNumber,
                        ClientId = viewModel.ClientId,
                        WarehouseId = viewModel.WarehouseId,
                        IsActive = viewModel.IsActive,
                        RoleIds = viewModel.SelectedRoleIds,
                        ProfileImage = viewModel.ProfileImage
                    };

                    // Create user
                    await _userService.CreateUserAsync(userCreateDto);

                    _toastService.AddSuccessToast("User created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating user: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");
                }
            }

            await PopulateFormDropdowns(viewModel);
            return View("Form", viewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ViewProfile(Guid? id)
        {

            Guid userId = id ?? Guid.Parse(_currentUserService.UserId);
            bool isOwnProfile = userId.ToString() == _currentUserService.UserId;
            // Check if user has edit permission
            bool hasEditAccess = false;

            // If not own profile and no edit access, redirect if not allowed to view
            if (!isOwnProfile && !hasEditAccess && !_currentUserService.HasPermission("User.Read"))
            {
                return Forbid();
            }
            var user = await _userService.GetUserByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ViewProfileModel
            {
                User = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl)
                        ? "/images/default-profile.png"
                        : user.ProfileImageUrl,
                    WarehouseId = user.WarehouseId,
                    WarehouseName = user.WarehouseName,
                    ClientId = user.ClientId,
                    ClientName = user.ClientName,
                    IsActive = user.IsActive
                },
                IsOwnProfile = isOwnProfile,
                HasEditAccess = hasEditAccess,
                ChangePasswordVM = new ChangePasswordViewModel
                {
                    UserId = user.Id
                }
            };

            // Load warehouses and clients for dropdowns if user has edit access
            if (hasEditAccess)
            {
                await PopulateViewProfileDropDown(viewModel);
            }

            return View("ViewProfile", viewModel);
        }
        /// <summary>
        /// Display the edit user form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.USER_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> EditProfile(Guid? id)
        {

            Guid userId = id ?? Guid.Parse(_currentUserService.UserId);
            bool isOwnProfile = userId.ToString() == _currentUserService.UserId;

            // Check if user has edit permission
            bool hasEditAccess = _currentUserService.HasPermission("User.Write");

            // If not own profile and no edit access, redirect if not allowed to view
            if (!isOwnProfile && !hasEditAccess && !_currentUserService.HasPermission("User.Read"))
            {
                return Forbid();
            }
            var user = await _userService.GetUserByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ViewProfileModel
            {
                User = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl)
                        ? "/images/default-profile.png"
                        : user.ProfileImageUrl,
                    WarehouseId = user.WarehouseId,
                    WarehouseName = user.WarehouseName,
                    ClientId = user.ClientId,
                    ClientName = user.ClientName,
                    IsActive = user.IsActive
                },
                IsOwnProfile = isOwnProfile,
                HasEditAccess = hasEditAccess,
                ChangePasswordVM = new ChangePasswordViewModel
                {
                    UserId = user.Id
                }
            };

            // Load warehouses and clients for dropdowns if user has edit access
            if (hasEditAccess)
            {
                await PopulateViewProfileDropDown(viewModel);
            }

            return View("ViewProfile", viewModel);
        }

        //this return 2 type, either call from form action or ajax call
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Re-check permissions
                    bool isOwnProfile = model.Id.ToString() == _currentUserService.UserId;
                    bool hasEditAccess = _currentUserService.HasPermission("User.Write");

                    if (!isOwnProfile && !hasEditAccess)
                    {
                        return Forbid();
                    }

                    if (!ModelState.IsValid)
                    {
                        var viewModel = new ViewProfileModel
                        {
                            User = model,
                            IsOwnProfile = isOwnProfile,
                            HasEditAccess = hasEditAccess
                        };
                        await PopulateViewProfileDropDown(viewModel);
                        return View("ViewProfile", viewModel);
                    }
                    var userUpdateDto = new UserUpdateDto
                    {
                        Id = model.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        WarehouseId = model.WarehouseId,
                        ClientId = model.ClientId,
                        IsActive = model.IsActive,
                        ProfileImage = model.ProfileImage,
                        HasEditAccess = hasEditAccess,
                        IsOwnProfile = isOwnProfile
                    };

                    await _userService.UpdateUserAsync(userUpdateDto);
                    _toastService.AddSuccessToast("Profile updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating user: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }
            else if (Request.IsAjaxRequest())
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            return RedirectToAction("ViewProfile", new { id = model.Id });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            bool isOwnProfile = model.UserId.ToString() == _currentUserService.UserId;
            bool hasEditAccess = _currentUserService.HasPermission("User.Write");
            try
            {
                if (!isOwnProfile && !hasEditAccess)
                {
                    return Forbid();
                }

                await _userService.ChangePasswordAsync(model.UserId, model.CurrentPassword, model.NewPassword, model.ConfirmPassword);

                _toastService.AddSuccessToast("Password changed successfully!");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error change password user: {ex.Message}");
                _toastService.AddErrorToast($"{ex.Message}");
            }
            if (hasEditAccess)
                return RedirectToAction("EditProfile", new { id = model.UserId });
            else
                return RedirectToAction("ViewProfile", new { id = model.UserId });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            try
            {
                var result = await _userService.GetUserRolesDataAsync(userId);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        allRoles = result.AllRoles.Select(r => new
                        {
                            id = r.Id,
                            name = r.Name,
                            description = r.Description
                            //isSystemRole = r.IsSystemRole,
                            //userCount = r.UserCount
                        }),
                        userRoleIds = result.UserRoleIds
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles for user {UserId}", userId);
                return Json(new { success = false, message = "Failed to load user roles" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserRoleChanges([FromBody] SaveUserRoleChangesRequest request)
        {
            try
            {
                var result = await _userService.SaveUserRoleChangesAsync(
                    request.UserId,
                    request.Changes,
                    _currentUserService.UserId
                );

                return Json(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? result.Data : result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user role changes for user {UserId}", request.UserId);
                return Json(new { success = false, message = "Failed to save role changes" });
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPermissions(Guid userId)
        {
            try
            {
                // Check if user has permission to manage permissions
                bool hasEditAccess = _currentUserService.HasPermission("User.Write");
                if (!hasEditAccess)
                {
                    return Forbid();
                }

                var result = await _userService.GetUserPermissionsDataAsync(userId);

                var response = new UserPermissionsDataViewModel
                {
                    AllPermissions = result.AllPermissions.Select(p => new PermissionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? "",
                        Module = p.Module
                    }).ToList(),
                    UserPermissions = result.UserPermissionIds.ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Failed to load permissions" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveUserPermissions([FromBody] SaveUserPermissionsRequestDto request)
        {
            try
            {
                // Check if user has permission to manage permissions
                bool hasEditAccess = _currentUserService.HasPermission("User.Write");
                if (!hasEditAccess)
                {
                    return Json(new UserPermissionResponseDto
                    {
                        Success = false,
                        Message = "You don't have permission to manage user permissions"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return Json(new UserPermissionResponseDto
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                if (request.Changes == null || !request.Changes.Any())
                {
                    return Json(new UserPermissionResponseDto
                    {
                        Success = false,
                        Message = "No changes to save"
                    });
                }

                var result = await _userService.SaveUserPermissionChangesAsync(
                    request.UserId,
                    request.Changes,
                    _currentUserService.UserId);

                if (result.IsSuccess)
                {
                    var successMessage = $"Successfully updated permissions: {result.Data}";
                    _toastService.AddSuccessToast(successMessage);

                    return Json(new UserPermissionResponseDto
                    {
                        Success = true,
                        Message = successMessage
                    });
                }
                else
                {
                    _toastService.AddErrorToast(result.ErrorMessage);
                    return Json(new UserPermissionResponseDto
                    {
                        Success = false,
                        Message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user permissions for user {UserId}", request.UserId);

                var errorMessage = "Failed to save permissions. Please try again.";
                _toastService.AddErrorToast(errorMessage);

                return Json(new UserPermissionResponseDto
                {
                    Success = false,
                    Message = errorMessage
                });
            }
        }
        /// <summary>
        /// Display the delete confirmation page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userDto = await _userService.GetUserByIdAsync(id);

            if (userDto == null)
            {
                return NotFound();
            }

            var viewModel = new UserItemViewModel
            {
                Id = userDto.Id,
                Username = userDto.Username,
                FullName = $"{userDto.FirstName} {userDto.LastName}".Trim(),
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                ClientName = userDto.ClientName,
                WarehouseName = userDto.WarehouseName,
                IsActive = userDto.IsActive,
                ProfileImageUrl = userDto.ProfileImageUrl,
                Roles = userDto.Roles,
                CreatedAt = userDto.CreatedAt,
                ModifiedAt = userDto.ModifiedAt
            };

            return View(viewModel);
        }

        /// <summary>
        /// Process the delete confirmation
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid userId)
        {
            try
            {
                await _userService.DeleteUserAsync(userId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "User deleted successfully" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"{ex.Message}" });
            }
        }

        // GET: Users/GetClientsByWarehouse
        [HttpGet]
        public async Task<IActionResult> GetClientsByWarehouse(Guid warehouseId)
        {
            if (warehouseId == Guid.Empty)
            {
                return BadRequest();
            }
            var clientsAll = await _clientService.GetAllClientsAsync();

            var clients = clientsAll
                .Where(c => c.WarehouseId == warehouseId && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return Json(clients);
        }
        #region Helper Methods

        /// <summary>
        /// Populates dropdown options for user form
        /// </summary>
        private async Task PopulateFormDropdowns(UserFormViewModel viewModel)
        {
            // Get clients for dropdown
            //var clients = await _clientService.GetAllClientsAsync();
            //viewModel.ClientOptions = clients.Select(c => new SelectListItem
            //{
            //    Value = c.Id.ToString(),
            //    Text = c.Name,
            //    Selected = viewModel.ClientId.HasValue && viewModel.ClientId == c.Id
            //}).ToList();

            // Add "None" option (for warehouse/system users)
            viewModel.ClientOptions.Insert(0, new SelectListItem { Value = "", Text = "None" });

            // Get warehouses for dropdown
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            viewModel.WarehouseOptions = warehouses.Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = w.Name,
                Selected = viewModel.WarehouseId == w.Id
            }).ToList();

            // Get roles for dropdown
            var roles = await _roleService.GetAllRolesAsync();
            viewModel.RoleOptions = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name,
                Selected = viewModel.SelectedRoleIds?.Contains(r.Id) ?? false
            }).ToList();
        }
        private async Task PopulateViewProfileDropDown(ViewProfileModel viewModel)
        {
            // Get warehouses for dropdown
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            viewModel.Warehouses = new SelectList(
                warehouses,
                "Id", "Name");

            if (viewModel.User.WarehouseId != Guid.Empty)
            {
                var clientsAll = await _clientService.GetAllClientsAsync();

                viewModel.Clients = new SelectList(
                    clientsAll.Where(x => x.WarehouseId == viewModel.User.WarehouseId).ToList(),
                    "Id", "Name");
            }
            else
            {
                viewModel.Clients = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            // Get roles for dropdown
            var roles = await _roleService.GetAllRolesAsync();
            viewModel.RoleOptions = roles.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name,
                Selected = viewModel.SelectedRoleIds?.Contains(r.Id) ?? false
            }).ToList();
        }
        #endregion
    }
}