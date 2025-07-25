using AutoMapper;
using SMIXKTBConvenienceCheque.DTOs.BatchOutput;
using SMIXKTBConvenienceCheque.DTOs.Cheque;
using SMIXKTBConvenienceCheque.DTOs.Report;
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

            CreateMap<HeaderChequeResponseDTO, BatchDetail>()
                .BeforeMap((src, dest) => TrimAllStringProperties(src));
            CreateMap<DetailChequeResponseDTO, BatchDetail>()
                .BeforeMap((src, dest) => TrimAllStringProperties(src));
            CreateMap<TrailerChequeResponseDTO, BatchDetail>().ReverseMap()
                .BeforeMap((src, dest) => TrimAllStringProperties(src));

            CreateMap<ChequeReportResponseDTO, usp_ReportCheque_SelectResult>().ReverseMap();
        }

        private static object TrimString(object value)
        {
            return value is string str ? str.Trim() : value;
        }

        /// <summary>
        /// fucntion trim value by DTO req
        /// </summary>
        /// <param name="obj"></param>
        private void TrimAllStringProperties(object obj)
        {
            var props = obj.GetType()
                           .GetProperties()
                           .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

            foreach (var prop in props)
            {
                var value = (string)prop.GetValue(obj);
                if (value != null)
                {
                    prop.SetValue(obj, value.Trim());
                }
            }
        }
    }
}