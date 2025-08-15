using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using SMIXKTBConvenienceCheque.Configurations;
using SMIXKTBConvenienceCheque.Data;
using SMIXKTBConvenienceCheque.DTOs.BatchOutput;
using SMIXKTBConvenienceCheque.Models;
using System.Data;
using System.Globalization;
using System.Text;

namespace SMIXKTBConvenienceCheque.Services.BatchOutput
{
    public class BatchOutputServices : IBatchOutputServices
    {
        private readonly AppDBContext _dBContext;
        private readonly IMapper _mapper;
        private readonly IOptions<ChequeSetting> _cheuqeSetting;
        private readonly Serilog.ILogger _logger;
        private readonly string _serviceName = nameof(BatchOutputServices);

        public BatchOutputServices(AppDBContext dBContext, IMapper mapper, IOptions<ChequeSetting> cheuqeSetting)
        {
            _dBContext = dBContext;
            _mapper = mapper;
            _cheuqeSetting = cheuqeSetting;
            _logger = Log.ForContext<BatchOutputServices>();
        }

        /// <summary>
        /// Inserts batch output data into the database.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{BatchOutputInsertResponseDTO}"/> containing the result of the operation.
        /// </returns>
        public async Task<BatchOutputInsertResponseDTO> BatchOutputInsert(BatchOutputInsertRequestDTO input)
        {
            var methodName = nameof(BatchOutputInsert);
            string filePathName = _cheuqeSetting.Value.Path + input.FileName;
            string fileName = Path.GetFileName(filePathName);

            //ตรวจสอบชื่อไฟล์ นำเข้าซ้ำ
            var fileNameCheuqe = await _dBContext.BatchOutPutHeaders.FirstOrDefaultAsync(h => h.IsActive == true && h.FileName == fileName);
            if (fileNameCheuqe != null)
                throw new Exception("File name is Duplicate.");

            Encoding encoding = Encoding.GetEncoding("windows-874"); // ANSI ภาษาไทย

            string[] allLines = File.ReadAllLines(filePathName, encoding);

            // call function
            await ReadBatchFileNoToUpsertFunction(allLines, input.FileName);

            var dto = new BatchOutputInsertResponseDTO
            {
                Message = "successfully."
            };
            return dto;
        }

        public async Task<BatchOutputInsertResponseDTO> UploadFileBackupBatchOutput(UpsertBatchFileNoUpload input)
        {
            var methodName = nameof(UploadFileBackupBatchOutput);
            _logger.Debug("[{ServiceName}][{FunctionName}] - Start backup date: {Date}", _serviceName, methodName, DateTime.Now);
            if (input.File is null || input.File.Equals(0))
                throw new Exception("File not found.");

            //ตรวจสอบชื่อไฟล์ นำเข้าซ้ำ
            string fileNameBackup = Path.GetFileName(input.File.FileName);
            var fileNameCheuqe = await _dBContext.BatchOutPutHeaders.FirstOrDefaultAsync(h => h.IsActive == true && h.FileName == fileNameBackup);
            if (fileNameCheuqe != null)
                throw new Exception("File name is Duplicate.");

            //select last name file
            var fileExtension = Path.GetExtension(input.File.FileName);
            if (fileExtension.ToLower() != ".txt")
                throw new Exception("File extension is invalid");

            var pathBackup = _cheuqeSetting.Value.Path;
            if (!Directory.Exists(pathBackup))
                Directory.CreateDirectory(pathBackup);

            _logger.Debug("[{ServiceName}][{FunctionName}] - Create file backup.", _serviceName, methodName);
            //set path
            var fullBackupPath = Path.Combine(pathBackup, fileNameBackup);

            //ตรวจสอบว่าเคย backup หรือยัง
            if (File.Exists(fullBackupPath))
                throw new Exception("File already exists in the backup directory.");

            // บันทึกไฟล์ลง disk
            using (var stream = new FileStream(fullBackupPath, FileMode.Create))
            {
                await input.File.CopyToAsync(stream);
            }

            //Encoding encoding = Encoding.GetEncoding("windows-874"); // ANSI ภาษาไทย

            //string[] allLines = File.ReadAllLines(fullBackupPath, encoding);

            //await ReadBatchFileNoToUpsertFunction(allLines, fileNameBackup);

            var dto = new BatchOutputInsertResponseDTO
            {
                Message = "successfully."
            };
            return dto;
        }

