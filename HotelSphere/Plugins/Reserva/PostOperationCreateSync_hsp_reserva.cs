using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PostOperationCreateSync_hsp_reserva : PluginBase
    {
        public PostOperationCreateSync_hsp_reserva() : base(typeof(PostOperationCreateSync_hsp_reserva)) { }
        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            new ReservaBusiness(context).ExecutarPosOperacaoSync(target);
        }
    }
}
