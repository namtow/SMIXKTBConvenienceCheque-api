using System.ComponentModel.DataAnnotations;

namespace SMIXKTBConvenienceCheque.DTOs.BatchOutput
{
    public class BatchOutputInsertRequestDTO
    {
        [Required]
        public string path { get; set; }
    }
}