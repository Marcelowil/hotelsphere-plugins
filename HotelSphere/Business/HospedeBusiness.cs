using HotelSphere.Base;
using HotelSphere.Helper;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class HospedeBusiness
    {
        private readonly LocalPluginContext _context;

        public HospedeBusiness(LocalPluginContext context)
        {
            _context = context;
        }

        public void ValidarDataNascimentoFutura(Entity target)
        {
            var dataNascimento = target.Contains("hsp_datanascimento") ? target.GetAttributeValue<DateTime>("hsp_datanascimento")
                : throw new InvalidPluginExecutionException("Não é possível salvar um hóspede sem a data de nascimento preenchida.");

            if (dataNascimento > DateTime.Today)
                throw new InvalidPluginExecutionException("A data de nascimento não pode ser maior que o dia de hoje.");
        }

        public void ValidarCpf(Entity target)
        {
            string cpf = target.Contains("hsp_cpf") ? target.GetAttributeValue<string>("hsp_cpf")
                : throw new InvalidPluginExecutionException("Não é possível salvar um hóspede sem o CPF preenchido.");

            if (!DocumentoValidator.CpfOuCnpjEhValido(cpf))
                throw new InvalidPluginExecutionException("O CPF preenchido é inválido.");
        }
    }
}
