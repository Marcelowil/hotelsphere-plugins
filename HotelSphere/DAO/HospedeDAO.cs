using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
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

        public bool ExisteCpfPorHotel(string cpf, EntityReference hotel, Guid ignorarId)
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
                        new ConditionExpression("hsp_hotel", ConditionOperator.Equal, hotel.Id),
                        new ConditionExpression("contactid", ConditionOperator.NotEqual , ignorarId)
                    }
                }
            };

            return _context.Service.RetrieveMultiple(query).Entities.Any();
        }

        public Entity ObterPorId(Guid hospedeId)
        {
            return _context.Service.Retrieve("contact", hospedeId, new ColumnSet("hsp_datanascimento", "hsp_hotel", "statuscode"));
        }

        public EntityCollection ObterPorReserva(Guid reservaId)
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "hsp_hotel", "statuscode")
            };

            var linkIntersecao = query.AddLink("hsp_hospede_reserva_association", "contactid", "contactid");

            linkIntersecao.LinkCriteria.AddCondition("hsp_reservaid", ConditionOperator.Equal, reservaId);

            return _context.Service.RetrieveMultiple(query);
        }
    }
}
