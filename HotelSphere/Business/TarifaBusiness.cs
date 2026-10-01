using HotelSphere.Base;
using HotelSphere.DAO;
using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Business
{
    public class TarifaBusiness
    {
        private readonly LocalPluginContext _context;
        private readonly TarifaDAO _tarifaDAO;

        public TarifaBusiness(LocalPluginContext context)
        {
            _context = context;
            _tarifaDAO = new TarifaDAO(_context);
        }

        public void ValidarDuplicidadeTarifa(Entity target, Entity preImage = null)
        {
            EntityReference hotel = preImage == null ? target.GetAttributeValue<EntityReference>("hsp_hotel") :
                preImage.GetAttributeValue<EntityReference>("hsp_hotel");

            EntityReference tipoQuarto = preImage == null ? target.GetAttributeValue<EntityReference>("hsp_tipodequarto") :
                preImage.GetAttributeValue<EntityReference>("hsp_tipodequarto");

            DateTime dataInicio = target.Contains("hsp_datainicio") ? target.GetAttributeValue<DateTime>("hsp_datainicio") :
                preImage.GetAttributeValue<DateTime>("hsp_datainicio");

            DateTime dataFim = target.Contains("hsp_datafinal") ? target.GetAttributeValue<DateTime>("hsp_datafinal") :
                preImage.GetAttributeValue<DateTime>("hsp_datafinal");

            EntityCollection tarifas = _tarifaDAO.BuscarTarifasPorHotelETipoQuarto(target.Id, hotel.Id, tipoQuarto.Id);

            foreach (Entity tarifa in tarifas.Entities)
            {
                DateTime tarifaInicio = tarifa.GetAttributeValue<DateTime>("hsp_datainicio").Date;
                DateTime tarifaFim = tarifa.GetAttributeValue<DateTime>("hsp_datafinal").Date;

                bool periodoDisponivel = dataInicio.Date > tarifaFim || dataFim.Date < tarifaInicio;

                if (!periodoDisponivel)
                {
                    string nomeTarifa = tarifa.GetAttributeValue<string>("hsp_nome") ?? "(sem nome)";

                    throw new InvalidPluginExecutionException(
                        $"Conflito de tarifa: já existe a tarifa \"{nomeTarifa}\" cadastrada para este hotel e tipo de quarto " +
                        $"no período de {tarifaInicio:dd/MM/yyyy} a {tarifaFim:dd/MM/yyyy}. " +
                        $"Ajuste as datas informadas ({dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}) ou altere o período da tarifa existente.");
                }
            }
        }
    }
}
