namespace SMIXKTBConvenienceCheque.DTOs.BatchOutput
{
    public class BatchOutputDetailDTO
    {
        public string RecordType { get; set; }               // char(1)
        public string CompanyAbbreviation { get; set; }      // char(10)
        public string CompanyName { get; set; }              // char(100)
        public string Address1 { get; set; }                 // char(70)
        public string Address2 { get; set; }                 // char(70)
        public string Address3 { get; set; }                 // char(70)
        public string PayeeAccountNumber { get; set; }       // char(20)
        public string PayeeTaxIdNumber { get; set; }         // char(15)
        public string SocialSecurityId { get; set; }         // char(15)
        public string Sequence { get; set; }                 // char(6)
        public string PaymentRefNo1 { get; set; }            // char(20)
        public string ChequeEffectiveDate { get; set; }      // char(8)
        public string ChequeNumber { get; set; }             // char(10)
        public string PayeeName { get; set; }                // char(100)
        public string WithholdingTaxAmount { get; set; }     // char(20)
        public string NetCheque { get; set; }                // char(20)
        public string ChequeStatus { get; set; }             // char(5)
        public string TransactionDate { get; set; }          // char(8)
        public string OutwardDate { get; set; }              // char(8)
        public string TotalRecords { get; set; }
        public string BatchData { get; set; }
    }
}