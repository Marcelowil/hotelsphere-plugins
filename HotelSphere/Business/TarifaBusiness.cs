using HotelSphere.Base;
using HotelSphere.DAO;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class TarifaBusiness
    {
        public static class Campos
        {
            public const string Hotel = "hsp_hotel";
            public const string TipoDeQuarto = "hsp_tipodequarto";
            public const string DataInicio = "hsp_datainicio";
            public const string DataFinal = "hsp_datafinal";
            public const string ValorDiaria = "hsp_valordiaria";
            public const string NomeTarifa = "hsp_nome";
            public const string DataDiaria = "hsp_datadiaria";
        }

        public static class Mensagens
        {
            public const string HotelObrigatorio = "O hotel é obrigatório para a tarifa.";
            public const string TipoDeQuartoObrigatorio = "O tipo do quarto é obrigatório para a tarifa.";
            public const string DataDeInicioObrigatoria = "A data de início é obrigatória para a tarifa.";
            public const string DataFinalObrigatoria = "A data final é obrigatória para a tarifa.";
            public const string ConflitoTarifa = "Conflito de tarifa: já existe a tarifa \"{0}\" cadastrada para este hotel e tipo de quarto no período de {1:dd/MM/yyyy} a {2:dd/MM/yyyy}. Ajuste as datas informadas ({3:dd/MM/yyyy} a {4:dd/MM/yyyy}) ou altere o período da tarifa existente.";
            public const string TipoQuartoNaoPertenceAoHotel = "O tipo de quarto selecionado não pertence ao hotel.";
            public const string BloquearAlteracaoDadosPrincipais = "Não é possível alterar o hotel, tipo de quarto ou valor da diária, quando há diárias associadas a esta tarifa.";
            public const string DataInicioPosteriorADiariaMaisAntiga = "A nova data de início ({0:dd/MM/yyyy}) não pode ser posterior à data da diária mais antiga vinculada a esta tarifa ({1:dd/MM/yyyy}). Altere a data de início para uma data igual ou anterior à da diária mais antiga.";
            public const string DataFinalAnteriorADiariaMaisRecente = "A nova data final ({0:dd/MM/yyyy}) não pode ser anterior à data da diária mais recente vinculada a esta tarifa ({1:dd/MM/yyyy}). Altere a data final para uma data igual ou posterior à da diária mais recente.";
        }

        private readonly LocalPluginContext _context;
        private readonly TarifaDAO _tarifaDAO;
        private readonly TipoQuartoBusiness _tipoQuartoBusiness;
        private readonly ReservaDAO _reservaDAO;
        private readonly DiariaDAO _diariaDAO;

        public TarifaBusiness(LocalPluginContext context)
        {
            _context = context;
            _tarifaDAO = new TarifaDAO(_context);
            _tipoQuartoBusiness = new TipoQuartoBusiness(context);
            _reservaDAO = new ReservaDAO(context);
            _diariaDAO = new DiariaDAO(context);
        }

        public void ValidarCriacao(Entity target)
        {
            EntityReference hotel = target.GetAttributeValue<EntityReference>(Campos.Hotel) ??
                throw new InvalidPluginExecutionException(Mensagens.HotelObrigatorio);

            EntityReference tipoQuarto = target.GetAttributeValue<EntityReference>(Campos.TipoDeQuarto) ??
                throw new InvalidPluginExecutionException(Mensagens.TipoDeQuartoObrigatorio);

            DateTime dataInicio = target.GetAttributeValue<DateTime?>(Campos.DataInicio) ??
                throw new InvalidPluginExecutionException(Mensagens.DataDeInicioObrigatoria);

            DateTime dataFinal = target.GetAttributeValue<DateTime?>(Campos.DataFinal) ??
                throw new InvalidPluginExecutionException(Mensagens.DataFinalObrigatoria);

            ValidarTipoDeQuartoPertenceAoHotel(tipoQuarto, hotel);
            ValidarDuplicidadeTarifa(hotel, tipoQuarto, dataInicio, dataFinal);
        }

        public void ValidarAtualiazacao(Entity target, Entity preImage)
        {
            ValidarTarifaAssociadaADiaria(target);

            EntityReference hotel = ObterValorEfetivo<EntityReference>(target, preImage, Campos.Hotel);
            EntityReference tipoQuarto = ObterValorEfetivo<EntityReference>(target, preImage, Campos.TipoDeQuarto);
            DateTime dataInicio = ObterValorEfetivo<DateTime>(target, preImage, Campos.DataInicio);
            DateTime dataFinal = ObterValorEfetivo<DateTime>(target, preImage, Campos.DataFinal);

            if (target.Contains(Campos.TipoDeQuarto) || target.Contains(Campos.Hotel))
                ValidarTipoDeQuartoPertenceAoHotel(tipoQuarto, hotel);

            if (target.Contains(Campos.DataInicio) || target.Contains(Campos.DataFinal))
                ValidarDuplicidadeTarifa(hotel, tipoQuarto, dataInicio, dataFinal, target.Id);
        }

        #region Regras da Tarifa
        private void ValidarDuplicidadeTarifa(EntityReference hotel, EntityReference tipoQuarto, DateTime dataInicio, DateTime dataFinal, Guid? tarifaId = null)
        {
            EntityCollection tarifas = _tarifaDAO.BuscarTarifasPorHotelETipoQuarto(tarifaId, hotel.Id, tipoQuarto.Id);

            foreach (Entity tarifa in tarifas.Entities)
            {
                DateTime tarifaInicio = tarifa.GetAttributeValue<DateTime>(Campos.DataInicio).Date;
                DateTime tarifaFim = tarifa.GetAttributeValue<DateTime>(Campos.DataFinal).Date;

                bool periodoDisponivel = dataInicio.Date > tarifaFim || dataFinal.Date < tarifaInicio;

                if (!periodoDisponivel)
                {
                    string nomeTarifa = tarifa.GetAttributeValue<string>(Campos.NomeTarifa) ?? "(sem nome)";

                    throw new InvalidPluginExecutionException(
                        string.Format(Mensagens.ConflitoTarifa, nomeTarifa, tarifaInicio, tarifaFim, dataInicio, dataFinal));
                }
            }
        }

        private void ValidarTarifaAssociadaADiaria(Entity target)
        {
            Entity diariaMaisRecente = _diariaDAO.ObterDiariaMaisRecentePorTarifa(target.Id);
            Entity diariaMaisAntiga = _diariaDAO.ObterDiariaMaisAntigaPorTarifa(target.Id);

            bool temDiariasAssociadasATarifa = diariaMaisRecente != null || diariaMaisAntiga != null;

            if (!temDiariasAssociadasATarifa)
                return;

            ValidarDadosPrincipaisNaoAlterados(target);
            ValidarDataInicioNaoPosteriorADiariaMaisAntiga(target, diariaMaisAntiga);
            ValidarDataFinalNaoAnteriorADiariaMaisRecente(target, diariaMaisRecente);
        }

        private static void ValidarDadosPrincipaisNaoAlterados(Entity target)
        {
            bool valoresPrincipaisAlterados = target.Contains(Campos.ValorDiaria) || target.Contains(Campos.Hotel) || target.Contains(Campos.TipoDeQuarto);

            if (valoresPrincipaisAlterados)
                throw new InvalidPluginExecutionException(Mensagens.BloquearAlteracaoDadosPrincipais);
        }

        private static void ValidarDataInicioNaoPosteriorADiariaMaisAntiga(Entity target, Entity diariaMaisAntiga)
        {
            if (!target.TryGetAttributeValue(Campos.DataInicio, out DateTime dataInicioTarifa))
                return;

            DateTime dataDiariaMaisAntiga = diariaMaisAntiga.GetAttributeValue<DateTime>(Campos.DataDiaria);

            if (dataInicioTarifa.Date > dataDiariaMaisAntiga.Date)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.DataInicioPosteriorADiariaMaisAntiga, dataInicioTarifa, dataDiariaMaisAntiga));
            }
        }

        private static void ValidarDataFinalNaoAnteriorADiariaMaisRecente(Entity target, Entity diariaMaisRecente)
        {
            if (!target.TryGetAttributeValue(Campos.DataFinal, out DateTime dataFinalTarifa))
                return;

            DateTime dataDiariaMaisRecente = diariaMaisRecente.GetAttributeValue<DateTime>(Campos.DataDiaria);

            if (dataFinalTarifa.Date < dataDiariaMaisRecente.Date)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.DataFinalAnteriorADiariaMaisRecente, dataFinalTarifa, dataDiariaMaisRecente));
            }
        }

        public void ValidarTipoDeQuartoPertenceAoHotel(EntityReference tipoQuarto, EntityReference hotel)
        {
            if (!_tipoQuartoBusiness.TipoQuartoPertenceAoHotel(tipoQuarto.Id, hotel.Id))
                throw new InvalidPluginExecutionException(Mensagens.TipoQuartoNaoPertenceAoHotel);
        }

        #endregion

        #region Consultas

        public EntityCollection BuscarTarifasPorTipoQuartoEPeriodo(EntityReference tipoQuartoRef, Guid hotelId, DateTime dataInicio, DateTime dataFinal)
        {
            return _tarifaDAO.BuscarPorTipoDeQuartoEPeriodo(tipoQuartoRef.Id, hotelId, dataInicio, dataFinal);
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
