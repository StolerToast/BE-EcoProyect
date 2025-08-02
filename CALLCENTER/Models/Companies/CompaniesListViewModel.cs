namespace smartbin.Models.Companies
{
    public class CompaniesListViewModel : JsonResponse
    {
        public dynamic Companies { get; set; }

        public static CompaniesListViewModel GetResponse(dynamic companies)
        {
            return new CompaniesListViewModel
            {
                Status = 0,
                Companies = companies
            };
        }
    }
}