# WCFServiceMatrice: objasnjenje zavrsenog zadatka

Ovaj dokument objasnjava sta je uradjeno u projektu `WCFServiceMatrice`, zasto je tako uradjeno i kako da ga citas za lab/ispit.

Zadatak:

```text
Koristeci WCF napisati sistem za rad aritmetickih operacija nad matricama.
Operacije: setovanje matrice na serverskoj strani, sabiranje,
mnozenje matrice skalarom, mnozenje matrice matricom,
transponovanje matrice i preuzimanje matrice sa servisa.
```

Bitna recenica:

```text
Operacije kao jedan od operanada uzimaju matricu koja je setovana na serverskoj strani.
```

To znaci:

```text
1. Klijent posalje matricu A na server.
2. Server zapamti matricu A.
3. Svaka sledeca operacija koristi A kao prvi operand.
```

Primer:

```csharp
proxy.Set(A);
Result r = proxy.Add(B);
```

Matematicki:

```text
r = A + B
```

## 1. Struktura projekta

Solution:

```text
WCFServiceMatrice
  WcfService1     WCF Service Application
  ConsoleApp1     Console App klijent
```

Server fajlovi:

```text
WcfService1
  IServiceMatrice.cs
  ServiceMatrice.svc
  ServiceMatrice.svc.cs
  Web.config
```

Klijent fajlovi:

```text
ConsoleApp1
  Program.cs
  App.config
```

## 2. Sta je bilo nezavrseno

Pre dorade, projekat je imao nekoliko problema:

- `ServiceMatrice.svc` je pokazivao na `WcfService1.Service1`, a stvarna klasa je `ServiceMatrice`;
- `Set` nije bio implementiran;
- `Take` nije bio implementiran;
- `Multiply` je racunao pogresno jer je upisivao rezultat preko postojece matrice;
- `Transpond` nije transponovao matricu, nego je sabirao elemente;
- `Result` se nije dosledno popunjavao;
- `Web.config` nije imao eksplicitno definisan `service` i endpoint;
- konzolni klijent je bio prazan.

Sada je sve povezano:

- server ima validan ugovor;
- servis radi sve trazene operacije;
- svaka operacija vraca `Result`;
- klijent poziva svaku operaciju i stampa rezultat.

## 3. Matrica kao DataContract

U `IServiceMatrice.cs` matrica je definisana ovako:

```csharp
[DataContract]
public class Matrica
{
    [DataMember]
    public int Rows { get; set; }

    [DataMember]
    public int Columns { get; set; }

    [DataMember]
    public decimal[] Values { get; set; }
}
```

Koristimo:

- `Rows`: broj redova;
- `Columns`: broj kolona;
- `Values`: sve vrednosti matrice u jednom nizu.

Nisam ostavio `decimal[,]` zato sto je za WCF sigurnije i jednostavnije da se kroz servis salje jednodimenzioni niz. WCF lako serijalizuje `decimal[]`, a mi sami znamo kako da izracunamo poziciju elementa.

Element na poziciji `(row, column)` se nalazi na indeksu:

```text
index = row * Columns + column
```

Primer matrice:

```text
1 2
3 4
```

u nizu se cuva kao:

```text
[1, 2, 3, 4]
```

Za `Rows = 2`, `Columns = 2`:

```text
(0,0) -> 0 * 2 + 0 = 0 -> 1
(0,1) -> 0 * 2 + 1 = 1 -> 2
(1,0) -> 1 * 2 + 0 = 2 -> 3
(1,1) -> 1 * 2 + 1 = 3 -> 4
```

Zato u klasi postoje pomocne metode:

```csharp
public decimal Get(int row, int column)
{
    return Values[row * Columns + column];
}

public void SetValue(int row, int column, decimal value)
{
    Values[row * Columns + column] = value;
}
```

## 4. Result kao zajednicki odgovor

Zadatak trazi da svaka operacija vrati:

- da li je doslo do greske;
- opis greske;
- matricu ako je operacija uspela.

Zato postoji:

```csharp
[DataContract]
public class Result
{
    [DataMember]
    public bool IsError { get; set; }

    [DataMember]
    public string ErrorMessage { get; set; }

    [DataMember]
    public Matrica Matrica { get; set; }
}
```

Ako je sve dobro:

```csharp
return new Result
{
    IsError = false,
    ErrorMessage = null,
    Matrica = result
};
```

Ako postoji greska:

```csharp
return new Result
{
    IsError = true,
    ErrorMessage = "Opis greske",
    Matrica = null
};
```

Ovo je namerno jednostavno. Nema potrebe da se za ovaj zadatak baca `FaultException`, jer tekst zadatka eksplicitno trazi flag i opis greske u rezultatu.

