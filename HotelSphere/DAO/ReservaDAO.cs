using HotelSphere.Base;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace HotelSphere.DAO
{
    public class ReservaDAO
    {
        private readonly LocalPluginContext _context;

        public ReservaDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public bool VerificarPeriodoDisponivel(Guid quartoId, DateTime dataEntrada, DateTime dataSaida, Guid? reservaId = null)
        {
            var filtroIgnorarReserva = (reservaId.HasValue && reservaId.Value != Guid.Empty) ?
                $"<condition attribute='hsp_reservaid' operator='ne' value='{reservaId.Value}'/>" :
                String.Empty;

            var fetchReserva = new FetchExpression($@"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
                              <entity name='hsp_reserva'>
                                <attribute name='hsp_reservaid'/>
                                <filter type='and'>
                                  <condition attribute='hsp_quarto' operator='eq' value='{quartoId}'/>
                                  <condition attribute='hsp_dataentrada' operator='on-or-before' value='{dataSaida}'/>
                                  <condition attribute='hsp_datadesaida' operator='on-or-after' value='{dataEntrada}'/>
                                  <condition attribute='hsp_statusreserva' operator='ne' value='6'/>
                                  {filtroIgnorarReserva}
                                </filter>
                              </entity>
                            </fetch>");

            return !_context.Service.RetrieveMultiple(fetchReserva).Entities.Any();
        }
    }
}
