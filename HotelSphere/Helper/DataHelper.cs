using System;

namespace HotelSphere.Helper
{
    public static class DataHelper
    {
        public static int CalcularIdade(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today.Date;

            int anos = hoje.Year - dataNascimento.Year;

            if (dataNascimento.Date > hoje.AddYears(-anos))
                anos--;

            return anos;
        }
    }
}
