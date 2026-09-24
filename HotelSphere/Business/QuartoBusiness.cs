using HotelSphere.Base;
using HotelSphere.DAO;
using Microsoft.Xrm.Sdk;

namespace HotelSphere.Business
{
    public class QuartoBusiness
    {
        private readonly LocalPluginContext _context;

        public QuartoBusiness(LocalPluginContext context)
        {
            _context = context;
        }

        public void ValidarQuartoDuplicado(Entity target, Entity preImage = null)
        {
            QuartoDAO quartoDAO = new QuartoDAO(_context);

            EntityReference hotel = preImage == null ? target.GetAttributeValue<EntityReference>("hsp_hotel") 
                : preImage.GetAttributeValue<EntityReference>("hsp_hotel");

            EntityCollection quartos = quartoDAO.BuscarQuartosPorHotel(hotel);

            string numeroCadastrado = target.GetAttributeValue<string>("hsp_numerodoquarto");

            foreach (Entity quarto in quartos.Entities)
            {
                if (numeroCadastrado.Equals(quarto.GetAttributeValue<string>("hsp_numerodoquarto")))
                {
                    throw new InvalidPluginExecutionException($"Já existe um quarto com o número {numeroCadastrado} cadastrado.");
                }
            }
        }
    }
}
