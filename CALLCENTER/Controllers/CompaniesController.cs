using Microsoft.AspNetCore.Mvc;
using smartbin.Models.Companies;
using smartbin.PostModels;
using System;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        [HttpGet]
        public ActionResult GetAll()
        {
            try
            {
                var companies = Companies.GetAll();
                return Ok(CompaniesListViewModel.GetResponse(companies));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("{companyId}")]
        public ActionResult GetById(string companyId)
        {
            try
            {
                var company = Companies.GetById(companyId);
                if (company == null)
                    return NotFound(new { status = 1, message = "Empresa no encontrada" });

                return Ok(CompaniesListViewModel.GetResponse(company));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public ActionResult Insert([FromBody] PostCompanies nuevaCompany)
        {
            try
            {
                var result = Companies.InsertWithTransaction(nuevaCompany);
                return Ok(new
                {
                    status = 0,
                    message = "Empresa creada",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 2,
                    message = $"Error al insertar: {ex.Message}"
                });
            }
        }

        [HttpPut("{companyId}")]
        public ActionResult Update(string companyId, [FromBody] PostCompanies updatedCompany)
        {
            try
            {
                bool success = Companies.UpdateWithTransaction(companyId, updatedCompany);
                return success
                    ? Ok(new { status = 0, message = "Empresa actualizada" })
                    : NotFound(new { status = 1, message = "Empresa no encontrada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 2,
                    message = $"Error al actualizar: {ex.Message}"
                });
            }
        }

        [HttpDelete("{companyId}")]
        public ActionResult Deactivate(string companyId)
        {
            try
            {
                bool success = Companies.DeactivateWithTransaction(companyId);
                return success
                    ? Ok(new { status = 0, message = "Empresa desactivada" })
                    : NotFound(new { status = 1, message = "Empresa no encontrada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 2,
                    message = $"Error al desactivar: {ex.Message}"
                });
            }
        }

        [HttpGet("map")]
        public ActionResult GetMapData()
        {
            try
            {
                var mapData = Companies.GetMapData();
                return Ok(new
                {
                    status = 0,
                    data = mapData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 1,
                    message = $"Error al obtener datos para mapa: {ex.Message}"
                });
            }
        }
    }
}