        public async Task<GetBatchOutputHeaderResponseDTO> GetBatchOutputHeader()
        {
            var methodName = nameof(GetBatchOutputHeader);
            _logger.Debug("[{ServiceName}][{MethodName}] - Start date: {Date}", _serviceName, methodName, DateTime.Now);
            var data = await _dBContext.BatchOutPutHeaders.Where(_ => _.IsActive == true).OrderByDescending(_ => _.BatchOutPutHeaderId).FirstOrDefaultAsync();

            var dataFile = data.FileName.Substring(34, 12);

            var result = new GetBatchOutputHeaderResponseDTO
            {
                BatchOutPutHeaderId = data.BatchOutPutHeaderId,
                FileName = data.FileName,
                CreatedDate = data.CreatedDate,
                Data = dataFile
            };
            return result;
        }

        #region Function

        private async Task<BatchOutputDetailDTO> ParseHeaderLine(string line)
        {
            return await Task.FromResult(new BatchOutputDetailDTO
            {
                RecordType = line.Substring(0, 1),
                CompanyAbbreviation = line.Substring(1, 10),
                CompanyName = line.Substring(11, 100),
                Address1 = line.Substring(111, 70),
                Address2 = line.Substring(181, 70),
                Address3 = line.Substring(251, 70),
                PayeeAccountNumber = line.Substring(321, 20),
                PayeeTaxIdNumber = line.Substring(341, 15),
                SocialSecurityId = line.Substring(356, 15),
                BatchData = line
            });
        }

        private async Task<BatchOutputDetailDTO> ParseDetailLine(string line)
        {
            return await Task.FromResult(new BatchOutputDetailDTO
            {
                RecordType = line.Substring(0, 1),
                Sequence = line.Substring(1, 6),
                PaymentRefNo1 = line.Substring(7, 20),
                ChequeEffectiveDate = line.Substring(27, 8),
                ChequeNumber = line.Substring(35, 10),
                PayeeName = line.Substring(45, 100),
                WithholdingTaxAmount = line.Substring(145, 20),
                NetCheque = line.Substring(165, 20),
                ChequeStatus = line.Substring(185, 5),
                TransactionDate = line.Substring(190, 8),
                OutwardDate = line.Substring(198, 8),
                BatchData = line
            });
        }

        private async Task<BatchOutputDetailDTO> ParseFooterLine(string line)
        {
            return await Task.FromResult(new BatchOutputDetailDTO
            {
                RecordType = line.Substring(0, 1),
                TotalRecords = line.Substring(1, 6),
                BatchData = line
            });
        }

