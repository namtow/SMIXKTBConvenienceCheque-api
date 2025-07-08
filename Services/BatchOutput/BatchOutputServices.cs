using AutoMapper;
using Serilog;
using SMIXKTBConvenienceCheque.Data;
using SMIXKTBConvenienceCheque.DTOs.BatchOutput;
using SMIXKTBConvenienceCheque.Models;
using System.Data;
using System.Text;

namespace SMIXKTBConvenienceCheque.Services.BatchOutput
{
    public class BatchOutputServices : IBatchOutputServices
    {
        private readonly AppDBContext _dBContext;
        private readonly IMapper _mapper;
        private readonly Serilog.ILogger _logger;
        private readonly string _serviceName = nameof(BatchOutputServices);

        public BatchOutputServices(AppDBContext dBContext, IMapper mapper)
        {
            _dBContext = dBContext;
            _mapper = mapper;
            _logger = Log.ForContext<BatchOutputServices>();
        }

        /// <summary>
        /// Inserts batch output data into the database.
        /// </summary>
        /// <returns>
        /// A <see cref="ServiceResponse{BatchOutputInsertResponseDTO}"/> containing the result of the operation.
        /// </returns>
        public async Task<BatchOutputInsertResponseDTO> BatchOutputInsert(BatchOutputInsertRequestDTO intput)
        {
            string filePath = intput.path;//@"D:\SMIxKTP_Change\DWN_SSIN133344_SSIN_Status-Change_202507040132 v2.txt";

            Encoding encoding = Encoding.GetEncoding("windows-874"); // ANSI ภาษาไทย

            // แยกข้อมูล
            BatchOutputDetailDTO headerLine = new BatchOutputDetailDTO();
            List<BatchOutputDetailDTO> detailLines = new List<BatchOutputDetailDTO>();
            BatchOutputDetailDTO footerLine = new BatchOutputDetailDTO();

            // ตรวจสอบ error
            List<string> errorList = new List<string>();

            string[] allLines = File.ReadAllLines(filePath, encoding);

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

            var batchControlInsert = new BatchControl
            {
                ItemCount = detailLines.Count,
                //BatchControlType = 2, // 2 out
                IsActive = true,
                CreatedByUserId = 1,
                CreatedDate = DateTime.Now,
                UpdatedByUserId = 1,
                UpdatedDate = DateTime.Now,
            };

            var headerInsert = _mapper.Map<BatchOutPutDetail>(headerLine);
            var detailInsert = _mapper.Map<List<BatchOutPutDetail>>(detailLines);
            var footerInsert = _mapper.Map<BatchOutPutDetail>(footerLine);

            if (allLines.Length != (detailLines.Count + 2))
            {
                throw new Exception("data not Match");
            }

            using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Insert BatchControl
                    _dBContext.BatchControls.Add(batchControlInsert);
                    await _dBContext.SaveChangesAsync();
                    int batchControlId = batchControlInsert.BatchControlId;

                    _dBContext.BatchOutPutDetails.Add(headerInsert);
                    //headerInsert.BatchControlId = batchControlId; // Set foreign key
                    headerInsert.DataType = "H"; // Set record type for header
                    headerInsert.IsActive = true;
                    headerInsert.CreatedByUserId = 1;
                    headerInsert.CreatedDate = DateTime.Now;
                    headerInsert.UpdatedByUserId = 1;
                    headerInsert.UpdatedDate = DateTime.Now;

                    _dBContext.BatchOutPutDetails.AddRange(detailInsert);
                    foreach (var detail in detailInsert)
                    {
                        //detail.BatchControlId = batchControlId; // Set foreign key
                        detail.DataType = "D"; // Set record type for detail
                        detail.IsActive = true;
                        detail.CreatedByUserId = 1;
                        detail.CreatedDate = DateTime.Now;
                        detail.UpdatedByUserId = 1;
                        detail.UpdatedDate = DateTime.Now;
                    }

                    _dBContext.BatchOutPutDetails.AddRange(footerInsert);
                    //footerInsert.BatchControlId = batchControlId; // Set foreign key
                    footerInsert.DataType = "F"; // Set record type for footer
                    footerInsert.IsActive = true;
                    footerInsert.CreatedByUserId = 1;
                    footerInsert.CreatedDate = DateTime.Now;
                    footerInsert.UpdatedByUserId = 1;
                    footerInsert.UpdatedDate = DateTime.Now;

                    // Insert BatchOutPutDetails
                    await _dBContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "[{FunctionName}] - Error occurred during transaction", _serviceName);
                    await transaction.RollbackAsync();
                    throw new Exception(e.Message);
                }
            }
            var dto = new BatchOutputInsertResponseDTO
            {
                Message = "successfully."
            };
            return dto;
        }

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
    }
}