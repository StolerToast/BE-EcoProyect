using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StationNotFoundException : Exception
{
    private string _message;

    public override string Message => _message;

    public StationNotFoundException(int id)
    {
        _message = "Could not find station with id " + id.ToString();
    }
}