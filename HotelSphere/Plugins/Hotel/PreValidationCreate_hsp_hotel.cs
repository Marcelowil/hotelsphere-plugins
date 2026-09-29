using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Hotel
{
    public class PreValidationCreate_hsp_hotel : PluginBase
    {
        public PreValidationCreate_hsp_hotel() : base(typeof(PreValidationCreate_hsp_hotel)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            new HotelBusiness(context).ValidarCnpj(target);
        }
    }
}
