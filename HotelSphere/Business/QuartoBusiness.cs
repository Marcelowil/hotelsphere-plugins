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
            public const string NumeroQuartoJaCadastrado = "Já existe um quarto com o número {0} cadastrado neste hotel.";
            public const string TipoDeQuartoNaoPreenchido = "O quarto {0} não possuí um tipo de quarto preenchido.";
        }

        private readonly LocalPluginContext _context;
        private readonly QuartoDAO _quartoDAO;

        public QuartoBusiness(LocalPluginContext context)
        {
            _context = context;
            _quartoDAO = new QuartoDAO(context);
        }

        #region Métodos públicos

        public void ValidarCriacao(Entity target)
        {
            ValidarQuartoDuplicado(target);
        }

        public void ValidarAtualizacao(Entity target, Entity preImage)
        {
            bool NumeroQuartoFoiAlterado = target.Contains(Campos.NumeroQuarto);
            bool HotelFoiAlterado = target.Contains(Campos.Hotel);

            if (NumeroQuartoFoiAlterado || HotelFoiAlterado)
                ValidarQuartoDuplicado(target, preImage);
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

            return quarto.GetAttributeValue<EntityReference>("hsp_tipodequarto") ?? 
                throw new InvalidPluginExecutionException(Mensagens.TipoDeQuartoNaoPreenchido);
        }
        #endregion

        #region Regras de Cadastro do Quarto

        private void ValidarQuartoDuplicado(Entity target, Entity preImage = null)
        {
            EntityReference hotel = ObterValorEfetivo<EntityReference>(target, preImage, Campos.Hotel);
            string numeroCadastrado = ObterValorEfetivo<string>(target, preImage, Campos.NumeroQuarto);

            EntityCollection quartos = _quartoDAO.BuscarQuartosPorHotel(hotel);

            foreach (Entity quarto in quartos.Entities)
            {
                if (numeroCadastrado.Equals(quarto.GetAttributeValue<string>(Campos.NumeroQuarto)))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(Mensagens.NumeroQuartoJaCadastrado, numeroCadastrado));
                }
            }
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
