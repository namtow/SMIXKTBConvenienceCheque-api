namespace SMIXKTBConvenienceCheque.DTOs.Report
{
    public class ChequeReportResponseDTO
    {
        public int? FileNo { get; set; }
        public int? BatchNo { get; set; }
        public string ClaimNo { get; set; }
        public string Prefix { get; set; }
        public string SchoolRunning { get; set; }
        public string SchoolName { get; set; }
        public DateTime? ChequeEffectiveDate { get; set; }
        public string ChequeNumber { get; set; }
        public string PayeeName { get; set; }
        public decimal? NetCheque { get; set; }
        public string ChequeStatus { get; set; }
        public string ChequeStatusDetail { get; set; }
        public DateTime? TransactionDate { get; set; }
        public DateTime? OutwardDate { get; set; }
    }
}