using SMIXKTBConvenienceCheque.Data;
using Serilog;
using SMIXKTBConvenienceCheque.DTOs.Cheque;
using SMIXKTBConvenienceCheque.Models;
using System.Globalization;
using System.Text;
using System;
using System.Linq.Dynamic.Core;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace SMIXKTBConvenienceCheque.Services.Cheque
{
    public class ChequeServices : IChequeServices
    {
        private readonly AppDBContext _dBContext;
        private readonly IMapper _mapper;
        private readonly Serilog.ILogger _logger;
        private readonly string _serviceName = nameof(ChequeServices);

        public ChequeServices(AppDBContext dBContext, IMapper mapper)
        {
            _dBContext = dBContext;
            _mapper = mapper;
            _logger = Log.ForContext<ChequeServices>();
        }

        public async Task<ServiceResponse<FileResponseDTO>> CreateFileCheque(FileChequeResponseDTO req)
        {
            //PadRight >> เติมช่องว่างทางขวา
            //PadRight >> เติมช่องว่างทางซ้าย
            var methodName = nameof(ChequeServices);
            try
            {
                _logger.Debug("[{ServiceName}][{MethodName}] - Start create file text date: {Date}", _serviceName, methodName, DateTime.Now);

                var batchNoGen = Guid.NewGuid().ToString().Substring(0, 23).Replace("-", "");
                //format to string
                var effectiveDate = req.EffectiveDate.ToString("ddMMyyyy");
                var uploadDate = req.UploadDate.Value.ToString("ddMMyyyy");

                var tmpDetail = _dBContext.TmpImportClaims.AsNoTracking();

                if (req.FileNo != 0)
                    tmpDetail = tmpDetail.Where(f => f.FileNo == req.FileNo);

                if (req.BatchNo != 0)
                    tmpDetail = tmpDetail.Where(b => b.BatchNo == req.BatchNo);

                //select data to insert
                var importClaim = await tmpDetail
                    .OrderBy(a => a.SchoolRunning)
                    .ThenBy(a => a.SeqNo)
                    .AsNoTracking().ToListAsync();

                if (importClaim.Count == 0)
                    throw new Exception("Data import detail Not found.");

                //set data
                var amountTotal = importClaim.Sum(t => t.PayTotal);
                var amountPayment = amountTotal == null ? "0.00" : amountTotal.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);
                int tmpCount = importClaim.Count;
                var countTotal = importClaim.Count.ToString();

                #region select header

                _logger.Debug("[{ServiceName}][{MethodName}] - Select header", _serviceName, methodName);
                //Header data
                var header = new HeaderChequeResponseDTO
                {
                    RecordType = "H".PadRight(1),
                    PayerAbbreviation = "SSIN".PadRight(10),
                    PayerName = "บริษัท สยามสไมล์ ประกันภัย จำกัด (มหาชน)".PadRight(100),
                    PayerAddress1 = "".PadRight(70),
                    PayerAddress2 = "".PadRight(70),
                    PayerAddress3 = "".PadRight(70),
                    PostCode = "".PadRight(5),
                    PayerAccountNo = "4756008690".PadRight(20),
                    PayerTaxID = "0107555000538".PadRight(15),
                    PayerSocialSecurity = "".PadRight(15),
                    EffectiveDate = effectiveDate.PadRight(8),
                    BatchNo = batchNoGen.PadRight(35),
                    FileBatchNoBankReference = "".PadRight(25),
                    KTBRef = "".PadRight(5),
                    Filler = "".PadRight(1049),
                    //CarriageReturn = "".PadRight(1),
                    //EndofLine = "".PadRight(1)
                };

                var propsHeaader = header.GetType()
                       .GetProperties()
                       .OrderBy(p => p.MetadataToken); // รักษาลำดับฟิลด์ตามประกาศ

                var stringHeader = string.Concat(propsHeaader.Select(p => p.GetValue(header)?.ToString() ?? ""));

                #endregion select header

                #region select detial

                _logger.Debug("[{ServiceName}][{MethodName}] - Select Detail", _serviceName, methodName);

                //Detail data
                var details = importClaim.Select(d => new DetailChequeResponseDTO
                {
                    RecordType = "D".PadRight(1),
                    PaymentRefNo1 = $"{d.SchoolRunning}{d.SeqNo.ToString().PadLeft(4, '0')}".PadRight(20),
                    PaymentRefNo2 = d.Reference2 == null ? "".PadRight(20) : d.Reference2.Replace("-", "").PadRight(20),
                    PaymentRefNo3 = d.Reference3 == null ? "".PadRight(20) : d.Reference3.Replace("-", "").PadRight(20),
                    SupplierRefNo = "".PadRight(15),
                    PayType = "C".PadRight(1),
                    PayeeName = d.CustName.PadRight(100),
                    PayeeIdCardNo = "".PadRight(15),
                    PayeeAddress1 = "-".PadRight(70),
                    PayeeAddress2 = "-".PadRight(70), //ถ้าไม่มี fix (-)
                    PayeeAddress3 = "".PadRight(70),
                    PostCode = d.ZipCode.PadRight(5),
                    PayeeBankCode = "".PadRight(3),
                    PayeeBankAccountNo = "".PadRight(20),
                    EffectiveDate = effectiveDate.PadRight(8),
                    //จำนวนเงินชิดขวา
                    InvoiceAmount = "".PadLeft(20),
                    TotalVATAmount = "".PadLeft(20),
                    VATPercent = "".PadLeft(5),
                    TotalWHTAmount = "".PadLeft(20),
                    TotalTaxableAmount = "".PadLeft(20),
                    TotalDiscountAmount = "".PadLeft(20),
                    NetChequeTransferAmount = d.PayTotal == null ? "".PadLeft(20)
                                                            : d.PayTotal.Value.ToString("#,##0.00", CultureInfo.InvariantCulture).PadLeft(20),
                    DeliveryMethod = "CR".PadRight(2),
                    PickupchequeLocation = "700".PadRight(15),
                    ChequeNumber = "".PadRight(10),
                    ChequeStatus = "".PadRight(5),
                    ChequeStatusDate = "".PadRight(8),
                    NotificationMethod = "".PadRight(1),
                    MobileNumber = "".PadRight(20),
                    EmailAddress = "".PadRight(70),
                    FAXNumber = "".PadRight(20),
                    DateReturnChequetoCompany = "".PadRight(8),
                    ReturnChequeMethod = "".PadRight(3),
                    StrikethroughFlag = "1".PadRight(1),
                    AccountPayeeOnlyFlag = "1".PadRight(1),
                    AcknowledgementDocumentNotify = "".PadRight(200),
                    PrintLocation = "".PadRight(15),
                    FileBatchNoBankReference = "".PadRight(25),
                    KTBRef = "".PadRight(30),
                    ForKTBSystem = "".PadRight(151),
                    FreeFiller = "".PadRight(350),
                    //CarriageReturn = "".PadRight(1),
                    //EndofLine = "".PadRight(1),
                }).ToList();

                List<string> stringdetail = new();

                foreach (var d in details)
                {
                    var propsdetail = d.GetType()
                          .GetProperties()
                          .OrderBy(p => p.MetadataToken); // รักษาลำดับฟิลด์ตามประกาศ

                    var detailLine = string.Concat(propsdetail.Select(p => p.GetValue(d)));

                    stringdetail.Add(detailLine);
                }

                #endregion select detial

                #region select trailer

                _logger.Debug("[{ServiceName}][{MethodName}] - Select Trailer", _serviceName, methodName);

                //Trailer
                var trailer = new TrailerChequeResponseDTO
                {
                    RecordType = "T".PadRight(1),
                    BatchNo = batchNoGen.PadRight(35),
                    TotalPaymentRecord = countTotal.PadLeft(15), //record ต้องชิดขวา
                    TotalPaymentAmount = amountPayment.PadLeft(20),
                    TotalWHTRecord = "0".PadLeft(15),
                    TotalWHTAmount = "0.00".PadLeft(20),
                    TotalInvoiceRecord = "0".PadLeft(15),
                    TotalInvoiceNetAmount = "00.00".PadLeft(20),
                    TotalMailRecord = "0".PadLeft(15),
                    FileBatchNoBankReference = "0".PadRight(25),
                    Filler = "0".PadRight(1317),
                    //CarriageReturn = "".PadRight(1),
                    //EndofLine = "".PadRight(1),
                };
                var propstrailer = trailer.GetType()
                      .GetProperties()
                      .OrderBy(p => p.MetadataToken); // รักษาลำดับฟิลด์ตามประกาศ

                var stringTrailer = string.Concat(propstrailer.Select(p => p.GetValue(trailer)?.ToString() ?? ""));

                #endregion select trailer

                //call function insert ป้องกันการ insert ไม่สำเร็จ
                await using var transaction = await _dBContext.Database.BeginTransactionAsync();
                try
                {
                    _logger.Debug("[{ServiceName}][{MethodName}] - Call function to upsert", _serviceName, methodName);

                    var resultId = await InsertBatchControll(tmpCount, amountTotal, req.FileNo, req.BatchNo, req.EffectiveDate, req.UploadDate);

                    await InsertBatchHeader(importClaim, resultId.BatchFileNoId);
                    await InsertBatchDetail(resultId.BatchFileNoId, header, stringHeader, details, trailer, stringTrailer);
                    await InsertChequeDetail(resultId, importClaim);

                    await transaction.CommitAsync(); //Commit หลังทำครบทุกเมธอด
                }
                catch (Exception e)
                {
                    _logger.Error(e, "[{FunctionName}] - Error occurred during transaction", methodName);
                    await transaction.RollbackAsync();
                    throw e;
                }
                //add list date to gen file text
                var finalList = new List<string> { stringHeader };
                finalList.AddRange(stringdetail);
                finalList.Add(stringTrailer);

                //create file for function
                var resultBytes = CreateTextFile(finalList);

                var dataOut = new FileResponseDTO
                {
                    Data = resultBytes,
                    IsResult = true,
                    Message = "Success",
                    FileName = $"SSIN EX_Kcorp_ConChq_{uploadDate}_{req.FileNo}_{req.BatchNo}_{countTotal}_{amountTotal}.txt"
                };

                return ResponseResult.Success(dataOut);
            }
            catch (Exception e)
            {
                _logger.Error(e, "[{ServiceName}][{MethodName}] - An Error occurred , {Msg}", _serviceName, methodName, e.Message);
                return ResponseResult.Failure<FileResponseDTO>(e.Message);
            }
        }

        #region Function

        //public byte[] CreateTextFile(List<string> data)
        //{
        //    var methodName = nameof(CreateTextFile);
        //    //create list string to create file text
        //    //List<string> listDataFiles = new List<string>();

        //    _logger.Debug("[{FunctionName}] - Add data :{@fileName} to list", methodName);
        //    // Add the mapped data to the list IS
        //    //foreach (var dto in data)
        //    //{
        //    //    // Assuming the DTO has a meaningful ToString() method
        //    //    listDataFiles.Add(dto.XMLDetail.ToString());
        //    //}

        //    var localPathFile = $"D:\\Documents\\test01.txt";

        //    // Create a UTF-8 encoding object with BOM
        //    Encoding utf8EncodingWithBom = new UTF8Encoding(true);
        //    using var writer = new StreamWriter(File.Create(localPathFile), utf8EncodingWithBom);

        //    //_logger.Information("[{}] - Writing Input File... {@fileName}", methodName, fileName);
        //    foreach (var item in data)
        //    {
        //        writer.WriteLine(item);
        //    }
        //    writer.Dispose();

        //    var a = File.ReadAllBytes(localPathFile);

        //    return a;
        //}
        /// <summary>
        /// Fucntion create file text For ANSI
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public byte[] CreateTextFile(List<string> lines)
        {
            var methodName = nameof(CreateTextFile);
            _logger.Debug("[{FunctionName}] - Generating file text", methodName);
            //var encoding = new UTF8Encoding(true);
            //var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true); // UTF-8 with BOM อ่่านไทยถูกต้องไม่เพี้ยน
            var encoding = Encoding.GetEncoding(874); // หรือ "windows-874"

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, encoding);

            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
            writer.Dispose();
            //writer.Flush(); // Ensure all data is written to the stream
            return memoryStream.ToArray(); // Return byte[] directly
        }

        private async Task<InsertBatchControllResponseDTO> InsertBatchControll(int count, decimal? sum, int? fileNo, int? batchNo, DateTime? effectiveDate, DateTime? uploadDate)
        {
            var methodName = nameof(InsertBatchControll);

            _logger.Debug("[{FunctionName}] - Upsert BatchControl", methodName);

            var now = DateTime.Now;

            //ตรวจสอบว่าเคยนำเข้ารึยัง
            var checkControl = await _dBContext.BatchControls
                .FirstOrDefaultAsync(c => c.IsActive == true && c.FileNo == fileNo);

            int controlId;
            var controlInsert = new BatchControl();

            if (checkControl == null)
            {
                controlInsert.IsActive = true;
                controlInsert.ItemCount = count;
                controlInsert.SumAmount = sum;
                controlInsert.CreatedDate = now;
                controlInsert.CreatedByUserId = 1;
                controlInsert.UpdatedDate = now;
                controlInsert.UpdatedByUserId = 1;
                controlInsert.FileNo = fileNo;
                controlInsert.EffectiveDate = effectiveDate;

                _dBContext.Add(controlInsert);
                await _dBContext.SaveChangesAsync();
            }
            else
            {
                if (checkControl.ItemCount > 10000)
                    throw new Exception($"FileNo: {fileNo} insert is completed. ");

                var checkBatch = await _dBContext.BatchFileNos.FirstOrDefaultAsync(b => b.IsActive == true && b.BatchControlId == checkControl.BatchControlId && b.BatchNo == batchNo);
                if (checkBatch != null)
                    throw new Exception($"FileNo: {fileNo} , BatchNo: {batchNo} is duplicate.");

                //update
                checkControl.ItemCount += count;
                checkControl.SumAmount += sum;
                checkControl.UpdatedDate = now;
                checkControl.UpdatedByUserId = 1;

                _dBContext.Update(checkControl);
            }

            controlId = checkControl == null ? controlInsert.BatchControlId
                        : checkControl.BatchControlId;

            _logger.Debug("[{FunctionName}] - Insert BatchFileNo", methodName);
            //insert batch file no
            BatchFileNo batchFileNo = new();
            batchFileNo.IsActive = true;
            batchFileNo.BatchControlId = controlId;
            batchFileNo.BatchNo = batchNo;
            batchFileNo.ItemCount = count;
            batchFileNo.SumAmount = sum;
            batchFileNo.UploadDate = uploadDate;
            batchFileNo.CreatedDate = now;
            batchFileNo.CreatedByUserId = 1;
            batchFileNo.UpdatedDate = now;
            batchFileNo.UpdatedByUserId = 1;

            _dBContext.Add(batchFileNo);

            await _dBContext.SaveChangesAsync();
            var batchFileNoId = batchFileNo.BatchFileNoId;

            return new InsertBatchControllResponseDTO
            {
                BatchControlId = controlId,
                BatchFileNoId = batchFileNoId
            };
        }

        private async Task InsertBatchHeader(List<TmpImportClaim> data, int id)
        {
            var methodName = nameof(InsertBatchHeader);
            _logger.Debug("[{FunctionName}] - Insert BatchHeader", methodName);

            var now = DateTime.Now;
            var tmpHeader = data
                .Where(h => h.PayTotal.HasValue)
                .GroupBy(a => a.ApplicationCode)
                .ToDictionary(g => g.Key,
                                g => new
                                {
                                    PayTotal = g.Sum(g => g.PayTotal),
                                    ItemCount = g.Count()
                                }
                                ).ToList();

            List<BatchHeader> batchHeaders = new();

            foreach (var item in tmpHeader)
            {
                BatchHeader headerInsert = new();
                headerInsert.BatchFileNoId = id;
                headerInsert.AppId = item.Key;
                headerInsert.ItemCount = item.Value.ItemCount;
                headerInsert.SumAmount = item.Value.PayTotal;
                headerInsert.IsActive = true;
                headerInsert.CreatedDate = now;
                headerInsert.CreatedByUserId = 1;
                headerInsert.UpdatedDate = now;
                headerInsert.UpdatedByUserId = 1;

                batchHeaders.Add(headerInsert);
            }
            _dBContext.BatchHeaders.AddRange(batchHeaders);
            await _dBContext.SaveChangesAsync();
        }

        private async Task InsertBatchDetail(int id, HeaderChequeResponseDTO header, string headerString, List<DetailChequeResponseDTO> listDetail, TrailerChequeResponseDTO footer, string footerString)
        {
            var methodName = nameof(InsertBatchDetail);
            _logger.Debug("[{FunctionName}] - Insert BatchDetail", methodName);
            var now = DateTime.Now;

            var finalBatchDetail = new List<BatchDetail>();
            //insert header
            var mapHeader = _mapper.Map<BatchDetail>(header);
            mapHeader.BatchFileNoId = id;
            mapHeader.IsActive = true;
            mapHeader.CreatedDate = now;
            mapHeader.CreatedByUserId = 1;
            mapHeader.UpdatedDate = now;
            mapHeader.UpdatedByUserId = 1;
            mapHeader.DataType = "H";
            mapHeader.BatchData = headerString;

            finalBatchDetail.Add(mapHeader);

            //insert loop detail
            foreach (var item in listDetail)
            {
                var propsdetail = item.GetType()
                                .GetProperties()
                                .OrderBy(p => p.MetadataToken);
                var detailLine = string.Concat(propsdetail.Select(p => p.GetValue(item)));

                var mapBatchDetail = _mapper.Map<BatchDetail>(item);

                mapBatchDetail.BatchFileNoId = id;
                mapBatchDetail.IsActive = true;
                mapBatchDetail.CreatedDate = now;
                mapBatchDetail.CreatedByUserId = 1;
                mapBatchDetail.UpdatedDate = now;
                mapBatchDetail.UpdatedByUserId = 1;
                mapBatchDetail.DataType = "D";
                mapBatchDetail.BatchData = detailLine;

                finalBatchDetail.Add(mapBatchDetail);
            }
            //insert trailer
            var mapTrailer = _mapper.Map<BatchDetail>(footer);
            mapTrailer.BatchFileNoId = id;
            mapTrailer.IsActive = true;
            mapTrailer.CreatedDate = now;
            mapTrailer.CreatedByUserId = 1;
            mapTrailer.UpdatedDate = now;
            mapTrailer.UpdatedByUserId = 1;
            mapTrailer.DataType = "F";
            mapTrailer.BatchData = footerString;

            finalBatchDetail.Add(mapTrailer);

            _dBContext.BatchDetails.AddRange(finalBatchDetail);

            await _dBContext.SaveChangesAsync();
        }

        private async Task InsertChequeDetail(InsertBatchControllResponseDTO id, List<TmpImportClaim> data)
        {
            var methodName = nameof(InsertChequeDetail);
            _logger.Debug("[{FunctionName}] - Insert ChequeDetail", methodName);

            var now = DateTime.Now;

            List<ChequeDetail> finalDetail = new();
            foreach (var item in data)
            {
                ChequeDetail mapDetail = new();
                //insert detail
                mapDetail.BatchControlId = id.BatchControlId;
                mapDetail.AppId = item.ApplicationCode;
                mapDetail.ClaimNo = item.ClaimNo;
                mapDetail.Prefix = $"{item.SchoolRunning}{item.SeqNo.ToString().PadLeft(4, '0')}";
                mapDetail.IsActive = true;
                mapDetail.CreatedDate = now;
                mapDetail.CreatedByUserId = 1;
                mapDetail.UpdatedDate = now;
                mapDetail.UpdatedByUserId = 1;
                mapDetail.BatchFileNoId = id.BatchFileNoId;

                finalDetail.Add(mapDetail);
            }
            _dBContext.ChequeDetails.AddRange(finalDetail);
            await _dBContext.SaveChangesAsync();
        }

        #endregion Function
    }
}