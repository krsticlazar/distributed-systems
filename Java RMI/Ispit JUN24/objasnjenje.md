# JUN24 - MQTT broker

## Sta zadatak trazi

Treba napraviti RMI simulaciju MQTT brokera. Broker cuva topike, klijenti se pretplacuju na topik metodom `subscribe`, a poruke se salju metodom `publish`.

Poruka ima `naslov` i `sadrzaj`. Topik je obican `String`.

## Struktura

- `Broker` je glavni remote interfejs servera.
- `BrokerImpl` je serverska implementacija brokera.
- `MqttClient` je callback remote interfejs koji implementira klijent.
- `Poruka` je `Serializable` jer se salje preko mreze kao podatak.
- `Server` registruje broker u RMI registry.
- `Klijent` moze da radi kao pretplaceni klijent ili kao klijent koji objavljuje poruku.

## Zasto callback

`subscribe` znaci da klijent ceka poruke koje ce tek stici. Zato broker ne moze samo da vrati rezultat odmah, nego mora kasnije da pozove klijenta kada neko uradi `publish`.

Zbog toga `MqttClient` postoji kao remote callback interfejs:

```java
public interface MqttClient extends Remote {
    void primiPoruku(String topic, Poruka poruka) throws RemoteException;
}
```

Klijent napravi objekat koji implementira `MqttClient` i prosledi ga brokeru. Broker cuva tu remote referencu u listi pretplacenih klijenata.

## Tok rada

1. Server pokrece registry na portu `1099`.
2. Server pravi `BrokerImpl`.
3. Server registruje broker pod imenom `Broker`.
4. Klijent koji slusa poziva `subscribe(topic, callback)`.
5. Broker kreira topik ako ne postoji i cuva callback.
6. Drugi klijent poziva `publish(topic, poruka)`.
7. Broker pronalazi sve pretplacene klijente za taj topik.
8. Broker svakom klijentu poziva `primiPoruku(topic, poruka)`.

## Za ispit

U ispitnoj svesci ne treba pisati kompletan meni i svu obradu argumenata. Bitno je da se vidi RMI struktura i callback.

Iz teksta zadatka treba zakljuciti:

- `Broker` je glavni remote objekat.
- `subscribe` prima topik i callback klijenta.
- `publish` prima topik i poruku.
- `Poruka` je `Serializable` jer ima samo naslov i sadrzaj.
- `MqttClient` je callback remote interfejs jer broker naknadno dostavlja poruke pretplacenim klijentima.
- `createTopic` moze biti privatna metoda u implementaciji, jer klijent ne pravi topik direktno.

Minimalni remote interfejs brokera:

```java
public interface Broker extends Remote {
    void subscribe(String topic, MqttClient klijent) throws RemoteException;
    void publish(String topic, Poruka poruka) throws RemoteException;
}
```

Minimalni callback interfejs:

```java
public interface MqttClient extends Remote {
    void primiPoruku(String topic, Poruka poruka) throws RemoteException;
}
```

Minimalna poruka:

```java
public class Poruka implements Serializable {
    private String naslov;
    private String sadrzaj;
}
```

Minimalna serverska logika:

```java
private Map<String, List<MqttClient>> topici = new HashMap<>();

private void createTopic(String topic) {
    topici.putIfAbsent(topic, new ArrayList<>());
}

public synchronized void subscribe(String topic, MqttClient klijent) throws RemoteException {
    createTopic(topic);
    topici.get(topic).add(klijent);
}

public void publish(String topic, Poruka poruka) throws RemoteException {
    List<MqttClient> pretplaceni;

    synchronized (this) {
        createTopic(topic);
        pretplaceni = new ArrayList<>(topici.get(topic));
    }

    for (MqttClient klijent : pretplaceni) {
        klijent.primiPoruku(topic, poruka);
    }
}
```

Minimalni klijentski callback:

```java
public class MqttClientImpl extends UnicastRemoteObject implements MqttClient {
    public MqttClientImpl() throws RemoteException {
        super();
    }

    public void primiPoruku(String topic, Poruka poruka) throws RemoteException {
        System.out.println(topic + ": " + poruka);
    }
}
```

Minimalno server pokretanje:

```java
LocateRegistry.createRegistry(1099);
Naming.rebind("rmi://localhost:1099/Broker", new BrokerImpl());
```

Minimalno klijent pretplacivanje:

```java
Broker broker = (Broker) Naming.lookup("rmi://localhost:1099/Broker");
MqttClient cb = new MqttClientImpl();
broker.subscribe("sport", cb);
```

Minimalno slanje poruke:

```java
Broker broker = (Broker) Naming.lookup("rmi://localhost:1099/Broker");
broker.publish("sport", new Poruka("Gol", "Domaci tim vodi 1:0"));
```

Najbitnija recenica za obrazlozenje: callback je potreban jer se klijent pretplacuje na buduce poruke, pa broker mora kasnije da pozove klijenta kada poruka stigne na odgovarajuci topik.
