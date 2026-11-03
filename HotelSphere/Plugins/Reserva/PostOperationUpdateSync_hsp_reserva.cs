using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Reserva
{
    public class PostOperationUpdateSync_hsp_reserva : PluginBase
    {
        public PostOperationUpdateSync_hsp_reserva() : base(typeof(PostOperationUpdateSync_hsp_reserva)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            new ReservaBusiness(context).ExecutarPosOperacaoSync(target, preImage);
        }
    }
}
