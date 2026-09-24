using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Quarto
{
    public class PreValidationCreate_hsp_quarto : PluginBase
    {
        public PreValidationCreate_hsp_quarto() : base(typeof(PreValidationCreate_hsp_quarto)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            QuartoBusiness business = new QuartoBusiness(context);
            business.ValidarQuartoDuplicado(target);
        }

    }
}
