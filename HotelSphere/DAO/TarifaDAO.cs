using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace HotelSphere.DAO
{
    public class TarifaDAO
    {
        private readonly LocalPluginContext _context;

        public TarifaDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public EntityCollection BuscarTarifasPorHotelETipoQuarto(Guid tarifaId, Guid hotelId, Guid tipoQuartoId)
        {
            var fetchTarifas = new FetchExpression($@"<fetch version='1.0' mapping='logical'>
                                      <entity name='hsp_tarifa'>
                                        <attribute name='hsp_nome'/>
                                        <attribute name='hsp_tarifaid'/>
                                        <attribute name='hsp_codigotarifa'/>
                                        <attribute name='hsp_hotel'/>
                                        <attribute name='hsp_tipodequarto'/>
                                        <attribute name='hsp_origem'/>
                                        <attribute name='hsp_datainicio'/>
                                        <attribute name='hsp_datafinal'/>
                                        <attribute name='statecode'/>
                                        <attribute name='hsp_tarifavigente'/>
                                        <attribute name='hsp_valordiaria'/>
                                        <order attribute='hsp_hotel' descending='false'/>
                                        <filter type='and'>
                                          <condition attribute='statecode' operator='eq' value='0'/>
                                          <condition attribute='hsp_hotel' operator='eq' value='{hotelId}'/>
                                          <condition attribute='hsp_tipodequarto' operator='eq' value='{tipoQuartoId}'/>
                                          <condition attribute='hsp_tarifaid' operator='ne' value='{tarifaId}'/>
                                        </filter>
                                      </entity>
                                    </fetch>");

            return _context.Service.RetrieveMultiple(fetchTarifas);
        }
    }
}