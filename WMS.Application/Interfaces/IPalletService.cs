using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

namespace WMS.Application.Interfaces
{
    public interface IPalletService
    {
        Task<ApiResponseDto<string>> UpdatePalletLocation(UpdatePalletLocationDto updatePalletLocationDto, string Username, Guid warehouseid);
        Task<ApiResponseDto<string>> ActualReleasePalletLocation(ReleasePalletLocationDto releasePalletLocationDto, string Username, Guid warehouseid);
        Task<ApiResponseDto<string>> ReleasePalletLocation(ReleasePalletLocationDto releasePalletLocationDto, string Username, Guid warehouseid);
        Task<ApiResponseDto<BulkUpdatePalletLocationResultDto>> BulkUpdatePalletLocationAsync(BulkUpdatePalletLocationDto dto, string username, Guid warehouseId);
    }
}
