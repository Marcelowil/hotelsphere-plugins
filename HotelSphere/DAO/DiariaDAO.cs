using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace HotelSphere.DAO
{
    public class DiariaDAO
    {
        private readonly LocalPluginContext _context;

        public DiariaDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public void CriarDiariasDaReserva(EntityCollection diarias)
        {
            var requests = new CreateMultipleRequest { Targets = diarias };

            _context.Service.Execute(requests);
        }

        public void ExcluirDiariasDaReserva(Guid reservaId)
        {
            var fetchDiarias = new FetchExpression($@"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
                      <entity name='hsp_reservadiaria'>
                        <attribute name='hsp_reservadiariaid'/>
                        <order attribute='hsp_reserva' descending='false'/>
                        <filter type='and'>
                          <condition attribute='hsp_reserva' operator='eq' value='{reservaId}'/>
                        </filter>
                      </entity>
                    </fetch>");

            var diarias = _context.Service.RetrieveMultiple(fetchDiarias);

            if (diarias.Entities.Count == 0) return;

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = false
                },
                Requests = new OrganizationRequestCollection()
            };

            foreach (var diaria in diarias.Entities)
            {
                var deleteRequest = new DeleteRequest
                {
                    Target = new EntityReference("hsp_reservadiaria", diaria.Id)
                };

                executeMultipleRequest.Requests.Add(deleteRequest);
            }

            _context.Service.Execute(executeMultipleRequest);
        }

        public Entity ObterDiariaMaisRecentePorTarifa(Guid tarifaId)
        {
            return ObterDiariaPorTarifaOrdenadaPorData(tarifaId, OrderType.Descending);
        }

        public Entity ObterDiariaMaisAntigaPorTarifa(Guid tarifaId)
        {
            return ObterDiariaPorTarifaOrdenadaPorData(tarifaId, OrderType.Ascending);
        }

        private Entity ObterDiariaPorTarifaOrdenadaPorData(Guid tarifaId, OrderType ordemData)
        {
            var query = new QueryExpression("hsp_reservadiaria")
            {
                ColumnSet = new ColumnSet("hsp_datadiaria", "hsp_tarifa"),
                TopCount = 1
            };

            query.Criteria.AddCondition("hsp_tarifa", ConditionOperator.Equal, tarifaId);

            query.AddOrder("hsp_datadiaria", ordemData);

            return _context.Service.RetrieveMultiple(query).Entities.FirstOrDefault();
        }
    }
}
