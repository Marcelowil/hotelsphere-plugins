using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Web.UI.WebControls;

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
    }
}
