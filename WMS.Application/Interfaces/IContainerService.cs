using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.API;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.RawMaterial;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Interfaces
{
    public interface IContainerService
    {
        Task<Guid> DeltaUpdateContainerAsync(ContainerCreateDto containerDto, string Username,bool WhFlag);
        Task<List<Containers>> GetAllContainerAsync();
        Task<PaginatedResult<ContainerViewDto>> GetPaginatedContainersAsync(
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending
        );
        Task<PaginatedResult<ContainerViewDto>> GetPaginatedContainersByTypeAsync(
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending,
            ContainerProcessType processType);
        Task<ApiResponseDto<string>> UpdateContainerAsync(ContainerUpdateDto containerDto, string Username);
        Task<ApiResponseDto<List<string>>> GetContainerPhotoUrlsAsync(Guid containerId);
        Task<ApiResponseDto<string>> DeleteContainerPhotoAsync(Guid photoId);
        Task<ApiResponseDto<string>> ReplaceContainerPhotoAsync(Guid photoId, IFormFile newFile);
        Task<ApiResponseDto<byte[]>> GenerateExcelReportAsync(Guid containerId);
        Task<ApiResponseDto<(byte[] FileContent, string FileName)>> GeneratePdfReportAsync(
    Guid containerId,
    CancellationToken cancellationToken = default);
        Task<ContainerPalletsResponse> GetContainerPalletsAsync(
            Guid containerId,
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null);

        Task<List<PalletItemDto>> GetPalletItemsAsync(Guid palletId);
        Task<List<JobAttachmentsDto>?> GetExternalContainerInfoAttachmentByIdAsync(Guid containerId, CancellationToken cancellationToken = default);
        Task<List<Containers>> GetContainersByProcessTypeAsync(ContainerProcessType processType);
    }
}
