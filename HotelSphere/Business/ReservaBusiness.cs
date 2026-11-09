using HotelSphere.Base;
using HotelSphere.DAO;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;

namespace HotelSphere.Business
{
    public class ReservaBusiness
    {
        private const int IdadeMinimaResponsavel = 18;
        private const int QuantidadeHospedePrincipal = 1;

        private static class Campos
        {
            public const string Reserva = "hsp_reserva";
            public const string Hotel = "hsp_hotel";
            public const string Quarto = "hsp_quarto";
            public const string Hospede = "hsp_hospede";
            public const string Tarifa = "hsp_tarifa";
            public const string ReservaDiaria = "hsp_reservadiaria";
            public const string StatusQuarto = "hsp_statusquarto";
            public const string NomeCompletoHospede = "fullname";
            public const string DataEntrada = "hsp_dataentrada";
            public const string DataSaida = "hsp_datadesaida";
            public const string TarifaDataInicio = "hsp_datainicio";
            public const string TarifaDataFinal = "hsp_datafinal";
            public const string ValorTotal = "hsp_valortotal";
            public const string DataDiaria = "hsp_datadiaria";
            public const string ValorDiaria = "hsp_valordiaria";
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
            public const string DataEntradaObrigatoria = "A data de entrada é obrigatória.";
            public const string DataSaidaObrigatoria = "A data de saída é obrigatória.";
            public const string PeriodoIndisponivel = "O quarto já está reservado entre {0:dd/MM/yyyy} e {1:dd/MM/yyyy}.";
            public const string TarifaSemDataInicial = "Tarifa sem data inicial.";
            public const string TarifaSemDataFinal = "Tarifa sem data final.";
            public const string SemTarifaParaDiaria = "Não há tarifa cadastrada para o dia {0:dd/MM/yyyy}";
            public const string ValorTotalDivergente = "O valor total da reserva está divergente com o valor simulado, verifique as tarifas cadastradas para o período.";
            public const string ValorTotalNaoCalculado = "O valor total da reserva não foi calculado, verifique se há tarifas cadastradas para o tipo do quarto ou período.";
        }

        private sealed class ReferenciasDaReserva
        {
            public EntityReference Hotel { get; set; }
            public EntityReference Quarto { get; set; }
            public EntityReference Hospede { get; set; }
        }

        private readonly LocalPluginContext _context;
        private readonly QuartoBusiness _quartoBusiness;
        private readonly HospedeBusiness _hospedeBusiness;
        private readonly TarifaBusiness _tarifaBusiness;
        private readonly ReservaDAO _reservaDAO;
        private readonly DiariaDAO _diariaDAO;

        public ReservaBusiness(LocalPluginContext context)
        {
            _context = context;
            _quartoBusiness = new QuartoBusiness(context);
            _hospedeBusiness = new HospedeBusiness(context);
            _tarifaBusiness = new TarifaBusiness(context);
            _reservaDAO = new ReservaDAO(context);
            _diariaDAO = new DiariaDAO(context);
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

        public void ExecutarPreOperacao(Entity target, Entity preImage = null)
        {
            ReferenciasDaReserva reserva = ObterReferenciasEfetivas(target, preImage);
            Guid hotelDaReservaId = ObterHotelObrigatorio(reserva).Id;

            DateTime dataEntrada = ObterValorEfetivo<DateTime?>(target, preImage, Campos.DataEntrada) ??
                throw new InvalidPluginExecutionException(Mensagens.DataEntradaObrigatoria);

            DateTime dataSaida = ObterValorEfetivo<DateTime?>(target, preImage, Campos.DataSaida) ??
                throw new InvalidPluginExecutionException(Mensagens.DataSaidaObrigatoria);

            ValidarPeriodoReservaDisponivel(target.Id, reserva.Quarto.Id, dataEntrada, dataSaida);
        }

        public void ExecutarPosOperacaoSync(Entity target, Entity preImage = null)
        {
            ReferenciasDaReserva reserva = ObterReferenciasEfetivas(target, preImage);

            DateTime dataEntrada = ObterValorEfetivo<DateTime?>(target, preImage, Campos.DataEntrada) ??
                throw new InvalidPluginExecutionException(Mensagens.DataEntradaObrigatoria);

            DateTime dataSaida = ObterValorEfetivo<DateTime?>(target, preImage, Campos.DataSaida) ??
                throw new InvalidPluginExecutionException(Mensagens.DataSaidaObrigatoria);

            Money valorTotalInformado = ObterValorEfetivo<Money>(target, preImage, Campos.ValorTotal) ??
                throw new InvalidPluginExecutionException(Mensagens.ValorTotalNaoCalculado);

            CalcularValorDaReserva(target, valorTotalInformado, reserva, dataEntrada, dataSaida);
        }

        #region Regras da Reserva

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

        public void ValidarPeriodoReservaDisponivel(Guid reservaId, Guid quartoId, DateTime dataEntrada, DateTime dataSaida)
        {
            bool periodoEstaDisponivel = _reservaDAO
                .VerificarPeriodoDisponivel(quartoId, DesconsiderarCheckIn(dataEntrada), DesconsiderarCheckOut(dataSaida), reservaId);

            if (!periodoEstaDisponivel)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.PeriodoIndisponivel, dataEntrada, dataSaida));
        }

