using SMIXKTBConvenienceCheque.DTOs.Cheque;
using SMIXKTBConvenienceCheque.Models;

namespace SMIXKTBConvenienceCheque.Services.Cheque
{
    public interface IChequeServices
    {
        Task<ServiceResponse<FileResponseDTO>> CreateFileCheque(FileChequeResponseDTO req);
    }
}