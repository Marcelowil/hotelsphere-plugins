using HotelSphere.Base;
using HotelSphere.DAO;
using System;

namespace HotelSphere.Business
{
    public class TipoQuartoBusiness
    {
        private readonly LocalPluginContext _context;
        private readonly TipoQuartoDAO _tipoQuartoDAO;

        public TipoQuartoBusiness(LocalPluginContext context)
        {
            _context = context;
            _tipoQuartoDAO = new TipoQuartoDAO(context);
        }

        public bool TipoQuartoPertenceAoHotel(Guid tipoQuartoId, Guid hotelId)
        {
            return _tipoQuartoDAO.BuscarTipoQuartoPorId(tipoQuartoId, hotelId).Entities.Count >= 1;
        }
    }
}
