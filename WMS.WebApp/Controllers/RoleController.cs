using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Roles;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Permissions;
using WMS.WebApp.Models.Roles;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;
        private readonly IClientService _clientService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            IClientService clientService,
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _clientService = clientService;
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ROLE_READ}")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetRoles(DataTablesRequest request)
        {
            var result = await _roleService.GetPaginatedRoles(
                request.Search?.Value,
                request.Start,
                request.Length,
                request.Order?.FirstOrDefault()?.Column ?? 0,
                request.Order?.FirstOrDefault()?.Dir == "asc"
            );

            result.Items.ForEach(x =>
            {
                x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.ROLE_WRITE);
                x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.ROLE_DELETE);
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
        /// Display the create Role form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ROLE_WRITE}")]
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new RolePageViewModel();

            viewModel.HasEditAccess = _currentUserService.HasPermission("Role.Write");

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Role form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var RoleCreateDto = new RoleCreateDto
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description
                    };

                    // Create Role
                    await _roleService.CreateRoleAsync(RoleCreateDto);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Role created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating Role: {ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }

                    _toastService.AddErrorToast($"{ex.Message}");
                }
            }
            else if (Request.IsAjaxRequest())
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            return View("Form", viewModel);
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ROLE_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {

            Guid RoleId = id ?? Guid.Parse(_currentUserService.UserId);
            bool isOwnProfile = RoleId.ToString() == _currentUserService.UserId;

            // Check if Role has edit permission
            bool hasEditAccess = false;

            // If not own profile and no edit access, redirect if not allowed to view
            if (!isOwnProfile && !hasEditAccess && !_currentUserService.HasPermission("Role.Read"))
            {
                return Forbid();
            }
            var Role = await _roleService.GetRoleByIdAsync(RoleId);


            if (Role == null)
            {
                return NotFound();
            }

            var viewModel = new RolePageViewModel
            {
                Role = new RoleViewModel
                {
                    Id = Role.Id,
                    Name = Role.Name,
                    Description = Role.Description,
                    PermissionCount = Role.PermissionCount,
                    UserCount = Role.UserCount,
                },
                HasEditAccess = hasEditAccess,
                IsEdit = true
            };

            return View("Detail", viewModel);
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ROLE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {

            Guid RoleId = id ?? Guid.Parse(_currentUserService.UserId);
            bool isOwnProfile = RoleId.ToString() == _currentUserService.UserId;

            // Check if Role has edit permission
            bool hasEditAccess = _currentUserService.HasPermission("Role.Write");

            // If not own profile and no edit access, redirect if not allowed to view
            if (!isOwnProfile && !hasEditAccess && !_currentUserService.HasPermission("Role.Read"))
            {
                return Forbid();
            }
            var Role = await _roleService.GetRoleByIdAsync(RoleId);


            if (Role == null)
            {
                return NotFound();
            }

            var viewModel = new RolePageViewModel
            {
                Role = new RoleViewModel
                {
                    Id = Role.Id,
                    Name = Role.Name,
                    Description = Role.Description,
                    PermissionCount = Role.PermissionCount,
                    UserCount = Role.UserCount
                },
                HasEditAccess = hasEditAccess,
                IsEdit = true
            };

            return View("Detail", viewModel);
        }

        //this return 2 type, either call from form action or ajax call
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Re-check permissions
                    bool isOwnProfile = model.Id.ToString() == _currentUserService.UserId;
                    bool hasEditAccess = _currentUserService.HasPermission("Role.Write");

                    if (!isOwnProfile && !hasEditAccess)
                    {
                        return Forbid();
                    }

                    if (!ModelState.IsValid)
                    {
                        var viewModel = new RolePageViewModel
                        {
                            Role = model,
                            HasEditAccess = hasEditAccess
                        };
                        return View("Edit", viewModel);
                    }
                    var roleUpdateDto = new RoleUpdateDto
                    {
                        Name = model.Name,
                        Description = model.Description,
                        //HasEditAccess = hasEditAccess
                    };

                    await _roleService.UpdateRoleAsync(model.Id, roleUpdateDto);
                    _toastService.AddSuccessToast("Profile updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating Role: {ex.Message}");
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
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetRolePermissions(Guid RoleId)
        {
            try
            {
                // Check if Role has permission to manage permissions
                bool hasEditAccess = _currentUserService.HasPermission("Role.Write");
                if (!hasEditAccess)
                {
                    return Forbid();
                }

                var result = await _roleService.GetRolePermissionsDataAsync(RoleId);

                var response = new RolePermissionDataViewModel
                {
                    AllPermissions = result.AllPermissions.Select(p => new PermissionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? "",
                        Module = p.Module
                    }).ToList(),
                    RolePermissions = result.RolePermissionIds.ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Role permissions for Role {RoleId}", RoleId);
                return StatusCode(500, new { success = false, message = "Failed to load permissions" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveRolePermissions([FromBody] SaveRolePermissionsRequestDto request)
        {
            try
            {
                // Check if Role has permission to manage permissions
                bool hasEditAccess = _currentUserService.HasPermission("Role.Write");
                if (!hasEditAccess)
                {
                    return Json(new UserPermissionResponseDto
                    {
                        Success = false,
                        Message = "You don't have permission to manage Role permissions"
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

                var result = await _roleService.SaveRolePermissionChangesAsync(
                    request.RoleId,
                    request.Changes,
                    request.RoleId.ToString());

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
                _logger.LogError(ex, "Error saving Role permissions for Role {RoleId}", request.RoleId);

                var errorMessage = "Failed to save permissions. Please try again.";
                _toastService.AddErrorToast(errorMessage);

                return Json(new UserPermissionResponseDto
                {
                    Success = false,
                    Message = errorMessage
                });
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid roleId)
        {
            try
            {
                await _roleService.DeleteRoleAsync(roleId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Role deleted successfully" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"{ex.Message}" });
            }
        }
    }
}