        private void CalcularValorDaReserva(Entity target, Money valorTotalInformado, ReferenciasDaReserva reserva, DateTime dataEntrada, DateTime dataSaida)
        {
            EntityReference tipoDoQuarto = _quartoBusiness.ObterTipoDeQuarto(reserva.Quarto);
            EntityCollection tarifasDoPeriodo = _tarifaBusiness.BuscarTarifasPorTipoQuartoEPeriodo(tipoDoQuarto, reserva.Hotel.Id, dataEntrada, dataSaida);

            EntityCollection diarias = CriarDiariasDaReserva(target.Id, tarifasDoPeriodo, dataEntrada, dataSaida);
            Money valorTotalDiarias = CalcularValorTotalDiarias(diarias);
            Money valorMedioDiarias = CalcularValorMedioDiarias(diarias);

            if (valorTotalDiarias.Value != valorTotalInformado.Value)
                throw new InvalidPluginExecutionException(Mensagens.ValorTotalDivergente);

            target[Campos.ValorDiaria] = valorMedioDiarias;
            _diariaDAO.CriarDiariasDaReserva(diarias);
        }

        private Money CalcularValorTotalDiarias(EntityCollection diarias)
        {
            return new Money(diarias.Entities.Sum(diaria => diaria.GetAttributeValue<Money>(Campos.ValorDiaria).Value));
        }

        private Money CalcularValorMedioDiarias(EntityCollection diarias)
        {
            return new Money(CalcularValorTotalDiarias(diarias).Value / diarias.Entities.Count);
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

        #region Gerar Diárias

        private EntityCollection CriarDiariasDaReserva(Guid reservaId, EntityCollection tarifas, DateTime dataEntrada, DateTime dataSaida)
        {
            EntityCollection diarias = new EntityCollection() { EntityName = Campos.ReservaDiaria };
            _diariaDAO.ExcluirDiariasDaReserva(reservaId);

            for (DateTime diaria = dataEntrada; diaria < dataSaida; diaria = diaria.AddDays(1))
            {
                Entity diariaCriada = CriarDiaria(reservaId, tarifas, diaria);

                if (diariaCriada != null)
                {
                    diarias.Entities.Add(diariaCriada);
                    continue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.SemTarifaParaDiaria, diaria));
            }

            return diarias;
        }

        private Entity CriarDiaria(Guid reservaId, EntityCollection tarifas, DateTime dataDiaria)
        {
            foreach (var tarifa in tarifas.Entities)
            {
                var dataInicialTarifa = tarifa.GetAttributeValue<DateTime?>(Campos.TarifaDataInicio) ??
                    throw new InvalidPluginExecutionException(Mensagens.TarifaSemDataInicial);

                var dataFinalTarifa = tarifa.GetAttributeValue<DateTime?>(Campos.TarifaDataFinal) ??
                    throw new InvalidPluginExecutionException(Mensagens.TarifaSemDataFinal);

                if (dataDiaria >= dataInicialTarifa && dataDiaria <= dataFinalTarifa)
                {
                    Entity diaria = new Entity(Campos.ReservaDiaria);
                    diaria[Campos.Reserva] = new EntityReference(Campos.Reserva, reservaId);
                    diaria[Campos.Tarifa] = new EntityReference(Campos.Tarifa, tarifa.Id);
                    diaria[Campos.DataDiaria] = dataDiaria;
                    diaria[Campos.ValorDiaria] = tarifa.GetAttributeValue<Money>(Campos.ValorDiaria);

                    return diaria;
                }
            }

            return null;
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

        private static T ObterValorEfetivo<T>(Entity target, Entity preImage, string atributo)
        {
            if (target.Contains(atributo))
                return target.GetAttributeValue<T>(atributo);

            return preImage != null ? preImage.GetAttributeValue<T>(atributo) : default(T);
        }

        private static EntityReference ObterHotelObrigatorio(ReferenciasDaReserva reserva)
        {
            return reserva.Hotel
                ?? throw new InvalidPluginExecutionException(Mensagens.HotelObrigatorio);
        }

        private static DateTime DesconsiderarCheckIn(DateTime dataEntrada)
        {
            return dataEntrada.AddDays(1);
        }

        private static DateTime DesconsiderarCheckOut(DateTime dataSaida)
        {
            return dataSaida.AddDays(-1);
        }

        #endregion
    }
}