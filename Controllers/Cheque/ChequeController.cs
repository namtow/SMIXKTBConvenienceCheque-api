using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> CreateFileCheque()
        {
            string contentType = "application/octet-stream"; //MIME type สำหรับไฟล์ .txt
            var fileText = await _services.CreateFileCheque();

            if (fileText.Data != null)
            {
                return File(fileText.Data.Data, contentType, fileText.Data.FileName);
            }
            return Ok(fileText);
        }
    }
}