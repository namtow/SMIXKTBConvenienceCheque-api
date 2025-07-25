using AutoMapper;
using Serilog;
using SMIXKTBConvenienceCheque.Data;
using SMIXKTBConvenienceCheque.DTOs.Report;
using SMIXKTBConvenienceCheque.Helpers;
using SMIXKTBConvenienceCheque.Models;

namespace SMIXKTBConvenienceCheque.Services.Report
{
    public class ReportServices : IReportServices
    {
        private readonly AppDBContext _dBContext;
        private readonly IMapper _mapper;
        private readonly Serilog.ILogger _logger;
        private const string _serviceName = nameof(ReportServices);
        private readonly List<string> _cheque = new() { "FileNo", "BatchNo", "ClaimNo", "Prefix", "เลขที่อ้างอิงเอกสาร", "ชื่อโรงเรียน", "วันทีพิมพ์บนหน้าเช็ค", "หมายเลขเช็ค", "ชื่อผู้รับเช็ค", "จำนวนเงินจ่ายสุทธิ", "สถานะของเช็ค", "รายละเอียดสถานะของเช็ค", "วันที่สถานะเช็คเปลี่ยนแปลง", "วันทีพิมพ์เช็ค" };

        public ReportServices(AppDBContext dBContext, IMapper mapper)
        {
            _dBContext = dBContext;
            _mapper = mapper;
            _logger = Log.ForContext<ReportServices>();
        }

        public async Task<ServiceResponse<ReportResponseDTO>> DownloadChequeReport(ChequeReportRequestDTO filter)
        {
            var methodName = nameof(DownloadChequeReport);
            try
            {
                _logger.Debug("[{ServiceName}][{FunctionName}] - Start Date: {Date} , Store: usp_ReportCheque_Select , Filter: {@Filter}", _serviceName, methodName, DateTime.Now, filter);
                var result = await _dBContext.Procedures.usp_ReportCheque_SelectAsync(filter.ChequeStatus);

                if (result.Count() == 0)
                    throw new Exception("ไม่พบข้อมูลรายงาน");

                _logger.Debug("[{ServiceName}][{FunctionName}] - Mapper data to export", _serviceName, methodName);
                var resultOut = _mapper.Map<List<ChequeReportResponseDTO>>(result);

                var fileName = $"รายงานเช็ค {DateTime.Now:ddMMyyyy mm}";
                var excelWorkSheet = new NPOIExcelExportHelper();

                excelWorkSheet.AddSheetHeader(resultOut, "Cheque", _cheque);

                var res = new ReportResponseDTO
                {
                    FileName = fileName,
                    Data = excelWorkSheet.GetFile(),
                };

                _logger.Debug("[{ServiceName}][{FunctionName}] - Done.", _serviceName, methodName);
                return ResponseResult.Success(res, "Success");
            }
            catch (Exception e)
            {
                _logger.Error(e, "[{ServiceName}][{FunctionName}] - An error occurred , {Msg}", _serviceName, methodName, e.Message);
                return ResponseResult.Failure<ReportResponseDTO>(e.Message);
            }
        }
    }
}