## 5. Serverska matrica

U `ServiceMatrice.svc.cs` postoji staticka promenljiva:

```csharp
private static Matrica matrica;
```

Ona predstavlja matricu setovanu na serverskoj strani.

Zasto `static`?

WCF servisna klasa moze da se instancira vise puta. Ako bismo cuvali matricu u obicnom polju, nije sigurno da bi sledeci poziv video istu vrednost. Staticko polje je zajednicko za sve instance klase.

Zasto `lock`?

Vise klijenata moze pozvati servis u isto vreme. `lock` obezbedjuje da citanje i pisanje serverske matrice ne udare jedno u drugo.

```csharp
private static readonly object LockObj = new object();
```

## 6. Set

`Set` postavlja serversku matricu.

```csharp
public Result Set(Matrica m)
```

Redosled:

1. Proveri da li je matrica validna.
2. Ako nije, vrati `Result` sa greskom.
3. Ako jeste, kopiraj matricu u serversko stanje.
4. Vrati kopiju setovane matrice.

Zasto kopija?

Da se ne bi direktno delio isti objekat koji je dosao od klijenta. U praksi je WCF svakako serijalizuje/deserijalizuje, ali kopija je cistija logika.

## 7. Take

`Take` vraca trenutnu serversku matricu.

```csharp
public Result Take()
```

Ako matrica nije setovana:

```text
Serverska matrica nije setovana.
```

Ako jeste setovana, vraca kopiju matrice.

Ova metoda je korisna da proverimo da li je `Set` stvarno uspeo.

## 8. Sabiranje matrica

Sabiranje je definisano samo ako matrice imaju iste dimenzije.

Ako je:

```text
A = [1 2]
    [3 4]

B = [10 20]
    [30 40]
```

onda:

```text
A + B = [11 22]
        [33 44]
```

Kod radi:

```csharp
result[i, j] = matrica[i, j] + m[i, j]
```

U implementaciji:

```csharp
result.SetValue(i, j, matrica.Get(i, j) + m.Get(i, j));
```

Ako dimenzije nisu iste, vraca se:

```text
Za sabiranje matrice moraju imati isti broj redova i kolona.
```

## 9. Mnozenje matrice skalarom

Skalar je jedan broj.

Ako je:

```text
A = [1 2]
    [3 4]
```

i skalar je `3`, onda:

```text
3A = [3  6]
     [9 12]
```

Kod radi:

```csharp
result[i, j] = matrica[i, j] * d
```

U implementaciji:

```csharp
result.SetValue(i, j, matrica.Get(i, j) * d);
```

## 10. Mnozenje matrica

Mnozenje matrica nije isto kao sabiranje. Uslov je:

```text
broj kolona prve matrice = broj redova druge matrice
```

Ako je:

```text
A dimenzije: 2 x 3
B dimenzije: 3 x 4
```

onda je rezultat:

```text
A * B dimenzije: 2 x 4
```

Formula:

```text
C[i,j] = suma A[i,k] * B[k,j]
```

za sve `k`.

U kodu:

```csharp
decimal sum = 0;
for (int k = 0; k < matrica.Columns; k++)
{
    sum += matrica.Get(i, k) * m.Get(k, j);
}
result.SetValue(i, j, sum);
```

Primer iz klijenta:

```text
A = [1 2]
    [3 4]

B = [5 6]
    [7 8]
```

Rezultat:

```text
A * B = [19 22]
        [43 50]
```

Racun:

```text
19 = 1*5 + 2*7
22 = 1*6 + 2*8
43 = 3*5 + 4*7
50 = 3*6 + 4*8
```

## 11. Transponovanje

Transponovanje menja redove i kolone.

Ako je:

```text
A = [1 2]
    [3 4]
```

onda je:

```text
A^T = [1 3]
      [2 4]
```

Formula:

```text
result[j,i] = A[i,j]
```

U kodu:

```csharp
result.SetValue(j, i, matrica.Get(i, j));
```

Bitno: ranija verzija nije ovo radila. Ona je imala:

```csharp
matrica.Mat[i,j] += matrica.Mat[j,i];
```

To nije transponovanje. To menja vrednosti sabiranjem, a transponovanje samo zamenjuje pozicije.

## 12. Web.config servera

Server koristi obican `basicHttpBinding`, jer nema callback-a ni sesija.

Najbitniji deo:

```xml
<services>
  <service name="WcfService1.ServiceMatrice"
           behaviorConfiguration="ServiceMatriceBehavior">
    <endpoint address=""
              binding="basicHttpBinding"
              contract="WcfService1.IServiceMatrice" />

    <endpoint address="mex"
              binding="mexHttpBinding"
              contract="IMetadataExchange" />
  </service>
</services>
```

