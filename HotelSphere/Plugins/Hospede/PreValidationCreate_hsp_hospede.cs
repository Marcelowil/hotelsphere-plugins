using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Hospede
{
    public class PreValidationCreate_hsp_hospede : PluginBase
    {
        public PreValidationCreate_hsp_hospede() : base(typeof(PreValidationCreate_hsp_hospede)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            HospedeBusiness hospedeBusiness = new HospedeBusiness(context);
            hospedeBusiness.ValidarDataNascimentoFutura(target);
            hospedeBusiness.ValidarCpf(target);
            hospedeBusiness.ConsultarCpfCadastradoPorHotel(target);
        }
    }
}
