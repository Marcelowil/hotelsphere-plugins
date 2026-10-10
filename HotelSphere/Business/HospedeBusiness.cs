using HotelSphere.Base;
using HotelSphere.DAO;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class HospedeBusiness
    {
        private const int IdadeMinimaResponsavel = 18;

        private static class Campos
        {
            public const string Nome = "firstname";
            public const string Sobrenome = "lastname";
            public const string Cpf = "hsp_cpf";
            public const string Hotel = "hsp_hotel";
            public const string DataNascimento = "hsp_datanascimento";
            public const string PreferenciaDeQuarto = "hsp_preferenciaquarto";
        }

        private static class Mensagens
        {
            public const string HospedeObrigatorio = "O hóspede é obrigatório para a reserva.";
            public const string HospedeNaoPertenceAoHotel = "O hóspede não pertence ao hotel da reserva.";
            public const string HospedeSemHotel = "O hóspede não possui hotel vinculado.";
            public const string HospedeSemDataNascimento = "O hóspede responsável não possui data de nascimento informada.";
            public const string HospedeResponsavelMenorDeIdade = "O hóspede responsável deve ter pelo menos {0} anos.";
            public const string CpfJaCadastrado = "O CPF {0} já possui cadastro neste hotel.";
            public const string CpfObrigatorio = "O hóspede não possui um CPF vinculado.";
            public const string FormatoCpfInvalido = "O CPF preenchido é inválido.";
            public const string DataNascimentoNoFuturo = "A data de nascimento não pode ser maior que o dia de hoje.";
        }

        private readonly LocalPluginContext _context;
        private readonly HospedeDAO _hospedeDAO;

        public HospedeBusiness(LocalPluginContext context)
        {
            _context = context;
            _hospedeDAO = new HospedeDAO(context);
        }

        #region Métodos públicos

        public void ValidarCriacao(Entity target)
        {
            ValidarDataNascimentoFutura(target);
            ValidarCpf(target);
            ConsultarCpfCadastradoPorHotel(target);
        }

        public void ValidarAtualizacao(Entity target, Entity preImage)
        {
            bool cpfAlterado = target.Contains(Campos.Cpf);
            bool hotelAlterado = target.Contains(Campos.Hotel);

            if (target.Contains(Campos.DataNascimento))
                ValidarDataNascimentoFutura(target);

            if (cpfAlterado)
                ValidarCpf(target);

            if (cpfAlterado || hotelAlterado)
                ConsultarCpfCadastradoPorHotel(target, preImage);
        }

        public void ValidarParaReserva(EntityReference hospedeRef, Guid hotelDaReserva)
        {
            Entity hospede = CarregarHospede(hospedeRef);

            ValidarHospedePertenceHotel(hospede, hotelDaReserva);
            ValidarResponsavelMaiorDeIdade(hospede);
        }

        #endregion

        #region Regras do cadastro do hóspede

        private void ValidarDataNascimentoFutura(Entity hospede)
        {
            DateTime dataNascimento = ExigirDataNascimento(hospede);

            if (dataNascimento > DateTime.Today)
                throw new InvalidPluginExecutionException(Mensagens.DataNascimentoNoFuturo);
        }

        private void ValidarCpf(Entity target)
        {
            string cpf = target.GetAttributeValue<string>(Campos.Cpf) ??
                throw new InvalidPluginExecutionException(Mensagens.CpfObrigatorio);

            if (!DocumentoValidator.CpfEhValido(cpf))
                throw new InvalidPluginExecutionException(Mensagens.FormatoCpfInvalido);
        }

        private void ConsultarCpfCadastradoPorHotel(Entity target, Entity preImage = null)
        {
            string cpf = ObterValorEfetivo<string>(target, preImage, Campos.Cpf);
            EntityReference hotel = ObterValorEfetivo<EntityReference>(target, preImage, Campos.Hotel);

            if (hotel == null || string.IsNullOrEmpty(cpf))
                return;

            if (_hospedeDAO.ExisteCpfPorHotel(cpf, hotel, target.Id))
                throw new InvalidPluginExecutionException(string.Format(Mensagens.CpfJaCadastrado, cpf));
        }

        #endregion

        #region Regras para a reserva

        private Entity CarregarHospede(EntityReference hospedeRef)
        {
            if (hospedeRef == null)
                throw new InvalidPluginExecutionException(Mensagens.HospedeObrigatorio);

            return _hospedeDAO.ObterPorId(hospedeRef.Id) ??
                throw new InvalidPluginExecutionException(Mensagens.HospedeObrigatorio);
        }

        private static void ValidarHospedePertenceHotel(Entity hospede, Guid hotelDaReserva)
        {
            EntityReference hotelDoHospede = hospede.GetAttributeValue<EntityReference>(Campos.Hotel)
                ?? throw new InvalidPluginExecutionException(Mensagens.HospedeSemHotel);

            if (hotelDoHospede.Id != hotelDaReserva)
                throw new InvalidPluginExecutionException(Mensagens.HospedeNaoPertenceAoHotel);
        }

        private static void ValidarResponsavelMaiorDeIdade(Entity hospede)
        {
            DateTime dataNascimento = ExigirDataNascimento(hospede);

            int idade = DataHelper.CalcularIdade(dataNascimento);

            if (idade < IdadeMinimaResponsavel)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.HospedeResponsavelMenorDeIdade, IdadeMinimaResponsavel));
        }

        #endregion

        #region Auxiliares

        private static DateTime ExigirDataNascimento(Entity hospede)
        {
            return hospede.GetAttributeValue<DateTime?>(Campos.DataNascimento) ??
                throw new InvalidPluginExecutionException(Mensagens.HospedeSemDataNascimento);
        }

        private static T ObterValorEfetivo<T>(Entity target, Entity preImage, string atributo)
        {
            if (target.Contains(atributo))
                return target.GetAttributeValue<T>(atributo);

            return preImage != null ? preImage.GetAttributeValue<T>(atributo) : default(T);
        }

        #endregion
    }
}