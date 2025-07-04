namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class HeaderChequeResponseDTO
    {
        public string RecordType { get; set; }
        public string PayerAbbreviation { get; set; }
        public string PayerName { get; set; }
        public string PayerAddress1 { get; set; }
        public string PayerAddress2 { get; set; }
        public string PayerAddress3 { get; set; }
        public string PostCode { get; set; }
        public string PayerACNo { get; set; }
        public string PayerTaxID { get; set; }
        public string PayerSocialSecurity { get; set; }
        public string EffectiveDate { get; set; }
        public string BatchNo { get; set; }
        public string BankReference { get; set; }
        public string KTB { get; set; }
        public string Filler { get; set; }
        public string CarriageReturn { get; set; }
        public string EndofLine { get; set; }
    }
}