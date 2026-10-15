using HotelSphere.Base;
using HotelSphere.DAO;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class ReservaBusiness
    {
        private const int IdadeMinimaResponsavel = 18;
        private const int QuantidadeHospedePrincipal = 1;

        private static class Campos
        {
            public const string Hotel = "hsp_hotel";
            public const string Quarto = "hsp_quarto";
            public const string Hospede = "hsp_hospede";
            public const string StatusQuarto = "hsp_statusquarto";
            public const string NomeCompletoHospede = "fullname";
        }

        private static class StatusDoQuarto
        {
            public const int Disponivel = 1;
            public const int Ocupado = 2;
            public const int Manutencao = 3;
            public const int Bloqueado = 4;
        }

        private static class Mensagens
        {
            public const string HotelObrigatorio = "O hotel é obrigatório para a reserva.";
            public const string QuartoSemHotel = "O quarto não possui hotel vinculado.";
            public const string QuartoSemStatus = "O quarto não possui um status preenchido.";
            public const string QuartoIndisponivel = "O quarto está indisponível para reserva. Status: {0}";
            public const string QuartoNaoPertenceAoHotel = "Quarto não localizado neste hotel.";
            public const string HospedeNaoPertenceAoHotel = "O hóspede {0} não pertence ao hotel da reserva.";
            public const string HospedeSemHotel = "O hóspede não possui hotel vinculado.";
            public const string HospedeResponsavelMenorDeIdade = "O hóspede responsável deve ter pelo menos {0} anos.";
            public const string HospedePrincipalDuplicado = "O hóspede principal não pode estar duplicado na lista de hóspedes da reserva.";
            public const string CapacidadeDoQuartoExcedida = "O quarto comporta no máximo {0} hóspede(s).";
        }

        private sealed class ReferenciasDaReserva
        {
            public EntityReference Hotel { get; set; }
            public EntityReference Quarto { get; set; }
            public EntityReference Hospede { get; set; }
        }

        private readonly QuartoBusiness _quartoBusiness;
        private readonly HospedeBusiness _hospedeBusiness;

        public ReservaBusiness(LocalPluginContext context)
        {
            _quartoBusiness = new QuartoBusiness(context);
            _hospedeBusiness = new HospedeBusiness(context);
        }

        public void ValidarCriacao(Entity target)
        {
            EntityReference hotel = target.GetAttributeValue<EntityReference>(Campos.Hotel)
                ?? throw new InvalidPluginExecutionException(Mensagens.HotelObrigatorio);

            ValidarQuarto(target.GetAttributeValue<EntityReference>(Campos.Quarto), hotel.Id);
            ValidarHospedePrincipal(target.GetAttributeValue<EntityReference>(Campos.Hospede), hotel.Id);
        }

        public void ValidarAtualizacao(Entity target, Entity preImage)
        {
            ReferenciasDaReserva reserva = ObterReferenciasEfetivas(target, preImage);
            Guid hotelDaReservaId = ObterHotelObrigatorio(reserva).Id;

            bool hotelAlterado = HotelFoiAlterado(target, preImage);
            bool quartoInformado = target.Contains(Campos.Quarto);
            bool hospedeInformado = target.Contains(Campos.Hospede);

            if (hotelAlterado || quartoInformado)
                ValidarQuarto(reserva.Quarto, hotelDaReservaId);

            if (hotelAlterado || hospedeInformado)
                ValidarHospedePrincipal(reserva.Hospede, hotelDaReservaId);

            EntityCollection hospedesAdicionais = CarregarEValidarHospedesAdicionais(reserva.Hospede, target.Id, hotelDaReservaId);

            if (reserva.Quarto != null)
                ValidarCapacidadeDoQuarto(hospedesAdicionais, reserva.Quarto);
        }

        #region Validações da Reserva

        private static bool HotelFoiAlterado(Entity target, Entity preImage)
        {
            if (!target.Contains(Campos.Hotel))
                return false;

            EntityReference hotelNovo = target.GetAttributeValue<EntityReference>(Campos.Hotel);
            EntityReference hotelAnterior = preImage?.GetAttributeValue<EntityReference>(Campos.Hotel);

            return hotelNovo?.Id != hotelAnterior?.Id;
        }

        private void ValidarHospedePrincipal(EntityReference hospedeRef, Guid hotelDaReservaId)
        {
            Entity hospede = _hospedeBusiness.ObterHospedePorId(hospedeRef);

            ValidarHospedePertenceAoHotel(hospede, hotelDaReservaId);
            ValidarResponsavelMaiorDeIdade(hospede);
            ValidarHospedeAtivo(hospede);
        }

        private void ValidarQuarto(EntityReference quartoRef, Guid hotelDaReservaId)
        {
            Entity quarto = _quartoBusiness.ObterQuartoPorId(quartoRef);

            ValidarQuartoPertenceAoHotel(quarto, hotelDaReservaId);
            ValidarQuartoDisponivel(quarto);
        }

        #endregion

        #region Validações do Quarto

        private static void ValidarQuartoPertenceAoHotel(Entity quarto, Guid hotelDaReservaId)
        {
            EntityReference hotelDoQuarto = quarto.GetAttributeValue<EntityReference>(Campos.Hotel)
                ?? throw new InvalidPluginExecutionException(Mensagens.QuartoSemHotel);

            if (hotelDoQuarto.Id != hotelDaReservaId)
                throw new InvalidPluginExecutionException(Mensagens.QuartoNaoPertenceAoHotel);
        }

        private static void ValidarQuartoDisponivel(Entity quarto)
        {
            OptionSetValue statusAtual = quarto.GetAttributeValue<OptionSetValue>(Campos.StatusQuarto)
                ?? throw new InvalidPluginExecutionException(Mensagens.QuartoSemStatus);

            if (QuartoEstaEmManutencao(statusAtual) || QuartoEstaBloqueado(statusAtual))
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.QuartoIndisponivel, ObterDescricaoDoStatus(quarto)));
        }

        private static bool QuartoEstaEmManutencao(OptionSetValue statusAtual) =>
            statusAtual.Value == StatusDoQuarto.Manutencao;

        private static bool QuartoEstaBloqueado(OptionSetValue statusAtual) =>
            statusAtual.Value == StatusDoQuarto.Bloqueado;

        private static string ObterDescricaoDoStatus(Entity quarto) =>
            quarto.FormattedValues.TryGetValue(Campos.StatusQuarto, out string descricao)
                ? descricao
                : string.Empty;

        private void ValidarCapacidadeDoQuarto(EntityCollection hospedesAdicionais, EntityReference quartoRef)
        {
            int capacidadeDoQuarto = _quartoBusiness.ObterCapacidade(quartoRef);
            int totalDeHospedes = hospedesAdicionais.Entities.Count + QuantidadeHospedePrincipal;

            if (capacidadeDoQuarto < totalDeHospedes)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.CapacidadeDoQuartoExcedida, capacidadeDoQuarto));
        }

        #endregion

        #region Validações do Hóspede

        private static void ValidarHospedePertenceAoHotel(Entity hospede, Guid hotelDaReservaId)
        {
            EntityReference hotelDoHospede = hospede.GetAttributeValue<EntityReference>(Campos.Hotel)
                ?? throw new InvalidPluginExecutionException(Mensagens.HospedeSemHotel);

            if (hotelDoHospede.Id != hotelDaReservaId)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.HospedeNaoPertenceAoHotel, hospede.GetAttributeValue<string>(Campos.NomeCompletoHospede)));
        }

        private void ValidarResponsavelMaiorDeIdade(Entity hospede)
        {
            int idade = _hospedeBusiness.ObterIdade(hospede);

            if (idade < IdadeMinimaResponsavel)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.HospedeResponsavelMenorDeIdade, IdadeMinimaResponsavel));
        }

        private EntityCollection CarregarEValidarHospedesAdicionais(
            EntityReference hospedePrincipalRef, Guid reservaId, Guid hotelDaReservaId)
        {
            EntityCollection hospedesAdicionais = CarregarHospedesDaReserva(reservaId);

            foreach (Entity hospedeAdicional in hospedesAdicionais.Entities)
            {
                ValidarHospedePertenceAoHotel(hospedeAdicional, hotelDaReservaId);
                ValidarHospedeAtivo(hospedeAdicional);
                ValidarHospedePrincipalDuplicado(hospedePrincipalRef, hospedeAdicional);
            }

            return hospedesAdicionais;
        }

        private void ValidarHospedeAtivo(Entity hospede)
        {
            _hospedeBusiness.ValidarHospedeAtivo(hospede);
        }

        private static void ValidarHospedePrincipalDuplicado(EntityReference hospedePrincipalRef, Entity hospedeAdicional)
        {
            if (hospedePrincipalRef?.Id == hospedeAdicional.Id)
                throw new InvalidPluginExecutionException(Mensagens.HospedePrincipalDuplicado);
        }

        #endregion

        #region Auxiliares

        public EntityCollection CarregarHospedesDaReserva(Guid reservaId)
        {
            return _hospedeBusiness.ObterHospedesPorIdDaReserva(reservaId);
        }

        private static ReferenciasDaReserva ObterReferenciasEfetivas(Entity target, Entity preImage)
        {
            return new ReferenciasDaReserva
            {
                Hotel = ObterReferenciaEfetiva(target, preImage, Campos.Hotel),
                Quarto = ObterReferenciaEfetiva(target, preImage, Campos.Quarto),
                Hospede = ObterReferenciaEfetiva(target, preImage, Campos.Hospede)
            };
        }

        private static EntityReference ObterReferenciaEfetiva(Entity target, Entity preImage, string atributo)
        {
            return target.Contains(atributo)
                ? target.GetAttributeValue<EntityReference>(atributo)
                : preImage?.GetAttributeValue<EntityReference>(atributo);
        }

        private static EntityReference ObterHotelObrigatorio(ReferenciasDaReserva reserva)
        {
            return reserva.Hotel
                ?? throw new InvalidPluginExecutionException(Mensagens.HotelObrigatorio);
        }

        #endregion
    }
}