Ovo mora da se poklopi:

```text
service name = namespace + servisna klasa
contract     = namespace + interfejs
```

Kod nas:

```text
service name = WcfService1.ServiceMatrice
contract     = WcfService1.IServiceMatrice
```

## 13. ServiceMatrice.svc

Sadrzaj:

```aspx
<%@ ServiceHost Language="C#" Debug="true" Service="WcfService1.ServiceMatrice" CodeBehind="ServiceMatrice.svc.cs" %>
```

Ovo je popravljeno. Pre je pisalo `WcfService1.Service1`, a ta klasa ne postoji kao servis za matrice.

## 14. Klijent i proxy

Klijent koristi rucni proxy preko `ChannelFactory`.

```csharp
ChannelFactory<IServiceMatrice> factory =
    new ChannelFactory<IServiceMatrice>("BasicHttpBinding_IServiceMatrice");

IServiceMatrice proxy = factory.CreateChannel();
```

Ovo znaci:

- klijent zna interfejs `IServiceMatrice`;
- endpoint cita iz `App.config`;
- `CreateChannel()` pravi proxy objekat;
- preko proxy-ja zovemo server kao da je lokalni objekat.

Primer:

```csharp
proxy.Set(serverska);
proxy.Add(operand);
proxy.MultiplyScalar(3);
proxy.Multiply(operandZaMnozenje);
proxy.Transpond();
proxy.Take();
```

## 15. App.config klijenta

U klijentu endpoint pokazuje na servis:

```xml
<endpoint address="http://localhost:54991/ServiceMatrice.svc"
          binding="basicHttpBinding"
          bindingConfiguration="BasicHttpBinding_IServiceMatrice"
          contract="WcfService1.IServiceMatrice"
          name="BasicHttpBinding_IServiceMatrice" />
```

Ovo ime:

```text
BasicHttpBinding_IServiceMatrice
```

se koristi u kodu:

```csharp
new ChannelFactory<IServiceMatrice>("BasicHttpBinding_IServiceMatrice");
```

Ako promenis port servera, moras promeniti i `address` u `App.config`.

## 16. Kako se pokrece

1. Pokreni server projekat `WcfService1`.
2. Browser treba da otvori:

```text
http://localhost:54991/ServiceMatrice.svc
```

3. Ako se port razlikuje, prekopiraj pravi port u:

```text
ConsoleApp1/App.config
```

4. Pokreni klijent `ConsoleApp1`.

## 17. Ocekivan izlaz klijenta

Klijent treba da ispise:

```text
Set serverske matrice
       1       2
       3       4

Take serverske matrice
       1       2
       3       4

Sabiranje serverske matrice i operanda
      11      22
      33      44

Mnozenje serverske matrice skalarom 3
       3       6
       9      12

Mnozenje serverske matrice drugom matricom
      19      22
      43      50

Transponovanje serverske matrice
       1       3
       2       4

Primer greske - sabiranje matrica razlicitih dimenzija
Greska: Za sabiranje matrice moraju imati isti broj redova i kolona.
```

Ovo pokriva sve operacije iz zadatka.

## 18. Sta ako zelis Service Reference umesto ChannelFactory

Trenutna verzija koristi `ChannelFactory`, sto je validan rucni proxy.

Ako profesor bas trazi `Service Reference`:

1. Pokreni server.
2. U klijentu dodaj Service Reference ka:

```text
http://localhost:54991/ServiceMatrice.svc
```

3. Namespace moze biti:

```text
ServiceReferenceMatrice
```

4. Tada bi kod koristio generisani proxy:

```csharp
var proxy = new ServiceMatriceClient();
```

5. Ako promenis ugovor, moras uraditi `Update Service Reference`.

Za vezbu je `ChannelFactory` dobar jer jasno pokazuje da je proxy objekat kanal ka endpoint-u.

## 19. Najbitnije za ispit

Ako se ovaj zadatak radi u svesci, pisi redom:

1. `DataContract` za `Matrica`.
2. `DataContract` za `Result`.
3. `ServiceContract` za `IServiceMatrice`.
4. Operacije:
   - `Set`;
   - `Add`;
   - `MultiplyScalar`;
   - `Multiply`;
   - `Transpond`;
   - `Take`.
5. Implementacija:
   - serverska matrica;
   - provera da li je setovana;
   - provera dimenzija;
   - vracanje `Result`.
6. Config:
   - `service name`;
   - `endpoint`;
   - `basicHttpBinding`;
   - `contract`.
7. Klijent:
   - kreira matricu;
   - pravi proxy;
   - zove sve operacije;
   - proverava `IsError`.

Ako ovo imas, pokrio si sustinu zadatka.