        private async Task ReadBatchFileNoToUpsertFunction(string[] data, string fileName)
        {
            var methodName = nameof(ReadBatchFileNoToUpsertFunction);
            var allLines = data;
            // แยกข้อมูล
            BatchOutputDetailDTO headerLine = new BatchOutputDetailDTO();
            List<BatchOutputDetailDTO> detailLines = new List<BatchOutputDetailDTO>();
            BatchOutputDetailDTO footerLine = new BatchOutputDetailDTO();

            // ตรวจสอบ error
            List<string> errorList = new List<string>();

            for (int i = 0; i < allLines.Length; i++)
            {
                string line = allLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                char recordType = line[0]; // ตัวแรกของบรรทัด = Record Type เช่น 'H', 'D', 'T'
                int lineNumber = i + 1;

                // ตรวจสอบความยาวของบรรทัดตามประเภทบันทึก
                if (recordType == 'H' || recordType == 'D' || recordType == 'T')
                {
                    if (recordType == 'H' && line.Length != 371)
                    {
                        throw new Exception("Invalid Format Header");
                    }

                    if (recordType == 'D' && line.Length != 206)
                    {
                        throw new Exception("Invalid Format Detail");
                    }

                    if (recordType == 'T' && line.Length != 7)
                    {
                        throw new Exception("Invalid Format Footer");
                    }
                }
                else
                {
                    throw new Exception("recordType Not Found");
                }

                switch (recordType)
                {
                    case 'H':
                        headerLine = await ParseHeaderLine(line);
                        break;

                    case 'D':
                        detailLines.Add(await ParseDetailLine(line));
                        break;

                    case 'T':
                        footerLine = await ParseFooterLine(line);
                        break;

                    default:
                        errorList.Add($"[Line {lineNumber}] Unknown record type: {recordType}");
                        break;
                }
            }

            var batchOutPutHeaderInsert = new BatchOutPutHeader
            {
                ItemCount = detailLines.Count,
                SumAmount = detailLines.Sum(d => decimal.TryParse(d.NetCheque, out var amount) ? amount : 0),
                IsActive = true,
                CreatedByUserId = 1,
                CreatedDate = DateTime.Now,
                UpdatedByUserId = 1,
                UpdatedDate = DateTime.Now,
                FileName = fileName,
            };

            var headerInsert = _mapper.Map<BatchOutPutDetail>(headerLine);
            var detailInsert = _mapper.Map<List<BatchOutPutDetail>>(detailLines);
            var footerInsert = _mapper.Map<BatchOutPutDetail>(footerLine);

            if (allLines.Length != (detailLines.Count + 2))
            {
                throw new Exception("Data not Match");
            }

            using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Insert BatchControl
                    _dBContext.BatchOutPutHeaders.Add(batchOutPutHeaderInsert);
                    await _dBContext.SaveChangesAsync();
                    int batchOutPutHeaderId = batchOutPutHeaderInsert.BatchOutPutHeaderId;

                    _dBContext.BatchOutPutDetails.Add(headerInsert);
                    headerInsert.BatchOutPutHeaderId = batchOutPutHeaderId; // Set foreign key
                    headerInsert.DataType = "H"; // Set record type for header
                    headerInsert.IsActive = true;
                    headerInsert.CreatedByUserId = 1;
                    headerInsert.CreatedDate = DateTime.Now;
                    headerInsert.UpdatedByUserId = 1;
                    headerInsert.UpdatedDate = DateTime.Now;

                    _dBContext.BatchOutPutDetails.AddRange(detailInsert);
                    foreach (var detail in detailInsert)
                    {
                        detail.BatchOutPutHeaderId = batchOutPutHeaderId; // Set foreign key
                        detail.DataType = "D"; // Set record type for detail
                        detail.IsActive = true;
                        detail.CreatedByUserId = 1;
                        detail.CreatedDate = DateTime.Now;
                        detail.UpdatedByUserId = 1;
                        detail.UpdatedDate = DateTime.Now;
                    }

                    _dBContext.BatchOutPutDetails.AddRange(footerInsert);
                    footerInsert.BatchOutPutHeaderId = batchOutPutHeaderId; // Set foreign key
                    footerInsert.DataType = "F"; // Set record type for footer
                    footerInsert.IsActive = true;
                    footerInsert.CreatedByUserId = 1;
                    footerInsert.CreatedDate = DateTime.Now;
                    footerInsert.UpdatedByUserId = 1;
                    footerInsert.UpdatedDate = DateTime.Now;

                    //Match file to insert
                    var detailOutputCount = detailInsert.Count;
                    var prefixOutPut = detailInsert.Select(p => p.PaymentRefNo1).ToList();
                    var tmpDetialInsert = _dBContext.ChequeDetails.AsNoTracking();

                    string[] formats = { "ddMMyyyy", "yyyy-MM-dd" };
                    int countSuccess = 0;
                    int countFail = 0;
                    // Insert BatchOutPutDetails
                    foreach (var detail in detailInsert)
                    {
                        var prefixCode = detail.PaymentRefNo1;
                        var tmpOutPut = tmpDetialInsert.FirstOrDefault(d => d.Prefix == prefixCode && d.IsActive == true);

                        if (tmpOutPut != null)//throw new Exception($"Prefix: {prefixCode} is not found");
                        {
                            tmpOutPut.ChequeEffectiveDate = string.IsNullOrEmpty(detail?.ChequeEffectiveDate)
                                                  ? null
                                                  : DateTime.ParseExact(detail.ChequeEffectiveDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                            tmpOutPut.ChequeNumber = detail?.ChequeNumber;
                            tmpOutPut.NetCheque = string.IsNullOrEmpty(detail?.NetCheque) ? 0 : Convert.ToDecimal(detail?.NetCheque);
                            tmpOutPut.PayeeName = detail?.PayeeName;
                            tmpOutPut.WithholdingTaxAmount = string.IsNullOrEmpty(detail?.WithholdingTaxAmount) ? 0 : Convert.ToDecimal(detail?.WithholdingTaxAmount);
                            tmpOutPut.ChequeStatus = detail?.ChequeStatus;
                            tmpOutPut.TransactionDate = string.IsNullOrEmpty(detail?.TransactionDate)
                                                  ? null
                                                  : DateTime.ParseExact(detail.TransactionDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                            tmpOutPut.OutwardDate = string.IsNullOrEmpty(detail?.OutwardDate)
                                                  ? null
                                                  : DateTime.ParseExact(detail.OutwardDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);

                            tmpOutPut.UpdatedDate = DateTime.Now;
                            tmpOutPut.UpdatedByUserId = 1;
                            countSuccess++;

                            _dBContext.ChequeDetails.Update(tmpOutPut);
                        }
                        else
                        {
                            countFail++;
                        }
                    }
                    _logger.Information("[{ServiceName}][{MethodName}] - Update success count: {CountSuccess} , Fail: {CountFail}", _serviceName, methodName, countSuccess, countFail);
                    if (countSuccess == 0) throw new Exception("Prefix not match all.");

                    await _dBContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "[{FunctionName}] - Error occurred during transaction", methodName);
                    await transaction.RollbackAsync();
                    throw new Exception(e.Message);
                }
            }
        }

        #endregion Function
    }
}