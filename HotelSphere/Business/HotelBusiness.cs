using HotelSphere.Base;
using HotelSphere.DAO;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Business
{
    public class HotelBusiness
    {
        private readonly LocalPluginContext _context;
        private readonly HotelDAO _hotelDAO;

        public HotelBusiness(LocalPluginContext context)
        {
            _context = context;
            _hotelDAO = new HotelDAO(_context);
        }

        public void ValidarCnpj(Entity target)
        {
            string cnpj = target.GetAttributeValue<string>("hsp_cnpj");

            if (!DocumentoValidator.CnpjEhValido(cnpj))
                throw new InvalidPluginExecutionException("O CNPJ preenchido é inválido.");

            ValidarCnpjDuplicado(cnpj);
        }

        private void ValidarCnpjDuplicado(string cnpj)
        {
            if (_hotelDAO.ExisteCpnjCadastrado(cnpj))
                throw new InvalidPluginExecutionException($"O CNPJ {cnpj} preenchido já possuí cadastro.");
        }
    }
}
