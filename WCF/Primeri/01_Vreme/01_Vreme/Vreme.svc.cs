using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace _01_Vreme
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class Vreme : IVreme
    {
        DateTime vreme;
        public Vreme()
        {
            vreme = DateTime.Now;
        }

        public DateTime GetVreme()
        {
            return DateTime.Now;
        }

        public VremeDodatno GetDataUsingDataContract(VremeDodatno composite)
        {
            VremeDodatno r = new VremeDodatno();
            r.PrvoPokretanje = vreme;
            r.StringValue = $"Vreme proteklo od startovanja sesisje je:{composite.PrvoPokretanje - vreme}";
            return r;
        }
    }
}
