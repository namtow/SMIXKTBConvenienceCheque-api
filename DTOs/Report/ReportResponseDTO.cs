namespace SMIXKTBConvenienceCheque.DTOs.Report
{
    public class ReportResponseDTO
    {
        public bool? IsResult { get; set; } = true;
        public byte[] Data { get; set; }
        public string Message { get; set; } = "Success";
        public string FileName { get; set; }
    }
}