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

        public void ValidarQuartoDuplicado(Entity target)
        {
            QuartoDAO quartoDAO = new QuartoDAO(_context);
            EntityCollection quartos = quartoDAO.BuscarQuartosPorHotel(target.GetAttributeValue<EntityReference>("hsp_hotel"));

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
