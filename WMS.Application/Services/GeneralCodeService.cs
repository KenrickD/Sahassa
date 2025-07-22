// GeneralCodeService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.Extensions;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class GeneralCodeService : IGeneralCodeService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<GeneralCodeService> _logger;

        public GeneralCodeService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            ILogger<GeneralCodeService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _logger = logger;
        }

        #region Code Type Operations

        public async Task<PaginatedResult<GeneralCodeTypeDto>> GetPaginatedCodeTypesAsync(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated code types: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}",
                searchTerm, skip, take);

            try
            {
                var query = _dbContext.GeneralCodeTypes
                    .ApplyTenantFilter(_currentUserService)
                    .Include(ct => ct.Warehouse)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(ct =>
                        ct.Name.ToLower().Contains(searchTerm) ||
                        (ct.Description != null && ct.Description.ToLower().Contains(searchTerm)));
                }

                // Apply sorting
                query = ApplyCodeTypeSorting(query, sortColumn, !sortAscending);

                var totalCount = await query.CountAsync();

                var codeTypes = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var codeTypeDtos = _mapper.Map<List<GeneralCodeTypeDto>>(codeTypes);

                // Get codes count for each type
                foreach (var dto in codeTypeDtos)
                {
                    dto.CodesCount = await _dbContext.GeneralCodes
                        .CountAsync(c => c.GeneralCodeTypeId == dto.Id && !c.IsDeleted);
                }

                return new PaginatedResult<GeneralCodeTypeDto>
                {
                    Items = codeTypeDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated code types");
                throw;
            }
        }

        public async Task<List<GeneralCodeTypeDto>> GetAllCodeTypesAsync()
        {
            var codeTypes = await _dbContext.GeneralCodeTypes
                .ApplyTenantFilter(_currentUserService)
                .Include(ct => ct.Warehouse)
                .OrderBy(ct => ct.Name)
                .ToListAsync();

            var result = _mapper.Map<List<GeneralCodeTypeDto>>(codeTypes);

            // Get codes count for each type
            foreach (var dto in result)
            {
                dto.CodesCount = await _dbContext.GeneralCodes
                    .CountAsync(c => c.GeneralCodeTypeId == dto.Id && !c.IsDeleted);
            }

            return result;
        }

        public async Task<GeneralCodeTypeDto> GetCodeTypeByIdAsync(Guid id)
        {
            var codeType = await _dbContext.GeneralCodeTypes
                .ApplyTenantFilter(_currentUserService)
                .Include(ct => ct.Warehouse)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (codeType == null)
                throw new KeyNotFoundException($"Code type with ID {id} not found.");

            var result = _mapper.Map<GeneralCodeTypeDto>(codeType);
            result.CodesCount = await _dbContext.GeneralCodes
                .CountAsync(c => c.GeneralCodeTypeId == id && !c.IsDeleted);

            return result;
        }

        public async Task<GeneralCodeTypeDto> CreateCodeTypeAsync(GeneralCodeTypeCreateDto dto)
        {
            _logger.LogInformation("Creating new code type: {Name}", dto.Name);

            // Validate unique name within warehouse
            if (await CodeTypeNameExistsAsync(dto.Name))
            {
                throw new InvalidOperationException($"Code type name '{dto.Name}' already exists.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var codeType = _mapper.Map<GeneralCodeType>(dto);
                codeType.Id = Guid.NewGuid();
                codeType.CreatedBy = _currentUserService.UserId;
                codeType.CreatedAt = _dateTime.Now;

                await _dbContext.AddAsync(codeType);
                await _dbContext.SaveChangesAsync();

                await _dbContext.Entry(codeType).Reference(ct => ct.Warehouse).LoadAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<GeneralCodeTypeDto>(codeType);
                result.CodesCount = 0;

                _logger.LogInformation("Code type created successfully: {Id} - {Name}", result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating code type: {Name}", dto.Name);
                throw;
            }
        }

        public async Task<GeneralCodeTypeDto> UpdateCodeTypeAsync(Guid id, GeneralCodeTypeUpdateDto dto)
        {
            _logger.LogInformation("Updating code type: {Id} - {Name}", id, dto.Name);

            if (await CodeTypeNameExistsAsync(dto.Name, id))
            {
                throw new InvalidOperationException($"Code type name '{dto.Name}' already exists.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var codeType = await _dbContext.GeneralCodeTypes
                    .ApplyTenantFilter(_currentUserService)
                    .Include(ct => ct.Warehouse)
                    .FirstOrDefaultAsync(ct => ct.Id == id);

                if (codeType == null)
                    throw new KeyNotFoundException($"Code type with ID {id} not found.");

                _mapper.Map(dto, codeType);
                codeType.ModifiedBy = _currentUserService.UserId;
                codeType.ModifiedAt = _dateTime.Now;

                _dbContext.Update(codeType);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<GeneralCodeTypeDto>(codeType);
                result.CodesCount = await _dbContext.GeneralCodes
                    .CountAsync(c => c.GeneralCodeTypeId == id && !c.IsDeleted);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating code type: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCodeTypeAsync(Guid id)
        {
            _logger.LogInformation("Deleting code type: {Id}", id);

            var codeType = await _dbContext.GeneralCodeTypes
                .ApplyTenantFilter(_currentUserService)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (codeType == null)
                throw new KeyNotFoundException($"Code type with ID {id} not found.");

            // Check if type has associated codes
            var hasCodes = await _dbContext.GeneralCodes.AnyAsync(c => c.GeneralCodeTypeId == id && !c.IsDeleted);
            if (hasCodes)
            {
                throw new InvalidOperationException($"Cannot delete code type '{codeType.Name}' as it has associated codes.");
            }

            codeType.IsDeleted = true;
            codeType.ModifiedBy = _currentUserService.UserId;
            codeType.ModifiedAt = _dateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Code Operations

        public async Task<PaginatedResult<GeneralCodeDto>> GetPaginatedCodesAsync(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending, Guid? codeTypeId = null)
        {
            var query = _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .ThenInclude(ct => ct.Warehouse)
                .AsQueryable();

            if (codeTypeId.HasValue)
            {
                query = query.Where(c => c.GeneralCodeTypeId == codeTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Detail != null && c.Detail.ToLower().Contains(searchTerm)) ||
                    c.GeneralCodeType.Name.ToLower().Contains(searchTerm));
            }

            query = ApplyCodeSorting(query, sortColumn, !sortAscending);

            var totalCount = await query.CountAsync();
            var codes = await query.Skip(skip).Take(take).ToListAsync();
            var codeDtos = _mapper.Map<List<GeneralCodeDto>>(codes);

            return new PaginatedResult<GeneralCodeDto>
            {
                Items = codeDtos,
                TotalCount = totalCount,
                FilteredCount = totalCount
            };
        }

        public async Task<List<GeneralCodeDto>> GetCodesByTypeIdAsync(Guid codeTypeId)
        {
            var codes = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .Where(c => c.GeneralCodeTypeId == codeTypeId)
                .OrderBy(c => c.Sequence)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<List<GeneralCodeDto>>(codes);
        }
        public async Task<List<GeneralCodeDto>> GetCodesByTypeAsync(string codeType)
        {
            var codes = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .Where(c => c.GeneralCodeType.Name == codeType)
                .OrderBy(c => c.Sequence)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<List<GeneralCodeDto>>(codes);
        }

        public async Task<List<GeneralCodeDto>> GetCodesByTypesAsync(List<string> codeTypes)
        {
            var codes = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .Where(c => codeTypes.Contains(c.GeneralCodeType.Name))
                .OrderBy(c => c.Sequence)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<List<GeneralCodeDto>>(codes);
        }

        public async Task<GeneralCodeDto> GetCodeByIdAsync(Guid id)
        {
            var code = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .ThenInclude(ct => ct.Warehouse)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (code == null)
                throw new KeyNotFoundException($"Code with ID {id} not found.");

            return _mapper.Map<GeneralCodeDto>(code);
        }

        public async Task<GeneralCodeDto> CreateCodeAsync(GeneralCodeCreateDto dto)
        {
            if (await CodeNameExistsInTypeAsync(dto.Name, dto.GeneralCodeTypeId))
            {
                throw new InvalidOperationException($"Code name '{dto.Name}' already exists in this type.");
            }

            // Auto-assign sequence if needed
            if (dto.Sequence <= 0)
            {
                dto.Sequence = await GetNextSequenceAsync(dto.GeneralCodeTypeId);
            }

            var code = _mapper.Map<GeneralCode>(dto);
            code.Id = Guid.NewGuid();

            await _dbContext.AddAsync(code);
            await _dbContext.SaveChangesAsync();

            return await GetCodeByIdAsync(code.Id);
        }

        public async Task<GeneralCodeDto> UpdateCodeAsync(Guid id, GeneralCodeUpdateDto dto)
        {
            if (await CodeNameExistsInTypeAsync(dto.Name, Guid.Empty, id)) // We'll get the type from existing code
            {
                throw new InvalidOperationException($"Code name '{dto.Name}' already exists in this type.");
            }

            var code = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (code == null)
                throw new KeyNotFoundException($"Code with ID {id} not found.");

            _mapper.Map(dto, code);
            code.ModifiedBy = _currentUserService.UserId;
            code.ModifiedAt = _dateTime.Now;

            _dbContext.Update(code);
            await _dbContext.SaveChangesAsync();

            return await GetCodeByIdAsync(id);
        }

        public async Task<bool> DeleteCodeAsync(Guid id)
        {
            var code = await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (code == null)
                throw new KeyNotFoundException($"Code with ID {id} not found.");

            // Check for usage in FG Receive Pallet
            bool isUsedInFGPallet = await _dbContext.GIV_FG_Receives
                .AnyAsync(p => p.PackageTypeId == id && !p.IsDeleted);

            // Check for usage in RM Receive Pallet
            bool isUsedInRMPallet = await _dbContext.GIV_RM_Receives
                .AnyAsync(p => p.PackageTypeId == id && !p.IsDeleted);

            // Check for usage in Container Status
            bool isUsedInContainer = await _dbContext.GIV_Containers
                .AnyAsync(c => c.StatusId == id && !c.IsDeleted);

            if (isUsedInFGPallet || isUsedInRMPallet || isUsedInContainer)
            {
                throw new InvalidOperationException("Cannot delete code because it is currently in use.");
            }

            code.IsDeleted = true;
            code.ModifiedBy = _currentUserService.UserId;
            code.ModifiedAt = _dateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Hierarchical Operations

        public async Task<List<GeneralCodeWithTypeDto>> GetCodesWithTypesAsync()
        {
            var codeTypes = await GetAllCodeTypesAsync();
            var result = new List<GeneralCodeWithTypeDto>();

            foreach (var codeType in codeTypes)
            {
                var codes = await GetCodesByTypeIdAsync(codeType.Id);
                result.Add(new GeneralCodeWithTypeDto
                {
                    CodeType = codeType,
                    Codes = codes,
                    IsExpanded = false
                });
            }

            return result;
        }

        public async Task<bool> ReorderCodesAsync(List<(Guid CodeId, int NewSequence)> reorderData)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var (codeId, newSequence) in reorderData)
                {
                    var code = await _dbContext.GeneralCodes
                        .ApplyTenantFilter(_currentUserService)
                        .FirstOrDefaultAsync(c => c.Id == codeId);

                    if (code != null)
                    {
                        code.Sequence = newSequence;
                        code.ModifiedBy = _currentUserService.UserId;
                        code.ModifiedAt = _dateTime.Now;
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reordering codes");
                throw;
            }
        }

        #endregion

        #region Validation Methods

        public async Task<bool> CodeTypeExistsAsync(Guid id)
        {
            return await _dbContext.GeneralCodeTypes
                .ApplyTenantFilter(_currentUserService)
                .AnyAsync(ct => ct.Id == id);
        }

        public async Task<bool> CodeTypeNameExistsAsync(string name, Guid? excludeId = null)
        {
            var warehouseId = _currentUserService.CurrentWarehouseId;
            return await _dbContext.GeneralCodeTypes
                .AnyAsync(ct => ct.Name == name &&
                              ct.WarehouseId == warehouseId &&
                              !ct.IsDeleted &&
                              (excludeId == null || ct.Id != excludeId));
        }

        public async Task<bool> CodeExistsAsync(Guid id)
        {
            return await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .AnyAsync(c => c.Id == id);
        }

        public async Task<bool> CodeNameExistsInTypeAsync(string name, Guid codeTypeId, Guid? excludeId = null)
        {
            return await _dbContext.GeneralCodes
                .AnyAsync(c => c.Name == name &&
                              c.GeneralCodeTypeId == codeTypeId &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }

        #endregion

        #region Utility Methods

        public async Task<List<GeneralCodeTypeDto>> GetCodeTypesForDropdownAsync()
        {
            return await _dbContext.GeneralCodeTypes
                .ApplyTenantFilter(_currentUserService)
                .OrderBy(ct => ct.Name)
                .Select(ct => new GeneralCodeTypeDto
                {
                    Id = ct.Id,
                    Name = ct.Name,
                    Description = ct.Description
                })
                .ToListAsync();
        }

        public async Task<List<GeneralCodeDto>> GetCodesForDropdownAsync(string codeTypeName)
        {
            return await _dbContext.GeneralCodes
                .ApplyTenantFilter(_currentUserService)
                .Include(c => c.GeneralCodeType)
                .Where(c => c.GeneralCodeType.Name == codeTypeName)
                .OrderBy(c => c.Sequence)
                .Select(c => new GeneralCodeDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Detail = c.Detail,
                    Sequence = c.Sequence
                })
                .ToListAsync();
        }

        public async Task<int> GetNextSequenceAsync(Guid codeTypeId)
        {
            var maxSequence = await _dbContext.GeneralCodes
                .Where(c => c.GeneralCodeTypeId == codeTypeId && !c.IsDeleted)
                .MaxAsync(c => (int?)c.Sequence) ?? 0;

            return maxSequence + 1;
        }

        #endregion

        #region Private Methods

        private IQueryable<GeneralCodeType> ApplyCodeTypeSorting(IQueryable<GeneralCodeType> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(ct => ct.Name) : query.OrderBy(ct => ct.Name),
                2 => sortDescending ? query.OrderByDescending(ct => ct.Description) : query.OrderBy(ct => ct.Description),
                3 => sortDescending ? query.OrderByDescending(ct => ct.CreatedAt) : query.OrderBy(ct => ct.CreatedAt),
                _ => sortDescending ? query.OrderByDescending(ct => ct.Name) : query.OrderBy(ct => ct.Name)
            };
        }

        private IQueryable<GeneralCode> ApplyCodeSorting(IQueryable<GeneralCode> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(c => c.GeneralCodeType.Name) : query.OrderBy(c => c.GeneralCodeType.Name),
                2 => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                3 => sortDescending ? query.OrderByDescending(c => c.Sequence) : query.OrderBy(c => c.Sequence),
                4 => sortDescending ? query.OrderByDescending(c => c.Detail) : query.OrderBy(c => c.Detail),
                _ => sortDescending ? query.OrderByDescending(c => c.Sequence) : query.OrderBy(c => c.Sequence)
            };
        }

        #endregion
    }
}