Distribuirani sistemi

Windows Communication Foundation (WCF)

Sadržaj

 Uvod

 API

 Primer

Interoperabilnost

WEB service standardi

 Messaging: SOAP

 Metadata: The Web Services
Description Language (WSDL)

 Security: WS-Security, WS-Trust i

WS-SecureConversation

 Reliability: WS-ReliableMessaging

 Transactions: Zasnovano na WS-

Coordination, WS-
AtomicTransaction

Kreiranje WCF servisa

 Servis klasa generisana u C# (ili VB ili nekom drugom CLR

zasnovanom jeziku) koja implementira jednu ili više metoda.

 Host proces u kojem servis radi.

 Jedan ili više endpoint-a koji omogućava klijentima pristup
servisu. Sva komunikacija sa WCF servisom se dešava preko
servisovog endpoint-a.

Primer servisa

using System.ServiceModel;

using System.ServiceModel;

[ServiceContract]
class NazivKlase
{

[OperationContract]
public void NekaMetoda(…)
{
…
}
…

}

[DataContract]
public class CekaonicaStatus
{

[DataMember]
public Guid LekarUiD { get; set; }
[DataMember]
public string Pacijent { get; set; }
[DataMember]
public Status Status { get; set; }

}

[ServiceContract]
public interface INazivInterfejsa
{

[OperationContract]
bool Check(object data);

}

class NazivKlase : INazivInterfejsa
{

public bool Check(object data)
{

…

return true;

}

}

Definisanje endpoint-a

<configuration>

<system.serviceModel>

<services>

<service type=“NazivKlase.NazivApp">

<endpoint

contract="INazivInterfejsa"
binding=”basicHttpBinding”
address=

"http://hostSajt/implementacijaServisa.svc"/>

</service>

</services>

</system.serviceModel>

</configuration>

Kreiranje WCF klijenta

Kreiranje WCF klijenta – kod

 NazivKlaseClient proxy;

 proxy = new NazivKlaseClient();

 proxy.NazivMetode();

 proxy.NazivMetodeAsync();