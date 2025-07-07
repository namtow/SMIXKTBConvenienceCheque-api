#nullable disable

using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class HeaderChequeResponseDTO
    {
        [StringLength(1)]
        public string RecordType { get; set; }

        [StringLength(10)]
        public string PayerAbbreviation { get; set; }

        [StringLength(100)]
        public string PayerName { get; set; }

        [StringLength(70)]
        public string PayerAddress1 { get; set; }

        [StringLength(70)]
        public string PayerAddress2 { get; set; }

        [StringLength(70)]
        public string PayerAddress3 { get; set; }

        [StringLength(5)]
        public string PostCode { get; set; }

        [StringLength(20)]
        public string PayerAccountNo { get; set; }

        [StringLength(15)]
        public string PayerTaxID { get; set; }

        [StringLength(15)]
        public string PayerSocialSecurity { get; set; }

        [StringLength(8)]
        public string EffectiveDate { get; set; }

        [StringLength(35)]
        public string BatchNo { get; set; }

        [StringLength(25)]
        public string FileBatchNoBankReference { get; set; }

        [StringLength(5)]
        public string KTBRef { get; set; }

        [StringLength(1049)]
        public string Filler { get; set; }

        [StringLength(1)]
        public string CarriageReturn { get; set; }

        [StringLength(1)]
        public string EndofLine { get; set; }
    }
}