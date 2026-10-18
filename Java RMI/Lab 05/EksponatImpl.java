import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class EksponatImpl extends UnicastRemoteObject implements Eksponat {   // Jedan deljeni eksponat koji zivi na serveru.
    private final String id;                                                   
    private final String naziv;                                                
    private int cena;                                                  
    private KlijentAukcije klijentAukcije;                    

    public EksponatImpl(String id, String naziv, int cena) throws RemoteException {
        super();                                                             
        this.id = id;
        this.naziv = naziv;
        this.cena = cena;
    }

    @Override
    public synchronized void prijaviLicitaciju(KlijentAukcije ka) throws RemoteException {   
        klijentAukcije = ka;
    }

    @Override
    public synchronized KlijentAukcije vratiKlijentaAukcije() throws RemoteException {
        return klijentAukcije;
    }

    @Override
    public synchronized void odustaniOdLicitacije(String klijentAukcijeId) throws RemoteException {
        if (klijentAukcije != null && klijentAukcije.vratiKlijentAukcijeId().equals(klijentAukcijeId)) {   // Odustati moze samo trenutno prijavljeni klijent.
            klijentAukcije = null;
        }
    }

    @Override
    public synchronized String vratiNaziv() throws RemoteException {
        return naziv;
    }

    @Override
    public synchronized int vratiCenu() throws RemoteException {
        return cena;
    }

    @Override
    public synchronized void povecajCenu(int iznos) throws RemoteException {
        if (iznos > 0) {                                                 
            cena += iznos;                                                 
        }
    }

    public String vratiId() {                                               
        return id;
    }
}
