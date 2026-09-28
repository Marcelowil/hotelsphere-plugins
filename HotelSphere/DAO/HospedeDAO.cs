using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace HotelSphere.DAO
{
    public class HospedeDAO
    {
        public LocalPluginContext _context;

        public HospedeDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public bool ExisteCpfPorHotel(string cpf, EntityReference hotel)
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("contactid"),
                TopCount = 1,
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression("hsp_cpf", ConditionOperator.Equal, cpf),
                        new ConditionExpression("hsp_hotel", ConditionOperator.Equal, hotel.Id)
                    }
                }
            };

            return _context.Service.RetrieveMultiple(query).Entities.Any();
        }
    }
}
