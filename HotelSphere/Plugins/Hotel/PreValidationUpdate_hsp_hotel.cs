using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Hotel
{
    public class PreValidationUpdate_hsp_hotel : PluginBase
    {
        public PreValidationUpdate_hsp_hotel() : base(typeof(PreValidationUpdate_hsp_hotel)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (target.Contains("hsp_cnpj"))
                new HotelBusiness(context).ValidarCnpj(target);
        }
    }
}
