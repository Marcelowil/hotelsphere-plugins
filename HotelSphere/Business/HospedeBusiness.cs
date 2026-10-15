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
            public const string NomeCompleto = "fullname";
            public const string Cpf = "hsp_cpf";
            public const string Hotel = "hsp_hotel";
            public const string DataNascimento = "hsp_datanascimento";
            public const string PreferenciaDeQuarto = "hsp_preferenciaquarto";
            public const string Status = "statuscode";
            public const int StatusAtivo = 1;
            public const int StatusInativo = 2;
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
            public const string HospedeInativo = "O hóspede {0} está inativo.";
            public const string HospedeSemStatus = "O hóspede não possuí status.";
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

        #region Consultas

        public Entity ObterHospedePorId(EntityReference hospedeRef)
        {
            if (hospedeRef == null)
                throw new InvalidPluginExecutionException(Mensagens.HospedeObrigatorio);

            return _hospedeDAO.ObterPorId(hospedeRef.Id) ??
                throw new InvalidPluginExecutionException(Mensagens.HospedeObrigatorio);
        }

        public EntityCollection ObterHospedesPorIdDaReserva(Guid reservaId)
        {
            return _hospedeDAO.ObterPorReserva(reservaId);
        }

        #endregion

        #region Auxiliares

        private static DateTime ExigirDataNascimento(Entity hospede)
        {
            return hospede.GetAttributeValue<DateTime?>(Campos.DataNascimento) ??
                throw new InvalidPluginExecutionException(Mensagens.HospedeSemDataNascimento);
        }

        public int ObterIdade(Entity hospede)
        {
            DateTime dataNascimento = ExigirDataNascimento(hospede);

            return DataHelper.CalcularIdade(dataNascimento);
        }

        public void ValidarHospedeAtivo(Entity hospede)
        {
            int? status = hospede.GetAttributeValue<OptionSetValue>(Campos.Status)?.Value ??
                throw new InvalidPluginExecutionException(Mensagens.HospedeSemStatus);

            if (status != Campos.StatusAtivo)
                throw new InvalidPluginExecutionException(
                    string.Format(Mensagens.HospedeInativo, hospede.GetAttributeValue<string>(Campos.NomeCompleto)));
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