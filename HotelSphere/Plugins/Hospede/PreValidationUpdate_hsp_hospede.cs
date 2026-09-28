using HotelSphere.Base;
using HotelSphere.Business;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Plugins.Hospede
{
    public class PreValidationUpdate_hsp_hospede : PluginBase
    {
        public PreValidationUpdate_hsp_hospede() : base(typeof(PreValidationUpdate_hsp_hospede)) { }

        protected override void ExecutePluginLogic(LocalPluginContext context)
        {
            if (!(context.Context.InputParameters["Target"] is Entity target))
                return;

            if (!(context.Context.PreEntityImages["preImage"] is Entity preImage))
                return;

            HospedeBusiness hospedeBusiness = new HospedeBusiness(context);

            if (target.Contains("hsp_datanascimento"))
                hospedeBusiness.ValidarDataNascimentoFutura(target);

            if (target.Contains("hsp_cpf"))
            {
                hospedeBusiness.ValidarCpf(target);
                hospedeBusiness.ConsultarCpfCadastradoPorHotel(target, preImage);
            }
        }
    }
}
