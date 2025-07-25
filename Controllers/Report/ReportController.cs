using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SMIXKTBConvenienceCheque.DTOs.Report;
using SMIXKTBConvenienceCheque.Services.Report;

namespace SMIXKTBConvenienceCheque.Controllers.Report
{
    [Authorize(Policy = Permission.Base)]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _services;
        private readonly Serilog.ILogger _logger;
        private const string _serviceName = nameof(ReportController);

        public ReportController(IReportServices services)
        {
            _services = services;
            _logger = Log.ForContext<ReportController>();
        }

        [HttpGet("cheque/filter")]
        public async Task<IActionResult> DownloadChequeReport([FromQuery] ChequeReportRequestDTO filter)
        {
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var result = await _services.DownloadChequeReport(filter);

            if (result.IsSuccess)
                return File(result.Data.Data, contentType, result.Data.FileName);

            return Ok(result);
        }
    }
}