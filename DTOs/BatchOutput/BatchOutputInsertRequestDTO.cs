using System.ComponentModel.DataAnnotations;

namespace SMIXKTBConvenienceCheque.DTOs.BatchOutput
{
    public class BatchOutputInsertRequestDTO
    {
        [Required]
        public string FileName { get; set; }
    }
}