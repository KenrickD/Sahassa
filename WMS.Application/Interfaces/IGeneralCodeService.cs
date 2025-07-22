using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs;

namespace WMS.Application.Interfaces
{
    public interface IGeneralCodeService
    {
        // Code Type operations
        Task<PaginatedResult<GeneralCodeTypeDto>> GetPaginatedCodeTypesAsync(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<List<GeneralCodeTypeDto>> GetAllCodeTypesAsync();
        Task<GeneralCodeTypeDto> GetCodeTypeByIdAsync(Guid id);
        Task<GeneralCodeTypeDto> CreateCodeTypeAsync(GeneralCodeTypeCreateDto dto);
        Task<GeneralCodeTypeDto> UpdateCodeTypeAsync(Guid id, GeneralCodeTypeUpdateDto dto);
        Task<bool> DeleteCodeTypeAsync(Guid id);

        // Code operations
        Task<PaginatedResult<GeneralCodeDto>> GetPaginatedCodesAsync(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending, Guid? codeTypeId = null);
        Task<List<GeneralCodeDto>> GetCodesByTypeIdAsync(Guid codeTypeId);
        Task<List<GeneralCodeDto>> GetCodesByTypeAsync(string codeType);
        Task<List<GeneralCodeDto>> GetCodesByTypesAsync(List<string> codeTypes);
        Task<GeneralCodeDto> GetCodeByIdAsync(Guid id);
        Task<GeneralCodeDto> CreateCodeAsync(GeneralCodeCreateDto dto);
        Task<GeneralCodeDto> UpdateCodeAsync(Guid id, GeneralCodeUpdateDto dto);
        Task<bool> DeleteCodeAsync(Guid id);

        // Hierarchical operations
        Task<List<GeneralCodeWithTypeDto>> GetCodesWithTypesAsync();
        Task<bool> ReorderCodesAsync(List<(Guid CodeId, int NewSequence)> reorderData);

        // Validation methods
        Task<bool> CodeTypeExistsAsync(Guid id);
        Task<bool> CodeTypeNameExistsAsync(string name, Guid? excludeId = null);
        Task<bool> CodeExistsAsync(Guid id);
        Task<bool> CodeNameExistsInTypeAsync(string name, Guid codeTypeId, Guid? excludeId = null);

        // Utility methods
        Task<List<GeneralCodeTypeDto>> GetCodeTypesForDropdownAsync();
        Task<List<GeneralCodeDto>> GetCodesForDropdownAsync(string codeTypeName);
        Task<int> GetNextSequenceAsync(Guid codeTypeId);
    }
}