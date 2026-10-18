import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

public class GlasanjeImpl extends UnicastRemoteObject implements Glasanje {
    private final Map<Integer, Integer> glasovi = new LinkedHashMap<>();
    private final Set<String> glasali = new HashSet<>();

    public GlasanjeImpl() throws RemoteException {
        super();
        glasovi.put(1, 0);
        glasovi.put(2, 0);
        glasovi.put(3, 0);
    }

    public synchronized boolean glasaj(String biracId, int kandidatId)
            throws RemoteException {
        if (glasali.contains(biracId) || !glasovi.containsKey(kandidatId)) {
            return false;
        }

        glasali.add(biracId);
        glasovi.put(kandidatId, glasovi.get(kandidatId) + 1);
        return true;
    }

    public synchronized Rezultat vratiRezultat(int kandidatId)
            throws RemoteException {
        Integer brojGlasova = glasovi.get(kandidatId);

        if (brojGlasova == null) {
            return null;
        }

        int mesto = 1;
        for (int drugiBroj : glasovi.values()) {
            if (drugiBroj > brojGlasova) {
                mesto++;
            }
        }

        return new Rezultat(kandidatId, brojGlasova, mesto);
    }
}
