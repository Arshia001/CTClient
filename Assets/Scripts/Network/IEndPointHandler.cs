using System.Collections;
using System.Collections.Generic;

namespace Network
{
    public interface IEndPointHandler
    {
        void SetupHub(ConnectionManager ConnectionManager);
    }
}
