using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace HotelSphere.DAO
{
    public class QuartoDAO
    {
        public LocalPluginContext _context;

        public QuartoDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public EntityCollection BuscarQuartosPorHotel(EntityReference hotel)
        {
            var fetchQuartos = new FetchExpression($@"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
                          <entity name='hsp_quarto'>
                            <attribute name='hsp_numerodoquarto'/>
                            <attribute name='hsp_quartoid'/>
                            <attribute name='hsp_hotel'/>
                            <attribute name='hsp_tipodequarto'/>
                            <attribute name='hsp_andar'/>
                            <attribute name='hsp_statusquarto'/>
                            <attribute name='hsp_observacoes'/>
                            <attribute name='createdon'/>
                            <order attribute='hsp_hotel' descending='false'/>
                            <order attribute='hsp_numerodoquarto' descending='false'/>
                            <filter type='and'>
                              <condition attribute='hsp_hotel' operator='eq' value='{hotel.Id}'/>
                            </filter>
                          </entity>
                        </fetch>");

            return _context.Service.RetrieveMultiple(fetchQuartos);
        }

        public Entity ObterPorId(Guid quartoId)
        {
            var columnSet = new ColumnSet("hsp_numerodoquarto", "hsp_hotel", "hsp_tipodequarto", "hsp_statusquarto");

            return _context.Service.Retrieve("hsp_quarto", quartoId, columnSet);
        }

        public int ObterCapacidade(Guid quartoId)
        {
            var fetchQuarto = new FetchExpression($@"
                        <fetch version='1.0' output-format='xml-platform' mapping='logical'>
                              <entity name='hsp_quarto'>
                                <attribute name='hsp_quartoid'/>
                                <filter type='and'>
                                  <condition attribute='hsp_quartoid' operator='eq' value='{quartoId}'/>
                                </filter>
                                <link-entity name='hsp_tipodequarto' from='hsp_tipodequartoid' to='hsp_tipodequarto' alias='tipo'>
                                  <attribute name='hsp_capacidade'/>
                                </link-entity>
                              </entity>
                            </fetch>");

            var resultado = _context.Service.RetrieveMultiple(fetchQuarto).Entities.First();

            return (int) resultado.GetAttributeValue<AliasedValue>("tipo.hsp_capacidade").Value;
        }
    }
}
