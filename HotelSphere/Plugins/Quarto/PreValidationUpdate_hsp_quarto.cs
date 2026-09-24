using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Quarto
{
    public class PreValidationUpdate_hsp_quarto : PluginBase
    {
        public PreValidationUpdate_hsp_quarto() : base(typeof(PreValidationUpdate_hsp_quarto)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            if (target.Contains("hsp_numerodoquarto"))
                new QuartoBusiness(context).ValidarQuartoDuplicado(target, preImage);
        }
    }
}
