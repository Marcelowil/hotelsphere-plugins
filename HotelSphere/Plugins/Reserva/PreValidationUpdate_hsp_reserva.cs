using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PreValidationUpdate_hsp_reserva : PluginBase
    {
        public PreValidationUpdate_hsp_reserva() : base(typeof(PreValidationUpdate_hsp_reserva)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            new ReservaBusiness(context).ValidarAtualizacao(target, preImage);
        }
    }
}
