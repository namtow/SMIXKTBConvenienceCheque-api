using SMIXKTBConvenienceCheque.DTOs.BatchOutput;

namespace SMIXKTBConvenienceCheque.Services.BatchOutput
{
    public interface IBatchOutputServices
    {
        Task<BatchOutputInsertResponseDTO> BatchOutputInsert(BatchOutputInsertRequestDTO intput);
    }
}