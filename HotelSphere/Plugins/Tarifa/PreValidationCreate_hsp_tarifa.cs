using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Plugins.Tarifa
{
    public class PreValidationCreate_hsp_tarifa : PluginBase
    {
        public PreValidationCreate_hsp_tarifa() : base(typeof(PreValidationCreate_hsp_tarifa)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            new TarifaBusiness(context).ValidarDuplicidadeTarifa(target);
        }
    }
}
