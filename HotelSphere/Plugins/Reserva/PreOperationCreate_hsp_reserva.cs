using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PreOperationCreate_hsp_reserva : PluginBase
    {
        public PreOperationCreate_hsp_reserva() : base(typeof(PreOperationCreate_hsp_reserva)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            new ReservaBusiness(context).ExecutarPreOperacao(target);
        }
    }
}
