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
using Quartz.Core;

namespace SMIXKTBConvenienceCheque.Services.Cheque
{
    public class ChequeServices : IChequeServices
    {
        private readonly AppDBContext _dBContext;
        private readonly Serilog.ILogger _logger;
        private readonly string _serviceName = nameof(ChequeServices);

        public ChequeServices(AppDBContext dBContext)
        {
            _dBContext = dBContext;
            _logger = Log.ForContext<ChequeServices>();
        }

        public async Task<ServiceResponse<FileResponseDTO>> CreateFileCheque()
        {
            try
            {
                //PadRight >> เติมช่องว่างทางขวา
                //PadRight >> เติมช่องว่างทางซ้าย
                var batchNoGen = Guid.NewGuid().ToString().Substring(0, 20);
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
                    PayerACNo = "1526007770".PadRight(20),
                    PayerTaxID = "0107555000538".PadRight(15),
                    PayerSocialSecurity = "107555000538".PadRight(15),
                    EffectiveDate = "07072025".PadRight(8),//DateTime.Now.ToString("dd/MM/yyyy")
                    BatchNo = batchNoGen.PadRight(35),
                    BankReference = "".PadRight(25),
                    KTB = "".PadRight(5),
                    Filler = "".PadRight(1049),
                    //CarriageReturn = "".PadRight(1),
                    //EndofLine = "".PadRight(1)
                };

                var propsHeaader = header.GetType()
                       .GetProperties()
                       .OrderBy(p => p.MetadataToken); // รักษาลำดับฟิลด์ตามประกาศ

                var stringHeader = string.Concat(propsHeaader.Select(p => p.GetValue(header)?.ToString() ?? ""));

                var tmpDetail = await _dBContext.TmpImportClaims.Where(x => x.fileNo == 1).Take(10).OrderBy(a => a.ApplicationCode).ThenBy(a => a.seqNo).ToListAsync();

                //Detail data
                var details = tmpDetail.Select(d => new DetailChequeResponseDTO
                {
                    RecordType = "D".PadRight(1),
                    PaymentRefNo1 = $"{d.ApplicationCode}0000{d.seqNo.ToString()}".PadRight(20),
                    PaymentReferenceNo2 = "".PadRight(20),
                    PaymentReferenceNo3 = "".PadRight(20),
                    SupplierReferenceNo = "".PadRight(15),
                    PayType = "C".PadRight(1),
                    PayeeName = d.CustName.PadRight(100),
                    PayeeIdCardNo = "".PadRight(15),
                    PayeeAddress1 = "-".PadRight(70),
                    PayeeAddress2 = "-".PadRight(70), //ถ้าไม่มี fix (-)
                    PayeeAddress3 = "".PadRight(70),
                    PostCode = d.ZipCode.PadRight(5),
                    PayeeBankCode = "".PadRight(3),
                    PayeeBankACNo = "".PadRight(20),
                    EffectiveDate = "07072025".PadRight(8),
                    //จำนวนเงินชิดขวา
                    InvoiceAmount = "".PadLeft(20),
                    TotalVATAmount = "".PadLeft(20),
                    VATPercent = "".PadLeft(5),
                    TotalWHTAmount = "".PadLeft(20),
                    TotalTaxableAmount = "".PadLeft(20),
                    TotalDiscountAmount = "".PadLeft(20),
                    NetChequeTrAmount = d.PayTotal == null ? "".PadLeft(20)
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

                //var amount = 23980;
                //var format = amount.ToString("#,##0.00", CultureInfo.InvariantCulture);
                var amountTotal = tmpDetail.Sum(t => t.PayTotal);
                var amountPayment = amountTotal == null ? "0.00" : amountTotal.Value.ToString("#,##0.00", CultureInfo.InvariantCulture);
                var count = tmpDetail.Count.ToString();

                //Trailer
                var trailer = new TrailerChequeResponseDTO
                {
                    RecordType = "T".PadRight(1),
                    BatchNo = batchNoGen.PadRight(35),
                    TotalPaymentRecord = count.PadLeft(15), //record ต้องชิดขวา
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

                //var finalList = new List<string> { stringHeader, stringdetail, stringTrailer };

                var finalList = new List<string> { stringHeader };
                finalList.AddRange(stringdetail);
                finalList.Add(stringTrailer);

                var resultBytes = CreateTextFile(finalList);

                var dataOut = new FileResponseDTO
                {
                    //todo: db
                    Data = resultBytes,
                    IsResult = true,
                    Message = "Success",
                    FileName = "SSIN EX_Kcorp_ConChq.txt"
                };

                return ResponseResult.Success(dataOut);
            }
            catch (Exception e)
            {
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

        public byte[] CreateTextFile(List<string> lines)
        {
            var methodName = nameof(CreateTextFile);
            _logger.Debug("[{FunctionName}] - Generating file text", methodName);
            // Create a UTF-8 encoding object with BOM
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

        //public byte[] CreateTextFile(List<string> lines)
        //{
        //    var methodName = nameof(CreateTextFile);
        //    _logger.Debug("[{FunctionName}] - Generating file text", methodName);

        //    var encoding = Encoding.GetEncoding(874); // TIS-620 (Windows-874)

        //    using var memoryStream = new MemoryStream();
        //    using (var writer = new StreamWriter($"D:\\Documents\\TEST0001.txt", false, encoding))
        //    {
        //        foreach (var line in lines)
        //        {
        //            writer.WriteLine(line);
        //        }
        //        writer.Flush(); // สำคัญมาก: ต้อง flush ก่อนอ่านจาก memoryStream
        //    }

        //    return memoryStream.ToArray(); // ส่งออกเป็น byte[]
        //}

        #endregion Function
    }
}