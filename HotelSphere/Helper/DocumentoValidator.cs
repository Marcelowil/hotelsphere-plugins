using System.Linq;
using System.Text.RegularExpressions;

namespace HotelSphere.Helper
{
    public static class DocumentoValidator
    {
        public static bool CpfEhValido(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            cpf = RemoverMascara(cpf);

            if (cpf.Length != 11 || !cpf.All(char.IsDigit))
                return false;

            if (cpf.All(c => c == cpf[0]))
                return false;

            int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;

            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;
            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        public static bool CnpjEhValido(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            cnpj = RemoverMascara(cnpj);

            if (cnpj.Length != 14)
                return false;

            if (cnpj.All(c => c == cnpj[0]))
                return false;

            return cnpj.All(char.IsDigit) ? ValidarCnpjNumerico(cnpj) : ValidarCnpjAlfanumerico(cnpj);
        }

        public static bool CpfOuCnpjEhValido(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return false;

            string limpo = RemoverMascara(documento);

            if (limpo.Length == 11)
                return CpfEhValido(documento);

            if (limpo.Length == 14)
                return CnpjEhValido(documento);

            return false;
        }

        private static string RemoverMascara(string documento)
        {
            return Regex.Replace(documento, @"[^0-9A-Za-z]", string.Empty).ToUpperInvariant();
        }

        private static bool ValidarCnpjNumerico(string cnpj)
        {
            int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;

            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;
            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        private static bool ValidarCnpjAlfanumerico(string cnpj)
        {
            if (!cnpj.All(char.IsLetterOrDigit))
                return false;

            int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int soma = 0;
            for (int i = 0; i < 12; i++)
                soma += ValorCaractere(cnpj[i]) * pesos1[i];

            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            soma = 0;
            for (int i = 0; i < 12; i++)
                soma += ValorCaractere(cnpj[i]) * pesos2[i];
            soma += digito1 * pesos2[12];

            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return (cnpj[12] - '0') == digito1 && (cnpj[13] - '0') == digito2;
        }

        private static int ValorCaractere(char c)
        {
            return c - '0';
        }
    }
}
