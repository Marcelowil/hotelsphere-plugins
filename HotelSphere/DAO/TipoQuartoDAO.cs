using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace HotelSphere.DAO
{
    public class TipoQuartoDAO
    {
        private readonly LocalPluginContext _context;

        public TipoQuartoDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public EntityCollection BuscarTipoQuartoPorId(Guid tipoQuarto, Guid hotelId)
        {
            var fetchTipo = new FetchExpression($@"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
                          <entity name='hsp_tipodequarto'>
                            <attribute name='hsp_tipodequartoid'/>
                            <filter type='and'>
                              <condition attribute='hsp_tipodequartoid' operator='eq' value='{tipoQuarto}'/>
                              <condition attribute='hsp_hotel' operator='eq' value='{hotelId}'/>
                            </filter>
                          </entity>
                        </fetch>");

            return _context.Service.RetrieveMultiple(fetchTipo);
        }
    }
}
