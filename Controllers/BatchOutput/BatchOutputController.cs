using Microsoft.AspNetCore.Mvc;
using Serilog;
using SMIXKTBConvenienceCheque.DTOs.BatchOutput;
using SMIXKTBConvenienceCheque.Models;
using SMIXKTBConvenienceCheque.Services.BatchOutput;

namespace SMIXKTBConvenienceCheque.Controllers.BatchOutput
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchOutputController : ControllerBase
    {
        private readonly Serilog.ILogger _logger;
        private readonly IBatchOutputServices _service;
        private const string _controllerName = nameof(BatchOutputController);

        public BatchOutputController(IBatchOutputServices service)
        {
            _logger = Log.ForContext<BatchOutputController>();
            _service = service;
        }

        /// <summary>
        /// Inserts batch output data into the database.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("batchoutputinsert")]
        public async Task<ServiceResponse<BatchOutputInsertResponseDTO>> BatchOutputInsert([FromQuery] BatchOutputInsertRequestDTO data)
        {
            var methodName = nameof(BatchOutputInsert);
            try
            {
                var res = await _service.BatchOutputInsert(data);
                return ResponseResult.Success(res, "Batch output data inserted successfully.");
            }
            catch (Exception e)
            {
                _logger.Error(e, "[{ControllerName}][{MethodName}] - An error occurred , {Msg}", _controllerName, methodName, e.Message);
                return ResponseResult.Failure<BatchOutputInsertResponseDTO>(e.Message);
            }
        }

        /// <summary>
        ///  Upload file to backup
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("uploadfile")]
        public async Task<ServiceResponse<BatchOutputInsertResponseDTO>> UploadFileBackupBatchOutput([FromForm] UpsertBatchFileNoUpload input)
        {
            var methodName = nameof(UploadFileBackupBatchOutput);
            try
            {
                var res = await _service.UploadFileBackupBatchOutput(input);
                return ResponseResult.Success(res, "Success");
            }
            catch (Exception e)
            {
                _logger.Error(e, "[{ControllerName}][{MethodName}] - An error occurred , {Msg}", _controllerName, methodName, e.Message);
                return ResponseResult.Failure<BatchOutputInsertResponseDTO>(e.Message);
            }
        }

        [HttpGet("batchheader")]
        public async Task<ServiceResponse<GetBatchOutputHeaderResponseDTO>> GetBatchOutputHeader()
        {
            var methodName = nameof(GetBatchOutputHeader);
            try
            {
                var res = await _service.GetBatchOutputHeader();
                return ResponseResult.Success(res, "Success");
            }
            catch (Exception e)
            {
                _logger.Error(e, "[{ControllerName}][{MethodName}] - An error occurred , {Msg}", _controllerName, methodName, e.Message);
                return ResponseResult.Failure<GetBatchOutputHeaderResponseDTO>(e.Message);
            }
        }
    }
}