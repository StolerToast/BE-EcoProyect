using smartbin.Models.Container;
public class ContainerListViewModel : JsonResponse
{
    public List<Container> Containers { get; set; }

    public static ContainerListViewModel GetResponse(List<Container> containers)
    {
        ContainerListViewModel r = new ContainerListViewModel();
        r.Status = 0;
        r.Containers = containers;
        return r;
    }
}
