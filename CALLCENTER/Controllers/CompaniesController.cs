using Microsoft.AspNetCore.Mvc;
using smartbin.Models.Companies;
using smartbin.PostModels;

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

        [HttpPost]
        public ActionResult Insert([FromBody] PostCompanies nuevaCompanies)
        {
            try
            {
                var result = Companies.Insert(nuevaCompanies);
                return Ok(new { status = 0, message = "Company created", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = $"Error al insertar: {ex.Message}" });
            }
        }
    }
}