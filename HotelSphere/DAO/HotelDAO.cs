using HotelSphere.Base;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace HotelSphere.DAO
{
    public class HotelDAO
    {
        private readonly LocalPluginContext _context;

        public HotelDAO(LocalPluginContext context)
        {
            _context = context;
        }

        public bool ExisteCpnjCadastrado(string cnpj)
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("accountid"),
                TopCount = 1,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("hsp_cnpj", ConditionOperator.Equal, cnpj)
                    }
                }
            };

            return _context.Service.RetrieveMultiple(query).Entities.Any();
        }
    }
}