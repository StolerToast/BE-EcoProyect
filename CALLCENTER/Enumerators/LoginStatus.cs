    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public enum LoginStatus
    {
        SuccessfullLogin,
        InvalidAgentOrPin,
        AgentAlreadyLoggedIn,
        InvalidStationId,
        InactiveStation,
        StationAlreadyInUse
    }

