using HotelSphere.Base;
using HotelSphere.DAO;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class QuartoBusiness
    {
        private static class Campos
        {
            public const string NumeroQuarto = "hsp_numerodoquarto";
            public const string Hotel = "hsp_hotel";
            public const string TipoDeQuarto = "hsp_tipodequarto";
            public const string Andar = "hsp_andar";
            public const string StatusQuarto = "hsp_statusquarto";
        }

        private static class Mensagens
        {
            public const string QuartoObrigatorio = "O quarto é obrigatório para a reserva.";
            public const string HotelObrigatorio = "O hotel é obrigatório para o quarto.";
            public const string NumeroQuartoObrigatorio = "O número do quarto é obrigatório para o quarto.";
            public const string NumeroQuartoJaCadastrado = "Já existe um quarto com o número {0} cadastrado neste hotel.";
            public const string TipoDeQuartoNaoPreenchido = "O quarto {0} não possuí um tipo de quarto preenchido.";
            public const string TipoDeQuartoNaoPertenceAoHotel = "O tipo de quarto selecionado não pertence a este hotel.";
        }

        private readonly LocalPluginContext _context;
        private readonly QuartoDAO _quartoDAO;
        private readonly TipoQuartoBusiness _tipoQuartoBusiness;

        public QuartoBusiness(LocalPluginContext context)
        {
            _context = context;
            _quartoDAO = new QuartoDAO(context);
            _tipoQuartoBusiness = new TipoQuartoBusiness(context);
        }

        #region Métodos públicos

        public void ValidarCriacao(Entity target)
        {
            EntityReference hotelRef = target.GetAttributeValue<EntityReference>(Campos.Hotel) ??
                throw new InvalidPluginExecutionException(Mensagens.HotelObrigatorio);

            string numeroQuarto = target.GetAttributeValue<string>(Campos.NumeroQuarto) ??
                throw new InvalidPluginExecutionException(Mensagens.NumeroQuartoObrigatorio);

            EntityReference tipoQuartoRef = target.GetAttributeValue<EntityReference>(Campos.TipoDeQuarto) ??
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.TipoDeQuartoNaoPreenchido, numeroQuarto));

            ValidarTipoQuartoPertenceAoHotel(tipoQuartoRef, hotelRef);
            ValidarQuartoDuplicado(hotelRef, numeroQuarto);
        }

        public void ValidarAtualizacao(Entity target, Entity preImage)
        {
            bool NumeroQuartoFoiAlterado = target.Contains(Campos.NumeroQuarto);
            bool HotelFoiAlterado = target.Contains(Campos.Hotel);
            bool tipoQuartoAlterado = target.Contains(Campos.TipoDeQuarto);

            EntityReference hotelRef = ObterValorEfetivo<EntityReference>(target, preImage, Campos.Hotel);

            if (HotelFoiAlterado || tipoQuartoAlterado)
            {
                EntityReference tipoQuartoRef = ObterValorEfetivo<EntityReference>(target, preImage, Campos.TipoDeQuarto);
                ValidarTipoQuartoPertenceAoHotel(tipoQuartoRef, hotelRef);
            }


            if (NumeroQuartoFoiAlterado)
            {
                string numeroQuarto = ObterValorEfetivo<string>(target, preImage, Campos.NumeroQuarto);
                ValidarQuartoDuplicado(hotelRef, numeroQuarto);
            }
        }


        #endregion

        #region Consultas
        public Entity ObterQuartoPorId(EntityReference quartoRef)
        {
            if (quartoRef == null)
                throw new InvalidPluginExecutionException(Mensagens.QuartoObrigatorio);

            return _quartoDAO.ObterPorId(quartoRef.Id) ??
                throw new InvalidPluginExecutionException(Mensagens.QuartoObrigatorio);
        }

        public int ObterCapacidade(EntityReference quartoRef)
        {
            return _quartoDAO.ObterCapacidade(quartoRef.Id);
        }

        public EntityReference ObterTipoDeQuarto(EntityReference quartoRef)
        {
            Entity quarto = ObterQuartoPorId(quartoRef);

            return quarto.GetAttributeValue<EntityReference>(Campos.TipoDeQuarto) ?? 
                throw new InvalidPluginExecutionException(Mensagens.TipoDeQuartoNaoPreenchido);
        }
        #endregion

        #region Regras de Cadastro do Quarto

        private void ValidarQuartoDuplicado(EntityReference hotelRef, string numeroQuarto)
        {
            EntityCollection quartos = _quartoDAO.BuscarQuartosPorHotel(hotelRef);

            foreach (Entity quarto in quartos.Entities)
            {
                if (numeroQuarto.Equals(quarto.GetAttributeValue<string>(Campos.NumeroQuarto)))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(Mensagens.NumeroQuartoJaCadastrado, numeroQuarto));
                }
            }
        }

        private void ValidarTipoQuartoPertenceAoHotel(EntityReference tipoQuartoRef, EntityReference hotelRef)
        {
            if(!_tipoQuartoBusiness.TipoQuartoPertenceAoHotel(tipoQuartoRef.Id, hotelRef.Id))
                throw new InvalidPluginExecutionException(Mensagens.TipoDeQuartoNaoPertenceAoHotel);
        }

        #endregion

        #region Auxiliares

        private static T ObterValorEfetivo<T>(Entity target, Entity preImage, string atributo)
        {
            if (target.Contains(atributo))
                return target.GetAttributeValue<T>(atributo);

            return preImage != null ? preImage.GetAttributeValue<T>(atributo) : default(T);
        }

        #endregion
    }
}
