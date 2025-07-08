using Microsoft.AspNetCore.Mvc;
using SMIXKTBConvenienceCheque.DTOs.Cheque;
using SMIXKTBConvenienceCheque.Services.Cheque;

namespace SMIXKTBConvenienceCheque.Controllers.Cheque
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChequeController : ControllerBase
    {
        private readonly IChequeServices _services;

        public ChequeController(IChequeServices services)
        {
            _services = services;
        }

        [HttpGet("download")]
        public async Task<IActionResult> CreateFileCheque([FromQuery] FileChequeResponseDTO req)
        {
            string contentType = "application/octet-stream"; //MIME type สำหรับไฟล์ .txt
            var fileText = await _services.CreateFileCheque(req);

            if (fileText.Data != null)
            {
                return File(fileText.Data.Data, contentType, fileText.Data.FileName);
            }
            return Ok(fileText);
        }
    }
}