namespace SMIXKTBConvenienceCheque.DTOs.Cheque
{
    public class FileResponseDTO
    {
        public bool? IsResult { get; set; }
        public byte[] Data { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
    }
}