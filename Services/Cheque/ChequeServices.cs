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

        public async Task<ServiceResponse<FileResponseDTO>> CreateFileCheque()
        {
            var methodName = nameof(ChequeServices);
            try
            {
                _logger.Debug("[{ServiceName}][{MethodName}] - Start create file text date: {Date}", _serviceName, methodName, DateTime.Now);
                //PadRight >> เติมช่องว่างทางขวา
                //PadRight >> เติมช่องว่างทางซ้าย
                var batchNoGen = Guid.NewGuid().ToString().Substring(0, 23).Replace("-", "");
                var effectiveDate = "08072025";
                var fileNo = 4;
                var batchNo = 1;

                var tmpDetail = await _dBContext.TmpImportClaims.Where(x => x.fileNo == fileNo && x.batchNo == batchNo)
                  //.Take(4988)
                  .OrderByDescending(a => a.ApplicationCode).ThenBy(a => a.seqNo)
                  .ToListAsync();

                if (tmpDetail.Count == 0)
                    throw new Exception("Data import detail Not found.");

                var amountTotal = tmpDetail.Sum(t => t.PayTotal);
                var amountPayment = amountTotal == null ? "0.00" : amountTotal.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);
                int tmpCount = tmpDetail.Count;
                var countTotal = tmpDetail.Count.ToString();

                #region select header

                //Header data
                var header = new HeaderChequeResponseDTO
                {
                    RecordType = "H".PadRight(1),
                    PayerAbbreviation = "SSIN".PadRight(10),
                    PayerName = "บริษัท สยามสไมล์ ประกันภัย จำกัด (มหาชน)".PadRight(100),
                    PayerAddress1 = "".PadRight(70),
                    PayerAddress2 = "".PadRight(70),
                    PayerAddress3 = "".PadRight(70),
                    PostCode = "12130".PadRight(5),
                    PayerAccountNo = "1526007770".PadRight(20),
                    PayerTaxID = "0107555000538".PadRight(15),
                    PayerSocialSecurity = "107555000538".PadRight(15),
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

                //Detail data
                var details = tmpDetail.Select(d => new DetailChequeResponseDTO
                {
                    RecordType = "D".PadRight(1),
                    PaymentRefNo1 = $"{d.ApplicationCode}{d.seqNo.ToString().PadLeft(4, '0')}".PadRight(20),
                    PaymentRefNo2 = d.BranchId.ToString().PadRight(20),
                    PaymentRefNo3 = d.ClaimNo.Replace("-", "").PadRight(20),
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

                //add list date to gen file text
                var finalList = new List<string> { stringHeader };
                finalList.AddRange(stringdetail);
                finalList.Add(stringTrailer);

                //call function insert
                var batchControlId = await InsertBatchControll(tmpCount);

                await InsertBatchHeader(tmpDetail, batchControlId);
                await InsertBatchDetail(batchControlId, header, stringHeader, details, trailer, stringTrailer);
                await InsertDetail(batchControlId, tmpDetail);
                //create file for function
                var resultBytes = CreateTextFile(finalList);

                var dataOut = new FileResponseDTO
                {
                    Data = resultBytes,
                    IsResult = true,
                    Message = "Success",
                    FileName = $"SSIN EX_Kcorp_ConChq_{fileNo}_{batchNo}_{countTotal}_{amountTotal}.txt"
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

        private async Task<int> InsertBatchControll(int count)
        {
            var now = DateTime.Now;
            await using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var controlInsert = new BatchControl();

                    controlInsert.IsActive = true;
                    controlInsert.BatchControlType = 1; //in
                    controlInsert.ItemCount = count;
                    controlInsert.CreatedDate = now;
                    controlInsert.CreatedByUserId = 1;
                    controlInsert.UpdatedDate = now;
                    controlInsert.UpdatedByUserId = 1;

                    _dBContext.Add(controlInsert);
                    await _dBContext.SaveChangesAsync();
                    var controlId = controlInsert.BatchControlId;
                    await transaction.CommitAsync();

                    return controlId;
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            }
        }

        private async Task InsertBatchHeader(List<TmpImportClaim> data, int id)
        {
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

            await using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                try
                {
                    List<BatchHeader> batchHeaders = new();

                    foreach (var item in tmpHeader)
                    {
                        BatchHeader headerInsert = new();
                        headerInsert.BatchControlId = id;
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
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            }
        }

        private async Task InsertBatchDetail(int id, HeaderChequeResponseDTO header, string headerString, List<DetailChequeResponseDTO> listDetail, TrailerChequeResponseDTO footer, string footerString)
        {
            var now = DateTime.Now;
            await using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var finalBatchDetail = new List<BatchDetail>();
                    //insert header
                    var mapHeader = _mapper.Map<BatchDetail>(header);
                    mapHeader.BatchControlId = id;
                    mapHeader.IsActive = true;
                    mapHeader.CreatedDate = now;
                    mapHeader.CreatedByUserId = 1;
                    mapHeader.UpdatedDate = now;
                    mapHeader.UpdatedByUserId = 1;
                    mapHeader.DataType = "H";
                    mapHeader.BatchData = headerString;

                    finalBatchDetail.Add(mapHeader);

                    //var trimListDetail = listDetail.TrimExcess();
                    //insert loop detail
                    foreach (var item in listDetail)
                    {
                        var propsdetail = item.GetType()
                                        .GetProperties()
                                        .OrderBy(p => p.MetadataToken);
                        var detailLine = string.Concat(propsdetail.Select(p => p.GetValue(item)));

                        var mapBatchDetail = _mapper.Map<BatchDetail>(item);

                        mapBatchDetail.BatchControlId = id;
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
                    mapTrailer.BatchControlId = id;
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
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            }
        }

        private async Task InsertDetail(int id, List<TmpImportClaim> data)
        {
            await using (var transaction = await _dBContext.Database.BeginTransactionAsync())
            {
                var now = DateTime.Now;
                try
                {
                    List<Detail> finalDetail = new();
                    foreach (var item in data)
                    {
                        Detail mapDetail = new();
                        //insert detail
                        mapDetail.BatchControlId = id;
                        mapDetail.AppId = item.ApplicationCode;
                        mapDetail.ClaimNo = item.ClaimNo;
                        mapDetail.Prefix = $"{item.ApplicationCode}{item.seqNo.ToString().PadLeft(4, '0')}";
                        mapDetail.IsActive = true;
                        mapDetail.CreatedDate = now;
                        mapDetail.CreatedByUserId = 1;
                        mapDetail.UpdatedDate = now;
                        mapDetail.UpdatedByUserId = 1;

                        finalDetail.Add(mapDetail);
                    }
                    _dBContext.Details.AddRange(finalDetail);
                    await _dBContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            }
        }

        #endregion Function
    }
}