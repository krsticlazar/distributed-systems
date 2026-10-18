using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace T2CB
{
    
    public interface IPrimerCallback
    {

        [OperationContract(IsOneWay = true)]
        void OnPrimerCallback();

        [OperationContract]
        int HowLongToDelay();
    }
}
