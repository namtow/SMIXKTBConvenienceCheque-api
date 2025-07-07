using AutoMapper;
using SMIXKTBConvenienceCheque.DTOs.BatchOutput;
using SMIXKTBConvenienceCheque.Models;

namespace SMIXKTBConvenienceCheque
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<BatchOutputDetailDTO, BatchOutPutDetail>()
            .ForAllMembers(opt =>
            {
                if (opt.DestinationMember.Name != nameof(BatchOutputDetailDTO.BatchData))
                {
                    opt.AddTransform(s => TrimString(s));
                }
            });
        }

        private static object TrimString(object value)
        {
            return value is string str ? str.Trim() : value;
        }
    }
}