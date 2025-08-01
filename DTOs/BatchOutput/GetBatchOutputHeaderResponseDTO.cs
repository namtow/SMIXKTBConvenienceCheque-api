namespace SMIXKTBConvenienceCheque.DTOs.BatchOutput
{
    public class GetBatchOutputHeaderResponseDTO
    {
        public int BatchOutPutHeaderId { get; set; }
        public string FileName { get; set; }
        public string Data { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}