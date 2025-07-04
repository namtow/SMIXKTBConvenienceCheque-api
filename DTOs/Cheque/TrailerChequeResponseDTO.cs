namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class TrailerChequeResponseDTO
    {
        public string RecordType { get; set; }
        public string BatchNo { get; set; }
        public string TotalPaymentRecord { get; set; }
        public string TotalPaymentAmount { get; set; }
        public string TotalWHTRecord { get; set; }
        public string TotalWHTAmount { get; set; }
        public string TotalInvoiceRecord { get; set; }
        public string TotalInvoiceNetAmount { get; set; }
        public string TotalMailRecord { get; set; }
        public string FileBatchNoBankReference { get; set; }
        public string Filler { get; set; }
        public string CarriageReturn { get; set; }
        public string EndofLine { get; set; }
    }
}