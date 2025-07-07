#nullable disable

namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class DetailChequeResponseDTO
    {
        public string RecordType { get; set; }
        public string PaymentRefNo1 { get; set; }
        public string PaymentRefNo2 { get; set; }
        public string PaymentRefNo3 { get; set; }
        public string SupplierRefNo { get; set; }
        public string PayType { get; set; }
        public string PayeeName { get; set; }
        public string PayeeIdCardNo { get; set; }
        public string PayeeAddress1 { get; set; }
        public string PayeeAddress2 { get; set; }
        public string PayeeAddress3 { get; set; }
        public string PostCode { get; set; }
        public string PayeeBankCode { get; set; }
        public string PayeeBankAccountNo { get; set; }
        public string EffectiveDate { get; set; }
        public string InvoiceAmount { get; set; }
        public string TotalVATAmount { get; set; }
        public string VATPercent { get; set; }
        public string TotalWHTAmount { get; set; }
        public string TotalTaxableAmount { get; set; }
        public string TotalDiscountAmount { get; set; }
        public string NetChequeTransferAmount { get; set; }
        public string DeliveryMethod { get; set; }
        public string PickupchequeLocation { get; set; }
        public string ChequeNumber { get; set; }
        public string ChequeStatus { get; set; }
        public string ChequeStatusDate { get; set; }
        public string NotificationMethod { get; set; }
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public string FAXNumber { get; set; }
        public string DateReturnChequetoCompany { get; set; }
        public string ReturnChequeMethod { get; set; }
        public string StrikethroughFlag { get; set; }
        public string AccountPayeeOnlyFlag { get; set; }
        public string AcknowledgementDocumentNotify { get; set; }
        public string PrintLocation { get; set; }
        public string FileBatchNoBankReference { get; set; }
        public string KTBRef { get; set; }
        public string ForKTBSystem { get; set; }
        public string FreeFiller { get; set; }
        public string CarriageReturn { get; set; }
        public string EndofLine { get; set; }
    }
}