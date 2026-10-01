using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Tarifa
{
    public class PreValidationUpdate_hsp_tarifa : PluginBase
    {
        public PreValidationUpdate_hsp_tarifa() : base(typeof(PreValidationUpdate_hsp_tarifa)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            if (target.Contains("hsp_datainicio") || target.Contains("hsp_datafinal"))
                new TarifaBusiness(context).ValidarDuplicidadeTarifa(target, preImage);
        }
    }
}
