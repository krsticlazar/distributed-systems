import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.LinkedHashMap;
import java.util.Map;

public class EAukcijaImpl extends UnicastRemoteObject implements EAukcija {   // Serverska baza svih eksponata.
    private final Map<String, Eksponat> eksponati;                             // Omogucava pretragu po identifikatoru.

    public EAukcijaImpl() throws RemoteException {
        super();
        eksponati = new LinkedHashMap<String, Eksponat>();                     // Mapa cuva pet unapred definisanih eksponata.
        eksponati.put("EKS_997", new EksponatImpl("EKS_997", "Starinska vaza", 1200));
        eksponati.put("EKS_991", new EksponatImpl("EKS_991", "Umetnicka slika", 99200));
        eksponati.put("EKS_992", new EksponatImpl("EKS_992", "Rucni sat", 3500));
        eksponati.put("EKS_993", new EksponatImpl("EKS_993", "Stara knjiga", 2400));
        eksponati.put("EKS_994", new EksponatImpl("EKS_994", "Srebrni escajg", 7800));
    }

    @Override
    public Eksponat vratiEksponat(String idEksponata) throws RemoteException {   // Klijentu vraca trazeni eksponat ili null.
        return eksponati.get(idEksponata);
    }
}
