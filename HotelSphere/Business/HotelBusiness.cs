using HotelSphere.Base;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Business
{
    public class HotelBusiness
    {
        private readonly LocalPluginContext _context;

        public HotelBusiness(LocalPluginContext context)
        {
            _context = context;
        }

        public void ValidarCnpj(Entity target)
        {
            string cnpj = target.GetAttributeValue<string>("hsp_cnpj");

            if (!DocumentoValidator.CnpjEhValido(cnpj))
                throw new InvalidPluginExecutionException("O CNPJ preenchido é inválido.");
        }
    }
}
