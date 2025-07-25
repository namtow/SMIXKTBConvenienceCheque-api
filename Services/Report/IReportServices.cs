using SMIXKTBConvenienceCheque.DTOs.Report;
using SMIXKTBConvenienceCheque.Models;

namespace SMIXKTBConvenienceCheque.Services.Report
{
    public interface IReportServices
    {
        Task<ServiceResponse<ReportResponseDTO>> DownloadChequeReport(ChequeReportRequestDTO filter);
    }
}