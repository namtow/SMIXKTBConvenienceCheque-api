using System.ComponentModel.DataAnnotations;

namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class FileChequeResponseDTO
    {
        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public int FileNo { get; set; }

        public int BatchNo { get; set; }
        public DateTime? UploadDate { get; set; }
    }
}