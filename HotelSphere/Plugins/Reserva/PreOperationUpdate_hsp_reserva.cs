using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PreOperationUpdate_hsp_reserva : PluginBase
    {
        public PreOperationUpdate_hsp_reserva() : base(typeof(PreOperationUpdate_hsp_reserva)) { }
        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            new ReservaBusiness(context).ExecutarPreOperacao(target, preImage);
        }
    }
}
