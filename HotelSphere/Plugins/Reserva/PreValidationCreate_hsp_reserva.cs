using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PreValidationCreate_hsp_reserva : PluginBase
    {
        public PreValidationCreate_hsp_reserva() : base(typeof(PreValidationCreate_hsp_reserva)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            new ReservaBusiness(context).ValidarCriacao(target);
        }
    }
}
